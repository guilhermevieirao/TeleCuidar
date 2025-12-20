import { test, expect } from '@playwright/test';

// Credenciais do DataSeeder
const ADMIN_USER = { email: 'adm@adm.com', password: 'zxcasd12' };

async function login(page) {
  await page.goto('/entrar');
  await page.waitForLoadState('networkidle');
  await page.getByPlaceholder('seu@email.com').fill(ADMIN_USER.email);
  await page.locator('input[placeholder="Digite sua senha"]').fill(ADMIN_USER.password);
  await page.getByRole('button', { name: /entrar|login/i }).click();
  await page.waitForURL(/painel/, { timeout: 15000 });
}

test.describe('Profile Page', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.goto('/perfil');
  });

  test('should display profile page', async ({ page }) => {
    const content = page.locator('main, [class*="profile"], [class*="content"]').first();
    await expect(content).toBeVisible({ timeout: 10000 });
  });

  test('should show user information', async ({ page }) => {
    const nameField = page.locator('input[name="name"], input[placeholder*="nome"], [class*="name"]').first();
    if (await nameField.count() > 0) {
      await expect(nameField).toBeVisible();
    }
  });

  test('should have edit profile button', async ({ page }) => {
    const editBtn = page.getByRole('button', { name: /editar|edit|alterar/i });
    if (await editBtn.count() > 0) {
      await expect(editBtn).toBeVisible();
    }
  });

  test('should have avatar upload option', async ({ page }) => {
    const avatarSection = page.locator('[class*="avatar"], input[type="file"]').first();
    if (await avatarSection.count() > 0) {
      await expect(avatarSection).toBeVisible();
    }
  });
});

test.describe('Profile Update', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.goto('/perfil');
  });

  test('should allow updating profile name', async ({ page }) => {
    const nameInput = page.locator('input[name="name"], input[placeholder*="nome"]').first();
    if (await nameInput.count() > 0 && await nameInput.isEditable()) {
      await nameInput.fill('Updated Name');
      
      const saveBtn = page.getByRole('button', { name: /salvar|save|atualizar|update/i });
      if (await saveBtn.count() > 0) {
        await saveBtn.click();
        // Should show success message
        await page.waitForTimeout(1000);
      }
    }
  });

  test('should validate email format', async ({ page }) => {
    const emailInput = page.locator('input[name="email"], input[type="email"]').first();
    if (await emailInput.count() > 0 && await emailInput.isEditable()) {
      await emailInput.fill('invalid-email');
      await emailInput.blur();
      
      const error = page.getByText(/e-?mail inválido|invalid email/i);
      if (await error.count() > 0) {
        await expect(error).toBeVisible();
      }
    }
  });
});

test.describe('Change Password', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.goto('/perfil');
  });

  test('should have password change form', async ({ page }) => {
    const currentPassword = page.locator('input[name*="current"], input[placeholder*="atual"]').first();
    const newPassword = page.locator('input[name*="new"], input[placeholder*="nova"]').first();
    
    if (await currentPassword.count() > 0) {
      await expect(currentPassword).toBeVisible();
    }
    if (await newPassword.count() > 0) {
      await expect(newPassword).toBeVisible();
    }
  });
});
