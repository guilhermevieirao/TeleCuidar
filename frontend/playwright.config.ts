import { defineConfig, devices } from '@playwright/test';

// Carrega configurações do .env via variáveis de ambiente
const baseURL = process.env.TEST_BASE_URL || 'http://localhost:4200';
const timeout = parseInt(process.env.TEST_TIMEOUT || '30000', 10);

export default defineConfig({
  testDir: './e2e',
  testMatch: /.*\.spec\.ts$/,
  testIgnore: /.*src.*/,
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  timeout: timeout,
  
  use: {
    baseURL: baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: 'npm run start',
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
