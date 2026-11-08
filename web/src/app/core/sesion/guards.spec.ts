import { EnvironmentInjector, runInInjectionContext } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  PartialMatchRouteSnapshot,
  provideRouter,
  Route,
  Router,
  UrlSegment,
  UrlTree,
} from '@angular/router';
import { Sesion } from '../api/modelos';
import { conRol, conSesion } from './guards';
import { rutaDeRegresoSegura, SesionService } from './sesion';

const cliente: Sesion = {
  id: 'u1',
  nombreCompleto: 'Cliente de Prueba',
  email: 'cliente@ejemplo.test',
  roles: ['Cliente'],
  estado: 'Activo',
  esMayorDeEdad: true,
};

function configurar(cargar: () => Promise<Sesion | null>): void {
  TestBed.configureTestingModule({
    providers: [provideRouter([]), { provide: SesionService, useValue: { cargar } }],
  });
}

const segmentos = [new UrlSegment('cuenta', {}), new UrlSegment('pagos', {})];

async function ejecutar(guard: typeof conSesion): Promise<boolean | UrlTree> {
  const resultado = runInInjectionContext(TestBed.inject(EnvironmentInjector), () =>
    guard({} as Route, segmentos, {} as PartialMatchRouteSnapshot),
  );
  return (await resultado) as boolean | UrlTree;
}

function comoTexto(resultado: boolean | UrlTree): string {
  return typeof resultado === 'boolean'
    ? String(resultado)
    : TestBed.inject(Router).serializeUrl(resultado);
}

describe('conSesion', () => {
  it('deja pasar con sesión', async () => {
    configurar(() => Promise.resolve(cliente));

    expect(await ejecutar(conSesion)).toBe(true);
  });

  it('sin sesión manda a entrar con la ruta de regreso', async () => {
    configurar(() => Promise.resolve(null));

    expect(comoTexto(await ejecutar(conSesion))).toBe('/entrar?volver=%2Fcuenta%2Fpagos');
  });

  it('si falla la carga trata al usuario como visitante', async () => {
    configurar(() => Promise.reject(new Error('sin red')));

    expect(comoTexto(await ejecutar(conSesion))).toBe('/entrar?volver=%2Fcuenta%2Fpagos');
  });
});

describe('conRol', () => {
  it('deja pasar con el rol pedido', async () => {
    configurar(() => Promise.resolve(cliente));

    expect(await ejecutar(conRol('Cliente'))).toBe(true);
  });

  it('con otro rol muestra "No tienes permiso"', async () => {
    configurar(() => Promise.resolve(cliente));

    expect(comoTexto(await ejecutar(conRol('Admin')))).toBe('/sin-permiso');
  });

  it('sin sesión manda a entrar', async () => {
    configurar(() => Promise.resolve(null));

    expect(comoTexto(await ejecutar(conRol('Admin')))).toBe('/entrar?volver=%2Fcuenta%2Fpagos');
  });
});

describe('rutaDeRegresoSegura', () => {
  it('acepta rutas internas', () => {
    expect(rutaDeRegresoSegura('/prestador/abc?x=1')).toBe('/prestador/abc?x=1');
  });

  it('rechaza destinos externos o raros', () => {
    for (const valor of [
      'https://otro.sitio',
      '//otro.sitio',
      '/\\otro.sitio',
      'javascript:alert(1)',
      '',
      null,
    ]) {
      expect(rutaDeRegresoSegura(valor)).toBe('/');
    }
  });
});
