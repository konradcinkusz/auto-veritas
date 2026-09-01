import { expect, test as setup } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { STORAGE_STATE } from '../storage-state';

/**
 * Registers one viewer and saves its session for the whole core tier.
 *
 * CHARTER.md: "The stored-context pattern becomes mandatory when the
 * core-regression tier lands and login stops being what most tests are about."
 * The smoke tier keeps registering per test on purpose — registration is
 * itself part of the surface it protects — but core tests are about the
 * dashboard, and paying for a registration round-trip in each one buys
 * nothing except a slower suite and more ways to fail.
 */
setup('authenticate a viewer for the core tier', async ({ page }) => {
  const email = `e2e-core-${randomUUID()}@example.test`;
  await page.goto('/register');
  await page.getByLabel('E-mail').fill(email);
  await page.getByLabel('Hasło').fill('E2e!Passw0rd');
  await page.getByLabel(/Akceptuję regulamin/).check();
  await page.getByRole('button', { name: 'Zarejestruj się' }).click();
  await expect(page.getByRole('heading', { name: 'Auto Veritas' })).toBeVisible();

  await page.context().storageState({ path: STORAGE_STATE });
});
