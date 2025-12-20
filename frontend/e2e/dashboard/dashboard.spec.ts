import { test, expect } from '@playwright/test';

// Credenciais do DataSeeder
const ADMIN_USER = { email: 'adm@adm.com', password: 'zxcasd12' };
const PROFESSIONAL_USER = { email: 'med@med.com', password: 'zxcasd12' };
const PATIENT_USER = { email: 'pac@pac.com', password: 'zxcasd12' };

// Helper para fazer login
async function login(page, user = ADMIN_USER) {
  await page.goto('/entrar');
  await page.waitForLoadState('networkidle');
  await page.getByPlaceholder('seu@email.com').fill(user.email);
  await page.locator('input[placeholder="Digite sua senha"]').fill(user.password);
  await page.getByRole('button', { name: /entrar|login/i }).click();
  await page.waitForURL(/painel/, { timeout: 15000 });
}

test.describe('Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('should display dashboard after login', async ({ page }) => {
    await expect(page).toHaveURL(/painel|dashboard/);
  });

  test('should show user info in header', async ({ page }) => {
    // Deve mostrar nome ou avatar do usuário
    const userInfo = page.locator('[class*="user"], [class*="avatar"], [class*="profile"]').first();
    await expect(userInfo).toBeVisible({ timeout: 10000 });
  });

  test('should have navigation menu', async ({ page }) => {
    // Sidebar ou menu de navegação
    const nav = page.locator('nav, [class*="sidebar"], [class*="menu"]').first();
    await expect(nav).toBeVisible();
  });

  test('should display statistics or cards', async ({ page }) => {
    // Dashboard deve ter cards ou estatísticas
    const statsSection = page.locator('[class*="card"], [class*="stat"], [class*="widget"]').first();
    await expect(statsSection).toBeVisible({ timeout: 10000 });
  });

  test('should have logout option', async ({ page }) => {
    // Procurar opção de logout
    const userMenu = page.locator('[class*="user"], [class*="avatar"], [class*="profile"]').first();
    if (await userMenu.count() > 0) {
      await userMenu.click();
    }
    
    const logoutBtn = page.getByRole('button', { name: /sair|logout/i }).or(
      page.getByRole('link', { name: /sair|logout/i })
    );
    await expect(logoutBtn).toBeVisible({ timeout: 5000 });
  });
});

test.describe('Dashboard Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('should navigate to appointments page', async ({ page }) => {
    const appointmentsLink = page.getByRole('link', { name: /consulta|appointment|agenda/i });
    if (await appointmentsLink.count() > 0) {
      await appointmentsLink.click();
      await expect(page).toHaveURL(/consulta|appointment|agenda/);
    }
  });

  test('should navigate to profile page', async ({ page }) => {
    const profileLink = page.getByRole('link', { name: /perfil|profile|config/i });
    if (await profileLink.count() > 0) {
      await profileLink.click();
      await expect(page).toHaveURL(/perfil|profile|config/);
    }
  });
});
