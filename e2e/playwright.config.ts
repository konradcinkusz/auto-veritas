import { defineConfig, devices } from '@playwright/test';
import { STORAGE_STATE } from './storage-state';

export default defineConfig({
  testDir: './tests',
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    // Sandboxes with a pre-provisioned Chromium point this at the binary
    // instead of downloading a browser per run.
    ...(process.env.PLAYWRIGHT_EXECUTABLE_PATH
      ? { launchOptions: { executablePath: process.env.PLAYWRIGHT_EXECUTABLE_PATH } }
      : {}),
  },
  // Tiers are separate PROJECTS, not just tags, because they need different
  // session handling: smoke registers per test (registration is part of the
  // surface it protects, and one of its flows is the anonymous redirect, which
  // a pre-seeded session would defeat), while core runs against one shared
  // stored session. Each project matches exactly one spec file so nothing is
  // collected twice under two different session assumptions.
  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'smoke',
      testMatch: /smoke\.spec\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'core',
      testMatch: /core\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], storageState: STORAGE_STATE },
      dependencies: ['setup'],
    },
  ],
});
