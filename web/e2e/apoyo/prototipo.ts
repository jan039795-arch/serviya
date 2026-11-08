import { expect, Page } from '@playwright/test';
import { haceDias, PNG_FICTICIO } from './archivos';

export type PerfilPrototipo = 'visitante' | 'cliente' | 'prestador' | 'admin';

/** Cambia el rol con el selector del prototipo y espera a que la sesión se actualice. */
export async function verComo(page: Page, perfil: PerfilPrototipo): Promise<void> {
  if (page.url() === 'about:blank') {
    await page.goto('/');
  }
  const selector = page.getByTestId('selector-rol');
  await selector.getByRole('combobox').selectOption(perfil);
  // El valor del <select> cambia al instante; la sesión simulada tarda un poco más.
  await expect(selector).toHaveAttribute('data-perfil', perfil);
}

async function continuar(page: Page, siguiente: string): Promise<void> {
  await page.getByTestId('continuar').click();
  await expect(page).toHaveURL(new RegExp(`/alta/${siguiente}$`));
}

export async function subirDocumento(page: Page, tipo: string): Promise<void> {
  const tarjeta = page.getByTestId(`documento-${tipo}`);
  await tarjeta.locator('input[type="file"]').setInputFiles(PNG_FICTICIO);
  await expect(tarjeta.getByText('✓ Subido')).toBeVisible();
}

/** Completa el alta del prestador de prueba con datos y documentos ficticios y la envía. */
export async function completarAlta(page: Page, nombre = 'Pablo Ficticio Ruiz'): Promise<void> {
  await verComo(page, 'prestador');
  await page.goto('/alta/1');
  await page.getByTestId('nombre').fill(nombre);
  await page.getByTestId('fecha-nacimiento').fill('1988-02-03');
  await page.getByTestId('telefono').fill('5512340000');
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
  await page.getByTestId('dia-1').check();
  await continuar(page, '6');
  await continuar(page, '7');
  await page.getByTestId('acepta-consentimiento').check();
  await continuar(page, '8');
  for (const tipo of ['IneFrente', 'IneReverso', 'Selfie']) {
    await subirDocumento(page, tipo);
  }
  await page.getByTestId('fecha-ComprobanteDomicilio').fill(haceDias(20));
  await subirDocumento(page, 'ComprobanteDomicilio');
  await continuar(page, '9');
  await continuar(page, '10');
  await page.getByTestId('enviar-revision').click();
  await expect(page.getByTestId('titulo-estado')).toHaveText('Estamos revisando tu perfil');
}
