import { expect, Page, test } from '@playwright/test';
import { haceDias } from './apoyo/archivos';
import { verComo } from './apoyo/prototipo';

const JUAN = 'c0000000-0000-7000-8000-000000000001';
const MARIA = 'c0000000-0000-7000-8000-000000000002';
const ROBERTO = 'c0000000-0000-7000-8000-000000000003';

async function pagar(page: Page, prestadorId: string, metodo: 'tarjeta' | 'oxxo' | 'rechazado') {
  await page.goto(`/contactar/${prestadorId}`);
  await page.getByTestId('pagar-contacto').click();
  await expect(page).toHaveURL(/\/pago\/simulado\?pagoId=/);
  await page.getByTestId(`pagar-${metodo}`).click();
  await expect(page).toHaveURL(/\/pago\/resultado\?pagoId=/);
}

test('un visitante que pulsa "Contactar" se registra y regresa a las opciones (US3, esc. 1 y 2)', async ({
  page,
}) => {
  await page.goto(`/prestador/${JUAN}`);
  await page.getByTestId('contactar').click();
  await expect(page).toHaveURL(/\/entrar\?volver=%2Fcontactar%2F/);

  await page.getByRole('link', { name: 'Crea una gratis' }).click();
  await page.getByTestId('nombre').fill('Laura Ficticia Gómez');
  await page.getByTestId('email').fill(`laura.${Date.now()}@ejemplo.test`);
  await page.getByTestId('password').fill('contrasena-de-prueba');
  await page.getByTestId('fecha-nacimiento').fill('1992-05-20');
  await page.getByTestId('acepta').check();
  await page.getByTestId('crear-cuenta').click();
  await page.getByTestId('codigo').fill('123456');
  await page.getByTestId('verificar').click();

  await expect(page).toHaveURL(new RegExp(`/contactar/${JUAN}$`));
  await expect(page.getByTestId('aviso-pago')).toContainText(
    'Pagas por acceso y contacto, no por el trabajo. No garantiza la contratación.',
  );
  await expect(page.getByTestId('opcion-individual')).toContainText('$39.00 MXN');
  await expect(page.getByTestId('opcion-individual')).toContainText('48 horas');
  await expect(page.getByTestId('opcion-pase')).toContainText('$129.00 MXN');
});

test('pago con tarjeta: teléfono al instante, sin doble cobro y solo lectura al vencer (US3, esc. 3 y 6)', async ({
  page,
}) => {
  await verComo(page, 'cliente');
  await pagar(page, JUAN, 'tarjeta');
  await expect(page.getByTestId('resultado-titulo')).toHaveText('¡Pago aprobado!');

  await page.getByTestId('ver-contactos').click();
  const acceso = page.getByTestId(`acceso-${JUAN}`);
  await acceso.getByTestId('ver-telefono').click();
  await expect(acceso.getByTestId('telefono')).toHaveText('+52 55 0000 0001');

  await page.goto(`/contactar/${JUAN}`);
  await expect(page.getByTestId('acceso-vigente')).toContainText('No se te cobra de nuevo');
  await expect(page.getByTestId('pagar-contacto')).toHaveCount(0);

  await page.goto('/mis-contactos');
  await page.getByTestId(`acceso-${JUAN}`).getByTestId('simular-vencimiento').click();
  await expect(page.getByTestId(`acceso-${JUAN}`)).toContainText('Vencido');
  await expect(page.getByTestId(`acceso-${JUAN}`).getByTestId('telefono')).toHaveCount(0);
});

test('pago en OXXO: queda pendiente y se activa al acreditarse (US3, esc. 4)', async ({ page }) => {
  test.slow();
  test.slow();
  await verComo(page, 'cliente');
  await pagar(page, MARIA, 'oxxo');
  await expect(page.getByTestId('resultado-titulo')).toHaveText('Pago pendiente');

  await page.goto('/mis-contactos');
  const acceso = page.getByTestId(`acceso-${MARIA}`);
  await expect(acceso).toContainText('Pago pendiente');
  await expect(acceso.getByTestId('ver-telefono')).toHaveCount(0);

  await acceso.getByTestId('simular-acreditacion').click();
  await expect(acceso).toContainText('Activo');
  await acceso.getByTestId('ver-telefono').click();
  await expect(acceso.getByTestId('telefono')).toBeVisible();
});

test('pago rechazado: no se cobra y se puede intentar de nuevo', async ({ page }) => {
  await verComo(page, 'cliente');
  await pagar(page, ROBERTO, 'rechazado');
  await expect(page.getByTestId('resultado-titulo')).toHaveText('No se completó el pago');
  await page.goto(`/contactar/${ROBERTO}`);
  await expect(page.getByTestId('pagar-contacto')).toBeVisible();
});

test('el crédito del cliente desbloquea un contacto gratis', async ({ page }) => {
  await verComo(page, 'cliente');
  await page.goto(`/contactar/${ROBERTO}`);
  await page.getByTestId('usar-credito').click();
  await expect(page).toHaveURL(/\/mis-contactos$/);
  await expect(page.getByTestId(`acceso-${ROBERTO}`)).toContainText('Activo');
});

test('pase con descuento, uso de un contacto y cancelación en un clic (US4)', async ({ page }) => {
  test.slow();
  await verComo(page, 'cliente');
  await pagar(page, JUAN, 'tarjeta');

  // Segundo contacto en 30 días: se ofrece el pase por $129 − $39 (US4, escenario 3).
  await page.goto(`/contactar/${MARIA}`);
  await expect(page.getByTestId('oferta-pase')).toContainText('$39.00 MXN');
  await page.getByTestId('renovacion').check();
  await expect(page.getByTestId('comprar-pase')).toHaveText(/Comprar pase por \$90\.00 MXN/);
  await page.getByTestId('comprar-pase').click();
  await page.getByTestId('pagar-tarjeta').click();
  await expect(page.getByTestId('resultado-titulo')).toHaveText('¡Pago aprobado!');

  await page.goto(`/contactar/${MARIA}`);
  await page.getByTestId('usar-pase').click();
  await expect(page).toHaveURL(/\/mis-contactos$/);

  await page.goto('/pase');
  await expect(page.getByTestId('contactos-disponibles')).toContainText('29 de 30');
  await page.getByTestId('cancelar-renovacion').click();
  await expect(page.getByTestId('renovacion-cancelada')).toContainText('No te volveremos a cobrar');
});

test('un menor de edad no puede pagar contactos (FR-004)', async ({ page }) => {
  await page.goto('/registro');
  await page.getByTestId('nombre').fill('Joven Ficticio Pérez');
  await page.getByTestId('email').fill(`joven.${Date.now()}@ejemplo.test`);
  await page.getByTestId('password').fill('contrasena-de-prueba');
  await page.getByTestId('fecha-nacimiento').fill(haceDias(365 * 16));
  await page.getByTestId('acepta').check();
  await page.getByTestId('crear-cuenta').click();
  await page.getByTestId('codigo').fill('123456');
  await page.getByTestId('verificar').click();
  // Esperar a que termine la verificación (inicia la sesión) antes de navegar.
  await expect(page).toHaveURL('http://localhost:4200/');

  await page.goto(`/contactar/${JUAN}`);
  await expect(page.getByTestId('menor-de-edad')).toBeVisible();
  await expect(page.getByTestId('pagar-contacto')).toHaveCount(0);
});
