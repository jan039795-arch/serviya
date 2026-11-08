import { expect, Page, test } from '@playwright/test';
import { haceDias, PNG_FICTICIO } from './apoyo/archivos';

async function entrarComoPrestador(page: Page): Promise<void> {
  await page.goto('/');
  await page.getByTestId('selector-rol').getByRole('combobox').selectOption('prestador');
  await expect(page.getByTestId('selector-rol').getByRole('combobox')).toHaveValue('prestador');
}

async function continuar(page: Page, siguiente: string): Promise<void> {
  await page.getByTestId('continuar').click();
  await expect(page).toHaveURL(new RegExp(`/alta/${siguiente}$`));
}

async function subirDocumento(page: Page, tipo: string): Promise<void> {
  const tarjeta = page.getByTestId(`documento-${tipo}`);
  await tarjeta.locator('input[type="file"]').setInputFiles(PNG_FICTICIO);
  await expect(tarjeta.getByText('✓ Subido')).toBeVisible();
}

test('alta completa: pasos con guardado, interrupción, documentos ficticios y envío', async ({
  page,
}) => {
  await entrarComoPrestador(page);
  await page.goto('/alta');
  await expect(page).toHaveURL(/\/alta\/1$/);

  await page.getByTestId('nombre').fill('Pablo Ficticio Ruiz');
  await expect(page.getByText('Así te verán los clientes: Pablo F.')).toBeVisible();
  await page.getByTestId('fecha-nacimiento').fill('1988-02-03');
  await page.getByTestId('telefono').fill('55 1234 0000');
  await page.getByTestId('direccion').fill('Calle Inventada 1');
  await page.getByTestId('colonia').fill('Centro');
  await continuar(page, '2');

  await page.getByTestId('oficio-1').check();
  await page.getByTestId('anios-1').fill('7');
  await continuar(page, '3');

  await page.getByTestId('minimo-0').fill('300');
  await page.getByTestId('maximo-0').fill('600');
  await continuar(page, '4');

  await page.getByTestId('municipio-0').check();
  await continuar(page, '5');

  // Se interrumpe el alta y se vuelve más tarde: retoma en el paso 5 (US1, escenario 2).
  await page.goto('/');
  await page.goto('/alta');
  await expect(page).toHaveURL(/\/alta\/5$/);
  await page.goto('/alta/1');
  await expect(page.getByTestId('telefono')).toHaveValue('5512340000');
  await page.goto('/alta/5');

  await page.getByTestId('dia-1').check();
  await continuar(page, '6');
  await page.getByTestId('descripcion').fill('Reparo fugas y cambio llaves.');
  await continuar(page, '7');

  await page.getByTestId('acepta-consentimiento').check();
  await continuar(page, '8');

  await subirDocumento(page, 'IneFrente');
  await subirDocumento(page, 'IneReverso');
  await subirDocumento(page, 'Selfie');
  await page.getByTestId('fecha-ComprobanteDomicilio').fill(haceDias(20));
  await subirDocumento(page, 'ComprobanteDomicilio');
  await continuar(page, '9');
  await continuar(page, '10');

  await page.getByTestId('enviar-revision').click();
  await expect(page).toHaveURL(/\/alta\/estado$/);
  await expect(page.getByTestId('titulo-estado')).toHaveText('Estamos revisando tu perfil');
});

test('enviar sin datos muestra la lista de faltantes', async ({ page }) => {
  await entrarComoPrestador(page);
  await page.goto('/alta/10');
  await page.getByTestId('enviar-revision').click();
  const error = page.getByTestId('error-envio');
  await expect(error).toContainText('INE (frente)');
  await expect(error).toContainText('Consentimiento para datos sensibles');
});

test('un comprobante de más de 3 meses se rechaza antes de subirlo', async ({ page }) => {
  await entrarComoPrestador(page);
  await page.goto('/alta/7');
  await page.getByTestId('acepta-consentimiento').check();
  await continuar(page, '8');
  const tarjeta = page.getByTestId('documento-ComprobanteDomicilio');
  await page.getByTestId('fecha-ComprobanteDomicilio').fill(haceDias(120));
  await tarjeta.locator('input[type="file"]').setInputFiles(PNG_FICTICIO);
  await expect(tarjeta.getByText('El comprobante debe tener máximo 3 meses.')).toBeVisible();
});

test('un menor de edad no puede enviar su perfil a revisión', async ({ page }) => {
  await entrarComoPrestador(page);
  await page.goto('/alta/1');
  await page.getByTestId('nombre').fill('Joven Ficticio Pérez');
  await page.getByTestId('fecha-nacimiento').fill(haceDias(365 * 16));
  await expect(page.getByTestId('aviso-menor')).toBeVisible();
  await page.getByTestId('telefono').fill('5512340000');
  await page.getByTestId('direccion').fill('Calle Inventada 2');
  await page.getByTestId('colonia').fill('Centro');
  await continuar(page, '2');
  await page.goto('/alta/10');
  await page.getByTestId('enviar-revision').click();
  await expect(page.getByTestId('error-envio')).toContainText('18 años');
});

test('un cliente sin rol de prestador no entra al alta', async ({ page }) => {
  await page.goto('/');
  await page.getByTestId('selector-rol').getByRole('combobox').selectOption('cliente');
  await page.goto('/alta');
  await expect(page.getByTestId('sin-permiso')).toBeVisible();
});
