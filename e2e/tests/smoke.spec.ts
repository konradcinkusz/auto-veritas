import { expect, test, type Page } from '@playwright/test';
import { randomUUID } from 'node:crypto';

/**
 * The smoke tier from CHARTER.md: the flows a visitor and the owner's agent depend
 * on, one browser, web-first assertions only. The stack under test is ephemeral
 * (compose-per-run or the session stack), so generated accounts need no cleanup.
 *
 * AUTH_URL/OFFERS_URL point at the identity service and the offers API for the
 * agent-write flow; ADMIN_EMAIL/ADMIN_PASSWORD are the seeded InitialAdmin of the
 * ephemeral stack, never production credentials.
 */

const AUTH_URL = process.env.AUTH_URL ?? 'http://localhost:8081';
const OFFERS_URL = process.env.OFFERS_URL ?? 'http://localhost:8082';
const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin@auto-veritas.local';
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'Admin123!';

async function registerFreshUser(page: Page): Promise<string> {
  const email = `e2e-${randomUUID()}@example.test`;
  await page.goto('/register');
  await page.getByLabel('E-mail').fill(email);
  await page.getByLabel('Hasło').fill('E2e!Passw0rd');
  await page.getByLabel(/Akceptuję regulamin/).check();
  await page.getByRole('button', { name: 'Zarejestruj się' }).click();
  await expect(page.getByRole('heading', { name: 'Auto Veritas' })).toBeVisible();
  return email;
}

test('an anonymous visitor is redirected to the login page @smoke', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByRole('button', { name: 'Zaloguj się' })).toBeVisible();
});

test('registration lands on a dashboard with both offer tables @smoke', async ({ page }) => {
  await registerFreshUser(page);

  await expect(page.getByRole('heading', { name: 'Porównanie modeli' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Porównanie finansowania' })).toBeVisible();
  await expect(page.getByRole('cell', { name: /BYD Atto 2 DM-i Active/ })).toBeVisible();
  await expect(page.getByRole('cell', { name: /Bankinter Consumer Finance/ })).toBeVisible();
});

test('every offer row carries verification metadata @smoke', async ({ page }) => {
  await registerFreshUser(page);

  const carsTable = page.locator('section.card').filter({ hasText: 'Porównanie modeli' });
  const rows = carsTable.locator('tbody tr');
  await expect(rows.first()).toBeVisible();
  const rowCount = await rows.count();
  expect(rowCount).toBeGreaterThan(0);
  for (let index = 0; index < rowCount; index++) {
    await expect(rows.nth(index).getByText(/sprawdzono/)).toBeVisible();
  }
});

test('the financing table exposes the repayment structure, balloon included @smoke', async ({ page }) => {
  await registerFreshUser(page);

  const creditsTable = page.locator('section.card').filter({ hasText: 'Porównanie finansowania' });
  await expect(creditsTable.getByRole('columnheader', { name: 'Struktura' })).toBeVisible();
  const toyotaRow = creditsTable.locator('tbody tr').filter({ hasText: 'Toyota Easy' });
  await expect(toyotaRow.getByText('BALON', { exact: true })).toBeVisible();
});

test('an offer added by the agent through the API appears for a viewer @smoke', async ({ page, request }) => {
  const login = await request.post(`${AUTH_URL}/api/v1/auth/login`, {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  expect(login.ok()).toBeTruthy();
  const { accessToken } = (await login.json()) as { accessToken: string };

  const slug = `e2e-agent-offer-${randomUUID().slice(0, 8)}`;
  const offerName = `E2E Nissan Qashqai ${slug.slice(-4)}`;
  const created = await request.post(`${OFFERS_URL}/api/v1/car-offers`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: {
      slug,
      name: offerName,
      variant: 'SUV / HEV',
      dgtLabel: 'Eco',
      powerCv: 158,
      cashPriceEur: 27900,
      priceConfidence: 'Confirmed',
      lastVerifiedAt: new Date().toISOString(),
    },
  });
  expect(created.status()).toBe(201);

  await registerFreshUser(page);
  await expect(page.getByRole('cell', { name: offerName })).toBeVisible();
});

test('logout ends the session @smoke', async ({ page }) => {
  await registerFreshUser(page);

  await page.getByRole('button', { name: 'Wyloguj' }).click();
  await expect(page).toHaveURL(/\/login/);

  await page.goto('/');
  await expect(page).toHaveURL(/\/login/);
});
