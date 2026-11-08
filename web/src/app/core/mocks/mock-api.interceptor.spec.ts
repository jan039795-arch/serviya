import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { Entorno } from '../../../environments/entorno';
import { ENTORNO } from '../entorno';
import { LATENCIA_SIMULADA, mockApiInterceptor, RUTAS_SIMULADAS } from './mock-api.interceptor';
import { coincidir, ok, problema, ruta, rutaRelativa } from './simulacion';

const entornoDePrueba = (busqueda: boolean): Entorno => ({
  produccion: false,
  apiUrl: '/api/v1',
  mocks: { cuentas: false, prestadores: false, busqueda, accesos: false, admin: false },
});

const rutasDePrueba = [
  ruta('busqueda', 'GET', '/prestadores/:id', ({ parametros, consulta }) =>
    ok({ id: parametros['id'], pagina: consulta.get('pagina') }),
  ),
  ruta('busqueda', 'POST', '/prestadores', () =>
    problema(422, 'Datos incompletos.', { errors: { nombre: ['Escribe tu nombre.'] } }),
  ),
];

function configurar(busquedaSimulada: boolean): {
  http: HttpClient;
  red: HttpTestingController;
} {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([mockApiInterceptor])),
      provideHttpClientTesting(),
      { provide: ENTORNO, useValue: entornoDePrueba(busquedaSimulada) },
      { provide: RUTAS_SIMULADAS, useValue: () => Promise.resolve(rutasDePrueba) },
      { provide: LATENCIA_SIMULADA, useValue: 0 },
    ],
  });
  return { http: TestBed.inject(HttpClient), red: TestBed.inject(HttpTestingController) };
}

describe('mockApiInterceptor', () => {
  it('responde con el manejador de la ruta y le pasa parámetros y consulta', async () => {
    const { http } = configurar(true);

    const respuesta = await firstValueFrom(
      http.get('/api/v1/prestadores/abc', { params: { pagina: '2' } }),
    );

    expect(respuesta).toEqual({ id: 'abc', pagina: '2' });
  });

  it('convierte las respuestas de error en HttpErrorResponse con ProblemDetails', async () => {
    const { http } = configurar(true);

    const error = await firstValueFrom(http.post('/api/v1/prestadores', {})).catch(
      (e: unknown) => e,
    );

    expect(error).toBeInstanceOf(HttpErrorResponse);
    const httpError = error as HttpErrorResponse;
    expect(httpError.status).toBe(422);
    expect(httpError.error.errors.nombre).toEqual(['Escribe tu nombre.']);
  });

  it('deja pasar a la API real las áreas que no están simuladas', async () => {
    const { http, red } = configurar(false);

    const respuesta = firstValueFrom(http.get('/api/v1/prestadores/abc'));
    await Promise.resolve();
    red.expectOne('/api/v1/prestadores/abc').flush({ real: true });

    expect(await respuesta).toEqual({ real: true });
    red.verify();
  });

  it('deja pasar las URL que no son de la API', async () => {
    const { http, red } = configurar(true);

    const respuesta = firstValueFrom(http.get('/assets/datos.json'));
    red.expectOne('/assets/datos.json').flush({ archivo: true });

    expect(await respuesta).toEqual({ archivo: true });
  });
});

describe('coincidir', () => {
  it('extrae los parámetros de la ruta', () => {
    expect(coincidir('/prestadores/:id/resenas', '/prestadores/x%20y/resenas')).toEqual({
      id: 'x y',
    });
  });

  it('devuelve null si la ruta no coincide', () => {
    expect(coincidir('/prestadores/:id', '/prestadores/abc/resenas')).toBeNull();
    expect(coincidir('/accesos/yo', '/accesos/opciones')).toBeNull();
  });
});

describe('rutaRelativa', () => {
  it('quita la base de la API y la cadena de consulta', () => {
    expect(rutaRelativa('/api/v1/prestadores?pagina=1', '/api/v1')).toBe('/prestadores');
  });

  it('ignora las URL que solo empiezan parecido', () => {
    expect(rutaRelativa('/api/v10/prestadores', '/api/v1')).toBeNull();
    expect(rutaRelativa('/otra/ruta', '/api/v1')).toBeNull();
  });
});
