import { expect, test } from '@playwright/test';
import { verComo } from './apoyo/prototipo';

/**
 * Recorrido del prototipo (T035). Los puntos de la Etapa 2 de `quickstart.md` están cubiertos así:
 *
 * 1. Alta en 10 pasos, interrumpida y retomada → `alta-prestador.prototipo.spec.ts`.
 * 2. Búsqueda y perfil sin teléfono, correo ni dirección → `busqueda.prototipo.spec.ts`.
 * 3. "Contactar" → registro → opciones de $39 y $129 con el aviso → `pago-contacto.prototipo.spec.ts`.
 * 4. Cola del administrador con aprobación y motivo → `admin-revision.prototipo.spec.ts`.
 * 5. Instalación como PWA → prueba manual en el celular (quickstart, "Probar en el celular").
 *
 * Este archivo cubre "Mi cuenta" y la navegación general.
 */

test('Mi cuenta lleva a contactos, pase y al alta de prestador', async ({ page }) => {
  await verComo(page, 'cliente');
  await page.getByTestId('nav-cuenta').click();
  await expect(page.getByTestId('datos-cuenta')).toContainText('cliente@ejemplo.test');
  await expect(page.getByTestId('ir-admin')).toHaveCount(0);

  await page.getByTestId('ir-mis-contactos').click();
  await expect(page).toHaveURL(/\/mis-contactos$/);
  await page.goBack();
  await page.getByTestId('ir-pase').click();
  await expect(page).toHaveURL(/\/pase$/);
  await page.goBack();

  // Un cliente agrega el rol de prestador con la misma cuenta (FR-002).
  await page.getByTestId('ser-prestador').click();
  await expect(page).toHaveURL(/\/alta\/1$/);
  await page.getByTestId('nav-cuenta').click();
  await expect(page.getByTestId('ir-perfil-prestador')).toBeVisible();
  await expect(page.getByTestId('ser-prestador')).toHaveCount(0);
});

test('el administrador ve el acceso a la administración en Mi cuenta', async ({ page }) => {
  await verComo(page, 'admin');
  await page.goto('/cuenta');
  await page.getByTestId('ir-admin').click();
  await expect(page).toHaveURL(/\/admin\/revisiones$/);
});

test('cerrar sesión regresa al inicio como visitante', async ({ page }) => {
  await verComo(page, 'cliente');
  await page.goto('/cuenta');
  await page.getByTestId('cerrar-sesion').click();
  await expect(page).toHaveURL('http://localhost:4200/');
  await expect(page.getByTestId('selector-rol')).toHaveAttribute('data-perfil', 'visitante');
  await page.getByTestId('nav-cuenta').click();
  await expect(page).toHaveURL(/\/entrar\?volver=%2Fcuenta$/);
});

test('una ruta que no existe muestra la página de no encontrada', async ({ page }) => {
  await page.goto('/esta-ruta-no-existe');
  await expect(page.getByRole('heading', { name: 'No encontramos esta página' })).toBeVisible();
});

test('"Reiniciar datos" borra la sesión y los datos simulados', async ({ page }) => {
  await verComo(page, 'cliente');
  page.once('dialog', (dialogo) => void dialogo.accept());
  await page.getByTestId('reiniciar-prototipo').click();
  await expect(page).toHaveURL('http://localhost:4200/');
  await expect(page.getByTestId('selector-rol')).toHaveAttribute('data-perfil', 'visitante');
});

test('al pedir sesión se recuerda la URL completa, con su consulta', async ({ page }) => {
  await page.goto('/pago/resultado?pagoId=abc');
  await expect(page).toHaveURL(/\/entrar\?volver=%2Fpago%2Fresultado%3FpagoId%3Dabc$/);
});
