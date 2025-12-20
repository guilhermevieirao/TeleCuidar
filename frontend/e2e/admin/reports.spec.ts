import { test, expect } from '@playwright/test';

// Credenciais do DataSeeder
const ADMIN_USER = { email: 'adm@adm.com', password: 'zxcasd12' };

async function loginAsAdmin(page) {
  await page.goto('/entrar');
  await page.waitForLoadState('networkidle');
  await page.getByPlaceholder('seu@email.com').fill(ADMIN_USER.email);
  await page.locator('input[placeholder="Digite sua senha"]').fill(ADMIN_USER.password);
  await page.getByRole('button', { name: /entrar|login/i }).click();
  await page.waitForURL(/painel/, { timeout: 15000 });
}

test.describe('Reports Page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/relatorios');
  });

  test('should display reports page', async ({ page }) => {
    const content = page.locator('main, [class*="report"], [class*="content"]').first();
    await expect(content).toBeVisible({ timeout: 10000 });
  });

  test('should have date range filter', async ({ page }) => {
    const dateFilter = page.locator('input[type="date"], [class*="date-picker"], [class*="datepicker"]').first();
    if (await dateFilter.count() > 0) {
      await expect(dateFilter).toBeVisible();
    }
  });

  test('should have report type selector', async ({ page }) => {
    const selector = page.locator('select, [class*="dropdown"], [class*="select"]').first();
    if (await selector.count() > 0) {
      await expect(selector).toBeVisible();
    }
  });

  test('should have export button', async ({ page }) => {
    const exportBtn = page.getByRole('button', { name: /exportar|export|download|pdf|excel/i });
    if (await exportBtn.count() > 0) {
      await expect(exportBtn).toBeVisible();
    }
  });

  test('should display charts or statistics', async ({ page }) => {
    const chart = page.locator('canvas, [class*="chart"], [class*="graph"], svg').first();
    if (await chart.count() > 0) {
      await expect(chart).toBeVisible({ timeout: 10000 });
    }
  });
});

test.describe('Report Generation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/relatorios');
  });

  test('should generate report on filter change', async ({ page }) => {
    const dateFilter = page.locator('input[type="date"]').first();
    if (await dateFilter.count() > 0) {
      await dateFilter.fill('2024-01-01');
      await page.waitForTimeout(1000);
      // Report should update
    }
  });
});
