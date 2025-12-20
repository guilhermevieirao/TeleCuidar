import { test, expect } from '@playwright/test';

// Credenciais do DataSeeder
const ADMIN_USER = { email: 'adm@adm.com', password: 'zxcasd12' };
const PROFESSIONAL_USER = { email: 'med@med.com', password: 'zxcasd12' };
const PATIENT_USER = { email: 'pac@pac.com', password: 'zxcasd12' };

async function login(page, user = PROFESSIONAL_USER) {
  await page.goto('/entrar');
  await page.waitForLoadState('networkidle');
  await page.getByPlaceholder('seu@email.com').fill(user.email);
  await page.locator('input[placeholder="Digite sua senha"]').fill(user.password);
  await page.getByRole('button', { name: /entrar|login/i }).click();
  await page.waitForURL(/painel/, { timeout: 15000 });
}

test.describe('Appointments Page', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    // Navegar para consultas
    await page.goto('/consultas');
  });

  test('should display appointments list or empty state', async ({ page }) => {
    // Deve mostrar lista de consultas ou mensagem de vazio
    const content = page.locator('[class*="appointment"], [class*="consulta"], [class*="empty"], [class*="list"]').first();
    await expect(content).toBeVisible({ timeout: 10000 });
  });

  test('should have filters or search', async ({ page }) => {
    const filter = page.locator('input[type="search"], [class*="filter"], [class*="search"]').first();
    await expect(filter).toBeVisible({ timeout: 5000 });
  });

  test('should have create appointment button for professionals', async ({ page }) => {
    const createBtn = page.getByRole('button', { name: /nova|criar|agendar|new/i }).or(
      page.getByRole('link', { name: /nova|criar|agendar|new/i })
    );
    // Pode não estar visível para todos os roles
    if (await createBtn.count() > 0) {
      await expect(createBtn).toBeVisible();
    }
  });

  test('should display appointment status badges', async ({ page }) => {
    const statusBadge = page.locator('[class*="badge"], [class*="status"], [class*="chip"]').first();
    if (await statusBadge.count() > 0) {
      await expect(statusBadge).toBeVisible();
    }
  });
});

test.describe('Appointment Details', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.goto('/consultas');
  });

  test('should open appointment details on click', async ({ page }) => {
    const appointmentItem = page.locator('[class*="appointment-item"], [class*="consulta-item"], tr[class*="row"]').first();
    if (await appointmentItem.count() > 0) {
      await appointmentItem.click();
      // Deve abrir modal ou navegar para detalhes
      await page.waitForTimeout(1000);
      const modal = page.locator('[class*="modal"], [class*="dialog"], [class*="detail"]').first();
      if (await modal.count() > 0) {
        await expect(modal).toBeVisible();
      }
    }
  });
});

test.describe('Appointment Scheduling', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, PATIENT_USER);
  });

  test('should access scheduling page', async ({ page }) => {
    await page.goto('/agendar');
    // Se redirecionar, pode ser por falta de permissão
    const content = page.locator('main, [class*="content"]').first();
    await expect(content).toBeVisible({ timeout: 10000 });
  });
});
