import { expect, Page, test } from '@playwright/test';
import { completarAlta, subirDocumento, verComo } from './apoyo/prototipo';

const TEXTO_ESTADO: Readonly<Record<string, string>> = {
  Aprobar: 'Aprobado',
  RequiereCorreccion: 'Requiere corrección',
  Rechazar: 'Rechazado',
};

async function abrirRevision(page: Page, nombre: string): Promise<void> {
  await page.goto('/admin');
  await expect(page).toHaveURL(/\/admin\/revisiones$/);
  await page
    .getByTestId('cola-revisiones')
    .getByRole('link', { name: new RegExp(nombre) })
    .click();
  await expect(page.getByTestId('titulo-revision')).toHaveText(nombre);
}

async function decidirDocumento(
  page: Page,
  tipo: string,
  decision: string,
  motivo?: string,
): Promise<void> {
  const tarjeta = page.getByTestId(`revision-${tipo}`);
  await tarjeta.getByTestId(`decision-${decision}`).click();
  if (motivo) {
    await tarjeta.getByTestId('motivo').fill(motivo);
    await tarjeta.getByTestId('confirmar-decision').click();
  }
  await expect(tarjeta.locator('.estado')).toHaveText(TEXTO_ESTADO[decision]);
}

test('la cola muestra los perfiles ficticios y el visor registra la consulta', async ({ page }) => {
  await verComo(page, 'admin');
  await abrirRevision(page, 'Héctor N.');

  const ine = page.getByTestId('revision-IneFrente');
  await ine.getByTestId('ver-documento').click();
  await expect(ine.getByTestId('visor-documento')).toBeVisible();
  await expect(ine.getByText('Esta consulta quedó registrada.')).toBeVisible();

  const decisionPerfil = page.getByTestId('decision-perfil');
  await expect(decisionPerfil.getByTestId('decision-Aprobar')).toBeDisabled();
  await expect(decisionPerfil.getByTestId('decision-RequiereCorreccion')).toBeDisabled();

  // El motivo es obligatorio al pedir corrección (FR-010).
  await ine.getByTestId('decision-RequiereCorreccion').click();
  await ine.getByTestId('confirmar-decision').click();
  await expect(ine.getByText('Escribe el motivo.')).toBeVisible();
});

test('US1 de punta a punta: corrección de la INE, reenvío y aprobación', async ({ page }) => {
  test.slow();
  await completarAlta(page);

  await verComo(page, 'admin');
  await abrirRevision(page, 'Pablo F.');
  await decidirDocumento(page, 'IneFrente', 'RequiereCorreccion', 'Foto borrosa');
  for (const tipo of ['IneReverso', 'Selfie', 'ComprobanteDomicilio']) {
    await decidirDocumento(page, tipo, 'Aprobar');
  }
  const decisionPerfil = page.getByTestId('decision-perfil');
  await decisionPerfil.getByTestId('decision-RequiereCorreccion').click();
  await decisionPerfil
    .getByTestId('motivo')
    .fill('Vuelve a tomar la foto de tu INE con buena luz.');
  await decisionPerfil.getByTestId('confirmar-decision').click();
  await expect(page).toHaveURL(/\/admin\/revisiones$/);

  // El prestador ve el motivo y solo vuelve a subir la INE (US1, escenario 4).
  await verComo(page, 'prestador');
  await page.goto('/alta/estado');
  await expect(page.getByTestId('titulo-estado')).toHaveText('Necesitamos que corrijas algo');
  await expect(page.getByText('Necesitamos que lo subas de nuevo: Foto borrosa')).toBeVisible();
  await expect(page.getByTestId('documento-Selfie')).toHaveCount(0);
  await subirDocumento(page, 'IneFrente');
  await page.getByTestId('reenviar').click();
  await expect(page.getByTestId('titulo-estado')).toHaveText('Estamos revisando tu perfil');

  // El administrador aprueba la INE corregida y publica el perfil (US1, escenario 5).
  await verComo(page, 'admin');
  await page.goto('/admin/revisiones');
  await expect(page.getByTestId('cola-revisiones').getByText('Corrección reenviada')).toBeVisible();
  await abrirRevision(page, 'Pablo F.');
  await decidirDocumento(page, 'IneFrente', 'Aprobar');
  await page.getByTestId('decision-perfil').getByTestId('decision-Aprobar').click();
  await expect(page.getByText('Perfil aprobado y publicado.')).toBeVisible();

  await verComo(page, 'prestador');
  await page.goto('/alta/estado');
  await expect(page.getByTestId('titulo-estado')).toHaveText('¡Tu perfil está publicado!');
});

test('un prestador no entra a la administración', async ({ page }) => {
  await verComo(page, 'prestador');
  await page.goto('/admin/revisiones');
  await expect(page.getByTestId('sin-permiso')).toBeVisible();
});
