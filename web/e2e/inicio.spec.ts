import { expect, test } from '@playwright/test';
import { verComo } from './apoyo/prototipo';

test('la página de inicio carga en español con la navegación principal', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('html')).toHaveAttribute('lang', 'es-MX');
  await expect(page.getByTestId('titulo-app')).toHaveText('ServiYa');
  await expect(page.getByRole('navigation', { name: 'Principal' }).getByRole('link')).toHaveCount(
    3,
  );
});

test('sin sesión, "Mi cuenta" pide entrar y recuerda a dónde volver', async ({ page }) => {
  await page.goto('/');
  await page.getByTestId('nav-cuenta').click();
  await expect(page).toHaveURL(/\/entrar\?volver=%2Fcuenta$/);
});

test('el selector del prototipo cambia el rol y protege la administración', async ({ page }) => {
  await page.goto('/admin');
  await expect(page).toHaveURL(/\/entrar/);

  await verComo(page, 'cliente');
  await page.goto('/admin');
  await expect(page.getByTestId('sin-permiso')).toBeVisible();

  await verComo(page, 'admin');
  await page.goto('/admin');
  await expect(page.getByRole('heading', { name: 'Perfiles por revisar' })).toBeVisible();
});
