import { defineConfig, devices } from '@playwright/test';

/** Pruebas E2E en perfiles de celular (research R11). */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  reporter: process.env['CI'] ? 'github' : 'list',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    locale: 'es-MX',
    timezoneId: 'America/Mexico_City',
  },
  projects: [
    { name: 'Pixel 7', use: { ...devices['Pixel 7'] } },
    { name: 'iPhone 14', use: { ...devices['iPhone 14'] } },
  ],
  webServer: {
    command: 'npm start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env['CI'],
    timeout: 180_000,
  },
});
