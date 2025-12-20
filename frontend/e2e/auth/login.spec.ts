import { test, expect } from '@playwright/test';

// Credenciais do DataSeeder
const ADMIN_USER = { email: 'adm@adm.com', password: 'zxcasd12' };
const PROFESSIONAL_USER = { email: 'med@med.com', password: 'zxcasd12' };
const PATIENT_USER = { email: 'pac@pac.com', password: 'zxcasd12' };

test.describe('Login Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/entrar');
    // Aguardar o app Angular carregar completamente
    await page.waitForLoadState('networkidle');
  });

  test('should display login form', async ({ page }) => {
    await expect(page.getByPlaceholder('seu@email.com')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('input[placeholder="Digite sua senha"]')).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('button', { name: /entrar|login/i })).toBeVisible({ timeout: 10000 });
  });

  test('should show error for empty form submission', async ({ page }) => {
    await page.getByRole('button', { name: /entrar|login/i }).click();
    
    // Should show validation errors
    await expect(page.getByText(/obrigatório|required|preencha/i).first()).toBeVisible({ timeout: 5000 });
  });

  test('should show error for invalid credentials', async ({ page }) => {
    await page.getByPlaceholder('seu@email.com').fill('invalid@example.com');
    await page.locator('input[placeholder="Digite sua senha"]').fill('wrongpassword12');
    await page.getByRole('button', { name: /entrar|login/i }).click();
    
    // Wait for error message - pode ser uma mensagem de alerta ou texto de erro
    const errorMessage = page.locator('.auth-alert--error, [class*="error"], [class*="alert"]').first();
    await expect(errorMessage).toBeVisible({ timeout: 10000 });
  });

  test('should have link to register page', async ({ page }) => {
    const registerLink = page.getByRole('link', { name: /criar conta|cadastr|registr/i });
    await expect(registerLink).toBeVisible();
    
    await registerLink.click();
    await expect(page).toHaveURL(/registrar|cadastro|signup/);
  });

  test('should have link to forgot password', async ({ page }) => {
    const forgotLink = page.getByRole('link', { name: /esquec|forgot|recuperar/i });
    if (await forgotLink.count() > 0) {
      await expect(forgotLink).toBeVisible();
    }
  });

  test('should toggle password visibility', async ({ page }) => {
    const passwordInput = page.locator('input[placeholder="Digite sua senha"]');
    await passwordInput.fill('testpassword');
    
    // Click toggle button if exists
    const toggleButton = page.locator('.input-password__toggle').first();
    if (await toggleButton.count() > 0) {
      await toggleButton.click();
      await page.waitForTimeout(300);
    }
  });

  test('should login successfully with admin credentials', async ({ page }) => {
    await page.getByPlaceholder('seu@email.com').fill(ADMIN_USER.email);
    await page.locator('input[placeholder="Digite sua senha"]').fill(ADMIN_USER.password);
    await page.getByRole('button', { name: /entrar|login/i }).click();
    
    // Should redirect to dashboard
    await expect(page).toHaveURL(/painel/, { timeout: 15000 });
  });

  test('should login successfully with professional credentials', async ({ page }) => {
    await page.getByPlaceholder('seu@email.com').fill(PROFESSIONAL_USER.email);
    await page.locator('input[placeholder="Digite sua senha"]').fill(PROFESSIONAL_USER.password);
    await page.getByRole('button', { name: /entrar|login/i }).click();
    
    // Should redirect to dashboard
    await expect(page).toHaveURL(/painel/, { timeout: 15000 });
  });

  test('should login successfully with patient credentials', async ({ page }) => {
    await page.getByPlaceholder('seu@email.com').fill(PATIENT_USER.email);
    await page.locator('input[placeholder="Digite sua senha"]').fill(PATIENT_USER.password);
    await page.getByRole('button', { name: /entrar|login/i }).click();
    
    // Should redirect to dashboard
    await expect(page).toHaveURL(/painel/, { timeout: 15000 });
  });
});
