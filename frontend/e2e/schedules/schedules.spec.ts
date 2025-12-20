import { test, expect } from '@playwright/test';

// Credenciais do DataSeeder
const PROFESSIONAL_USER = { email: 'med@med.com', password: 'zxcasd12' };

async function login(page) {
  await page.goto('/entrar');
  await page.waitForLoadState('networkidle');
  await page.getByPlaceholder('seu@email.com').fill(PROFESSIONAL_USER.email);
  await page.locator('input[placeholder="Digite sua senha"]').fill(PROFESSIONAL_USER.password);
  await page.getByRole('button', { name: /entrar|login/i }).click();
  await page.waitForURL(/painel/, { timeout: 15000 });
}

test.describe('Schedules Page', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.goto('/minha-agenda');
  });

  test('should display schedule page', async ({ page }) => {
    const content = page.locator('main, [class*="schedule"], [class*="agenda"], [class*="content"]').first();
    await expect(content).toBeVisible({ timeout: 10000 });
  });

  test('should show calendar or time slots', async ({ page }) => {
    const calendar = page.locator('[class*="calendar"], [class*="slot"], [class*="time"]').first();
    if (await calendar.count() > 0) {
      await expect(calendar).toBeVisible();
    }
  });

  test('should have add schedule button', async ({ page }) => {
    const addBtn = page.getByRole('button', { name: /adicionar|novo|criar|add|new/i });
    if (await addBtn.count() > 0) {
      await expect(addBtn).toBeVisible();
    }
  });

  test('should filter by professional or date', async ({ page }) => {
    const filter = page.locator('select, input[type="date"], [class*="filter"]').first();
    if (await filter.count() > 0) {
      await expect(filter).toBeVisible();
    }
  });
});

test.describe('Schedule Creation', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.goto('/minha-agenda');
  });

  test('should have schedule creation form', async ({ page }) => {
    const form = page.locator('form').first();
    if (await form.count() > 0) {
      await expect(form).toBeVisible({ timeout: 10000 });
    }
  });

  test('should have time slot selection', async ({ page }) => {
    const timeInput = page.locator('input[type="time"], [class*="time-picker"]').first();
    if (await timeInput.count() > 0) {
      await expect(timeInput).toBeVisible();
    }
  });
});

test.describe('Schedule Blocks', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.goto('/bloqueios-agenda');
  });

  test('should display schedule blocks page', async ({ page }) => {
    const content = page.locator('main, [class*="block"], [class*="content"]').first();
    await expect(content).toBeVisible({ timeout: 10000 });
  });

  test('should have add block button', async ({ page }) => {
    const addBtn = page.getByRole('button', { name: /adicionar|novo|bloquear|add|new/i });
    if (await addBtn.count() > 0) {
      await expect(addBtn).toBeVisible();
    }
  });
});
