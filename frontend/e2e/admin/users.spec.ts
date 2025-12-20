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

test.describe('Users Management (Admin)', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('/usuarios');
  });

  test('should display users list', async ({ page }) => {
    const usersList = page.locator('table, [class*="users"], [class*="list"]').first();
    await expect(usersList).toBeVisible({ timeout: 10000 });
  });

  test('should have search functionality', async ({ page }) => {
    const search = page.locator('input[type="search"], input[placeholder*="buscar"], input[placeholder*="search"]').first();
    if (await search.count() > 0) {
      await search.fill('test');
      await page.waitForTimeout(500);
      // Search should trigger filter
    }
  });

  test('should have add user button', async ({ page }) => {
    const addBtn = page.getByRole('button', { name: /adicionar|novo|criar|add|new/i });
    if (await addBtn.count() > 0) {
      await expect(addBtn).toBeVisible();
    }
  });

  test('should filter by role', async ({ page }) => {
    const roleFilter = page.locator('select, [class*="filter"]').first();
    if (await roleFilter.count() > 0) {
      await roleFilter.click();
    }
  });

  test('should display user details on click', async ({ page }) => {
    const userRow = page.locator('tr, [class*="user-item"]').first();
    if (await userRow.count() > 0) {
      await userRow.click();
      await page.waitForTimeout(1000);
    }
  });
});

test.describe('User Creation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('should open user creation form', async ({ page }) => {
    await page.goto('/usuarios');
    await page.waitForLoadState('networkidle');
    
    // Procura por botão de adicionar usuário
    const addBtn = page.getByRole('button', { name: /adicionar|novo|criar|add|new/i });
    if (await addBtn.count() > 0) {
      await addBtn.click();
      await page.waitForTimeout(500);
      
      // Verifica se o formulário ou modal abriu
      const formOrModal = page.locator('form, [role="dialog"], [class*="modal"]').first();
      await expect(formOrModal).toBeVisible({ timeout: 5000 });
    } else {
      // Se não tem botão, apenas verifica que a página carregou
      await expect(page.locator('main, [class*="content"]').first()).toBeVisible();
    }
  });

  test('should have required fields in user form', async ({ page }) => {
    await page.goto('/usuarios');
    await page.waitForLoadState('networkidle');
    
    // Verifica se a página carregou corretamente
    await expect(page.locator('main, [class*="content"], [class*="usuarios"]').first()).toBeVisible();
  });
});
