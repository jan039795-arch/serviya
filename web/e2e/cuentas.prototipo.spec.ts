import { expect, test } from '@playwright/test';

test.describe('cuentas en el prototipo', () => {
  test('un formulario vacío muestra los errores sin enviar', async ({ page }) => {
    await page.goto('/registro');
    await page.getByTestId('crear-cuenta').click();
    await expect(page.getByText('Escribe tu correo.')).toBeVisible();
    await expect(
      page.getByText('Para crear tu cuenta, acepta el aviso y los términos.'),
    ).toBeVisible();
    await expect(page).toHaveURL(/\/registro$/);
  });

  test('registro de cliente, verificación con código y sesión iniciada', async ({ page }) => {
    const correo = `cliente.${Date.now()}@ejemplo.test`;
    await page.goto('/registro');
    await page.getByTestId('nombre').fill('Laura Ficticia Gómez');
    await page.getByTestId('email').fill(correo);
    await page.getByTestId('password').fill('contrasena-de-prueba');
    await page.getByTestId('fecha-nacimiento').fill('1992-05-20');
    await page.getByTestId('acepta').check();
    await page.getByTestId('crear-cuenta').click();

    await expect(page).toHaveURL(/\/verificar$/);
    await page.getByTestId('codigo').fill('123456');
    await page.getByTestId('verificar').click();
    await expect(page).toHaveURL(/\/$/);

    await page.getByTestId('nav-cuenta').click();
    await expect(page).toHaveURL(/\/cuenta$/);
  });

  test('registro de prestador desde ?rol=prestador lleva al alta', async ({ page }) => {
    await page.goto('/registro?rol=prestador');
    await expect(page.getByRole('radio', { name: 'Quiero ofrecer mis servicios' })).toBeChecked();
    await page.getByTestId('nombre').fill('Pablo Ficticio Ruiz');
    await page.getByTestId('email').fill(`prestador.${Date.now()}@ejemplo.test`);
    await page.getByTestId('password').fill('contrasena-de-prueba');
    await page.getByTestId('fecha-nacimiento').fill('1988-02-03');
    await page.getByTestId('acepta').check();
    await page.getByTestId('crear-cuenta').click();
    await page.getByTestId('codigo').fill('123456');
    await page.getByTestId('verificar').click();
    await expect(page).toHaveURL(/\/alta/);
  });

  test('menor de edad: puede registrarse pero ve el aviso', async ({ page }) => {
    await page.goto('/registro');
    await page.getByTestId('fecha-nacimiento').fill('2015-01-01');
    await expect(page.getByTestId('aviso-menor')).toBeVisible();
  });

  test('entrar regresa a la ruta pedida y rechaza destinos externos', async ({ page }) => {
    await page.goto('/entrar?volver=%2Fcuenta');
    await page.getByTestId('email').fill('cliente@ejemplo.test');
    await page.getByTestId('password').fill('cualquier-cosa');
    await page.getByTestId('entrar').click();
    await expect(page).toHaveURL(/\/cuenta$/);

    await page.goto('/entrar?volver=https%3A%2F%2Fotro.sitio');
    await page.getByTestId('email').fill('cliente@ejemplo.test');
    await page.getByTestId('password').fill('cualquier-cosa');
    await page.getByTestId('entrar').click();
    await expect(page).toHaveURL('http://localhost:4200/');
  });
});
