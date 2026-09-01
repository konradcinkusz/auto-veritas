import { expect, test } from '@playwright/test';
import { randomUUID } from 'node:crypto';

/**
 * The core-regression tier from CHARTER.md: flows that are not smoke-critical
 * but would be expensive to break — the dashboard's filtering, sorting and
 * trust-ordering behaviour, and the empty states nobody looks at until they
 * are wrong.
 *
 * Runs against a shared stored session (see auth.setup.ts), so these tests are
 * about the dashboard rather than about logging in.
 */

const AUTH_URL = process.env.AUTH_URL ?? 'http://localhost:8081';
const OFFERS_URL = process.env.OFFERS_URL ?? 'http://localhost:8082';
const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin@auto-veritas.local';
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'Admin123!';

const DAY_MS = 24 * 60 * 60 * 1000;

async function agentToken(request: import('@playwright/test').APIRequestContext): Promise<string> {
  const login = await request.post(`${AUTH_URL}/api/v1/auth/login`, {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  const { accessToken } = (await login.json()) as { accessToken: string };
  return accessToken;
}

test('the search box narrows the car table and the count tag follows @core', async ({ page }) => {
  await page.goto('/');

  const countTag = page.locator('.count-tag').first();
  await expect(countTag).toBeVisible();
  const before = await countTag.innerText();

  // A term that cannot match any real model: the table should empty out and
  // say so rather than silently rendering zero rows.
  await page.getByLabel('Szukaj modelu / marki').fill('zzzz-no-such-model');
  await expect(page.getByText('Brak modeli spełniających wybrane kryteria.')).toBeVisible();

  await page.getByRole('button', { name: /Wyczyść filtry/ }).first().click();
  await expect(countTag).toHaveText(before);
});

test('the DGT filter keeps only matching rows @core', async ({ page }) => {
  await page.goto('/');

  await page.getByLabel('Etykieta DGT').selectOption('Cero');
  const carTable = page.locator('section.card').first();
  const labelCells = carTable.locator('tbody tr td:nth-child(2)');

  // Every remaining row must carry the filtered label. Asserting on all of
  // them, not just the first, is the point: a filter that leaks one wrong row
  // is exactly the bug this tier exists to catch.
  const count = await labelCells.count();
  expect(count).toBeGreaterThan(0);
  for (let i = 0; i < count; i += 1) {
    await expect(labelCells.nth(i)).toContainText(/CERO|Cero/i);
  }
});

test('stale offers sort below fresh ones even when cheaper @core', async ({ page, request }) => {
  const token = await agentToken(request);
  const headers = { Authorization: `Bearer ${token}` };
  const marker = randomUUID().slice(0, 6);

  // The trust model in one assertion: a stale offer that is CHEAPER must still
  // rank below a fresh dearer one. Default ordering is freshness first, price
  // second — degrade, never hide, and never let a dead price look like the
  // best deal.
  const staleName = `E2E Core Stale ${marker}`;
  const freshName = `E2E Core Fresh ${marker}`;

  await request.post(`${OFFERS_URL}/api/v1/car-offers`, {
    headers,
    data: {
      slug: `e2e-core-stale-${marker}`,
      name: staleName,
      variant: 'Kompakt / BEV',
      dgtLabel: 'Cero',
      powerCv: 150,
      cashPriceEur: 16000,
      priceConfidence: 'Confirmed',
      lastVerifiedAt: new Date(Date.now() - 200 * DAY_MS).toISOString(),
    },
  });
  await request.post(`${OFFERS_URL}/api/v1/car-offers`, {
    headers,
    data: {
      slug: `e2e-core-fresh-${marker}`,
      name: freshName,
      variant: 'Kompakt / BEV',
      dgtLabel: 'Cero',
      powerCv: 150,
      cashPriceEur: 44000,
      priceConfidence: 'Confirmed',
      lastVerifiedAt: new Date().toISOString(),
    },
  });

  await page.goto('/');
  await page.getByLabel('Szukaj modelu / marki').fill(`E2E Core`);

  const names = page.locator('section.card').first().locator('tbody tr td.model-name');
  await expect(names.filter({ hasText: freshName })).toHaveCount(1);
  await expect(names.filter({ hasText: staleName })).toHaveCount(1);

  const rendered = await names.allInnerTexts();
  const freshIndex = rendered.findIndex((text) => text.includes(freshName));
  const staleIndex = rendered.findIndex((text) => text.includes(staleName));
  expect(freshIndex).toBeGreaterThanOrEqual(0);
  expect(staleIndex).toBeGreaterThanOrEqual(0);
  expect(freshIndex).toBeLessThan(staleIndex);
});

test('a never-updated offer reports an empty history rather than an error @core', async ({ page, request }) => {
  const token = await agentToken(request);
  const marker = randomUUID().slice(0, 6);
  const offerName = `E2E Core Untouched ${marker}`;

  await request.post(`${OFFERS_URL}/api/v1/car-offers`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      slug: `e2e-core-untouched-${marker}`,
      name: offerName,
      variant: 'SUV / HEV',
      dgtLabel: 'Eco',
      powerCv: 140,
      cashPriceEur: 27000,
      priceConfidence: 'Confirmed',
      lastVerifiedAt: new Date().toISOString(),
    },
  });

  await page.goto('/');
  await page.getByLabel('Szukaj modelu / marki').fill(offerName);

  const row = page.locator('tbody tr', { hasText: offerName });
  await row.getByRole('button', { name: 'Historia' }).click();

  // An offer that has never been edited has no prior versions. The panel must
  // say that plainly — an empty table or a spinner that never resolves reads
  // as "history is broken", which undermines the exact claim it supports.
  await expect(page.getByText('Brak wcześniejszych zmian — to pierwsza zapisana wersja.')).toBeVisible();
});

