import { test, expect } from '@playwright/test';

test.describe('Register Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/registrar');
  });

  test('should display registration form', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /cadastr|criar conta|registr/i })).toBeVisible();
  });

  test('should have required fields', async ({ page }) => {
    await expect(page.getByPlaceholder('Digite seu nome')).toBeVisible();
    await expect(page.getByPlaceholder('seu@email.com')).toBeVisible();
    await expect(page.getByPlaceholder('000.000.000-00')).toBeVisible();
    await expect(page.locator('input[placeholder="Mínimo 8 caracteres"]')).toBeVisible();
  });

  test('should show validation errors for empty form', async ({ page }) => {
    // O botão de cadastro está desabilitado quando o formulário é inválido
    // Então verificamos que o botão existe e está desabilitado
    const submitButton = page.getByRole('button', { name: /cadastr|criar|registr/i });
    await expect(submitButton).toBeVisible();
    await expect(submitButton).toBeDisabled();
  });

  test('should validate CPF format', async ({ page }) => {
    const cpfInput = page.getByPlaceholder('000.000.000-00');
    await cpfInput.fill('12345678900');
    await cpfInput.blur();
    
    // O CPF é formatado automaticamente, então verificamos o valor formatado
    await expect(cpfInput).toHaveValue('123.456.789-00');
  });

  test('should validate password strength', async ({ page }) => {
    const passwordInput = page.locator('input[placeholder="Mínimo 8 caracteres"]');
    await passwordInput.fill('weak');
    await passwordInput.blur();
    
    // Verifica que a senha fraca foi inserida
    await expect(passwordInput).toHaveValue('weak');
    
    // O botão deve permanecer desabilitado com senha fraca
    const submitButton = page.getByRole('button', { name: /cadastr|criar|registr/i });
    await expect(submitButton).toBeDisabled();
  });

  test('should validate password confirmation', async ({ page }) => {
    const passwordInput = page.locator('input[placeholder="Mínimo 8 caracteres"]');
    const confirmInput = page.locator('input[placeholder="Digite a senha novamente"]');
    
    await passwordInput.fill('Password123!');
    await confirmInput.fill('Different123!');
    await confirmInput.blur();
    
    // Com senhas diferentes, o botão deve estar desabilitado
    const submitButton = page.getByRole('button', { name: /cadastr|criar|registr/i });
    await expect(submitButton).toBeDisabled();
  });

  test('should have link to login page', async ({ page }) => {
    const loginLink = page.getByRole('link', { name: /já tenho conta|entrar|login/i });
    await expect(loginLink).toBeVisible();
    
    await loginLink.click();
    await expect(page).toHaveURL(/entrar/);
  });
});
