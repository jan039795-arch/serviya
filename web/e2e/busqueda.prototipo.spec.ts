import { expect, test } from '@playwright/test';
import { completarAlta, verComo } from './apoyo/prototipo';

const JUAN = 'c0000000-0000-7000-8000-000000000001';
const HECTOR_EN_REVISION = 'c0000000-0000-7000-8000-000000000013';
const IVAN_SUSPENDIDO = 'c0000000-0000-7000-8000-000000000014';
const VALLE_ALTO = 'b0000000-0000-7000-8000-000000000001';

/** Teléfonos, correos o direcciones que nunca deben verse en una página pública (SC-006). */
const DATOS_PRIVADOS = /\+52|\b55\s?\d{4}\s?\d{4}\b|@|Calle Inventada|Avenida Imaginaria/;

test('buscar un oficio en un municipio muestra solo aprobados que cubren la zona', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByTestId('oficio').selectOption({ label: 'Fontanero' });
  await page.getByTestId('municipio').selectOption({ label: 'Valle Alto' });
  await page.getByTestId('buscar').click();

  await expect(page).toHaveURL(new RegExp(`/buscar\\?oficio=fontanero&municipio=${VALLE_ALTO}$`));
  await expect(page.getByRole('heading', { name: 'Fontanero en Valle Alto' })).toBeVisible();
  const resultados = page.getByTestId('resultados');
  await expect(resultados).toContainText('Juan P.');
  await expect(resultados).toContainText('Jorge E.');
  await expect(resultados).not.toContainText('Iván C.');
  expect(await page.locator('main').innerText()).not.toMatch(DATOS_PRIVADOS);
});

test('los filtros viven en la URL y se pueden quitar', async ({ page }) => {
  await page.goto('/buscar?oficio=fontanero');
  await page.getByText('Filtrar resultados').click();
  await page.getByTestId('filtro-insignia').selectOption('AntecedentesPresentados');
  await page.getByTestId('aplicar-filtros').click();

  await expect(page).toHaveURL(/insignia=AntecedentesPresentados/);
  await expect(page.getByTestId('total-resultados')).toHaveText('1 prestador');
  await page.getByRole('button', { name: 'Quitar filtros' }).click();
  await expect(page).not.toHaveURL(/insignia=/);
});

test('el perfil público no muestra teléfono, correo, dirección ni documentos', async ({ page }) => {
  await page.goto(`/prestador/${JUAN}`);
  await expect(page.getByTestId('nombre-publico')).toHaveText('Juan P.');
  await expect(page.getByText('Antecedentes presentados')).toBeVisible();
  await expect(page.getByTestId('resenas').locator('li')).toHaveCount(3);
  await expect(page.getByTestId('contactar')).toBeVisible();
  expect(await page.locator('main').innerText()).not.toMatch(DATOS_PRIVADOS);
  await expect(page).toHaveTitle(/Juan P\. · Fontanero en Valle Alto/);
});

test('perfiles en revisión o suspendidos no se abren por enlace directo', async ({ page }) => {
  for (const id of [HECTOR_EN_REVISION, IVAN_SUSPENDIDO]) {
    await page.goto(`/prestador/${id}`);
    await expect(page.getByTestId('perfil-no-disponible')).toBeVisible();
  }
});

test.describe('con permiso de ubicación', () => {
  test.use({ permissions: ['geolocation'], geolocation: { latitude: 19.4, longitude: -99.1 } });

  test('"Usar mi ubicación" elige el municipio', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('usar-ubicacion').click();
    await expect(page.getByText('Te ubicamos en Valle Alto.')).toBeVisible();
    await expect(page.getByTestId('municipio')).toHaveValue(VALLE_ALTO);
  });
});

test('un perfil aprobado en el prototipo aparece en la búsqueda', async ({ page }) => {
  test.slow();
  await completarAlta(page, 'Rita Ficticia Luna');

  await verComo(page, 'admin');
  await page.goto('/admin/revisiones');
  await page
    .getByTestId('cola-revisiones')
    .getByRole('link', { name: /Rita F\./ })
    .click();
  for (const tipo of ['IneFrente', 'IneReverso', 'Selfie', 'ComprobanteDomicilio']) {
    await page.getByTestId(`revision-${tipo}`).getByTestId('decision-Aprobar').click();
    await expect(page.getByTestId(`revision-${tipo}`).locator('.estado')).toHaveText('Aprobado');
  }
  await page.getByTestId('decision-perfil').getByTestId('decision-Aprobar').click();
  await expect(page).toHaveURL(/\/admin\/revisiones$/);

  await verComo(page, 'visitante');
  await page.goto(`/buscar?oficio=fontanero&municipio=${VALLE_ALTO}`);
  await page.getByTestId('resultados').getByText('Rita F.').click();
  await expect(page.getByTestId('nombre-publico')).toHaveText('Rita F.');
  await expect(page.getByText('Identidad verificada')).toBeVisible();
  expect(await page.locator('main').innerText()).not.toMatch(DATOS_PRIVADOS);
});