test('an offer with no recorded source says so in its details panel @core', async ({ page, request }) => {
  const token = await agentToken(request);
  const marker = randomUUID().slice(0, 6);
  const offerName = `E2E Core Sourceless ${marker}`;

  // Deliberately no sourceName / sourceUrl — both columns are nullable.
  await request.post(`${OFFERS_URL}/api/v1/car-offers`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      slug: `e2e-core-sourceless-${marker}`,
      name: offerName,
      variant: 'Kombi / PHEV',
      dgtLabel: 'Eco',
      powerCv: 180,
      cashPriceEur: 31000,
      priceConfidence: 'Estimated',
      lastVerifiedAt: new Date().toISOString(),
    },
  });

  await page.goto('/');
  await page.getByLabel('Szukaj modelu / marki').fill(offerName);

  const row = page.locator('tbody tr', { hasText: offerName });
  await row.getByRole('button', { name: 'Szczegóły' }).click();

  // Claiming verification while pointing at nothing is the failure the panel
  // exists to close, so the absence has to be stated, not left blank.
  await expect(page.getByText('brak zapisanego źródła')).toBeVisible();
});

test('filter state round-trips through the URL @core', async ({ page }) => {
  await page.goto('/');

  await page.getByLabel('Szukaj modelu / marki').fill('BYD');
  await page.getByLabel('Etykieta DGT').selectOption('Cero');

  // The address bar is written without navigating, so wait for it rather than
  // assuming it is synchronous with the keystroke.
  await expect(page).toHaveURL(/[?&]q=BYD/);
  await expect(page).toHaveURL(/[?&]dgt=Cero/);
  const shared = page.url();

  // What a second person opening the pasted link actually gets: the controls
  // restored, not just the query string present.
  await page.goto(shared);
  await expect(page.getByLabel('Szukaj modelu / marki')).toHaveValue('BYD');
  await expect(page.getByLabel('Etykieta DGT')).toHaveValue('Cero');

  // Defaults are dropped rather than written, so a reset link is clean.
  await page.getByRole('button', { name: /Wyczyść filtry/ }).first().click();
  await expect(page).not.toHaveURL(/[?&]q=/);
  await expect(page).not.toHaveURL(/[?&]dgt=/);
});
