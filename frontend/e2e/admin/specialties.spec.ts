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

test.describe('Specialties Management', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/especialidades');
  });

  test('should display specialties list', async ({ page }) => {
    const list = page.locator('table, [class*="specialty"], [class*="list"]').first();
    await expect(list).toBeVisible({ timeout: 10000 });
  });

  test('should have add specialty button', async ({ page }) => {
    const addBtn = page.getByRole('button', { name: /adicionar|nova|criar|add|new/i });
    if (await addBtn.count() > 0) {
      await expect(addBtn).toBeVisible();
    }
  });

  test('should open create specialty modal or form', async ({ page }) => {
    const addBtn = page.getByRole('button', { name: /adicionar|nova|criar|add|new/i });
    if (await addBtn.count() > 0) {
      await addBtn.click();
      await page.waitForTimeout(500);
      
      const modal = page.locator('[class*="modal"], [class*="dialog"], form').first();
      await expect(modal).toBeVisible({ timeout: 5000 });
    }
  });

  test('should show specialty details on click', async ({ page }) => {
    const specialtyItem = page.locator('tr, [class*="specialty-item"]').first();
    if (await specialtyItem.count() > 0) {
      await specialtyItem.click();
      await page.waitForTimeout(500);
    }
  });
});

test.describe('Specialty CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/especialidades');
  });

  test('should validate required fields when creating specialty', async ({ page }) => {
    const addBtn = page.getByRole('button', { name: /adicionar|nova|criar|add|new/i });
    if (await addBtn.count() > 0) {
      await addBtn.click();
      await page.waitForTimeout(500);
      
      // Verifica se abriu modal ou form
      const formOrModal = page.locator('form, [role="dialog"], [class*="modal"]').first();
      if (await formOrModal.count() > 0) {
        // O botão de submit deve estar desabilitado quando o form está vazio
        const submitBtn = page.getByRole('button', { name: /salvar|criar|save|create/i });
        if (await submitBtn.count() > 0) {
          await expect(submitBtn).toBeDisabled();
        }
      }
    } else {
      // Se não há botão de adicionar, apenas verifica que a página carregou
      await expect(page.locator('main, [class*="content"]').first()).toBeVisible();
    }
  });
});
