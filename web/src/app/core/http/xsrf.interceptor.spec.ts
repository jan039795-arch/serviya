import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
  withNoXsrfProtection,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { Entorno } from '../../../environments/entorno';
import { ENTORNO } from '../entorno';
import {
  ENCABEZADO_XSRF,
  ES_NAVEGADOR,
  esDeLaApi,
  leerCookie,
  xsrfInterceptor,
} from './xsrf.interceptor';

const sinMocks: Entorno['mocks'] = {
  cuentas: false,
  prestadores: false,
  busqueda: false,
  accesos: false,
  admin: false,
};

function configurar(apiUrl = '/api/v1', plataforma = 'browser') {
  TestBed.configureTestingModule({
    providers: [
      // Igual que en app.config: sin el XSRF de Angular, que solo cubre URL relativas.
      provideHttpClient(withNoXsrfProtection(), withInterceptors([xsrfInterceptor])),
      provideHttpClientTesting(),
      {
        provide: ENTORNO,
        useValue: { produccion: false, apiUrl, mocks: sinMocks } satisfies Entorno,
      },
      { provide: ES_NAVEGADOR, useValue: plataforma === 'browser' },
    ],
  });
  return { http: TestBed.inject(HttpClient), red: TestBed.inject(HttpTestingController) };
}

/** Deja correr las promesas pendientes (la petición sale después de recibir el token). */
const siguienteVuelta = (): Promise<void> => new Promise((listo) => setTimeout(listo, 0));

function ponerCookie(valor: string | null): void {
  document.cookie =
    valor === null
      ? 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
      : `XSRF-TOKEN=${valor}; path=/`;
}

describe('xsrfInterceptor', () => {
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    ponerCookie(null);
  });

  it('agrega el encabezado y las credenciales a lo que modifica datos en la API', async () => {
    ponerCookie('token%2Bficticio');
    const { http, red } = configurar();

    const respuesta = firstValueFrom(http.post('/api/v1/cuentas/login', {}));
    const peticion = red.expectOne('/api/v1/cuentas/login');
    peticion.flush({});
    await respuesta;

    expect(peticion.request.headers.get(ENCABEZADO_XSRF)).toBe('token+ficticio');
    expect(peticion.request.withCredentials).toBe(true);
  });

  it('en una lectura manda credenciales pero no el encabezado', async () => {
    ponerCookie('token');
    const { http, red } = configurar();

    const respuesta = firstValueFrom(http.get('/api/v1/cuentas/yo'));
    const peticion = red.expectOne('/api/v1/cuentas/yo');
    peticion.flush({});
    await respuesta;

    expect(peticion.request.headers.has(ENCABEZADO_XSRF)).toBe(false);
    expect(peticion.request.withCredentials).toBe(true);
  });

  it('nunca manda el token ni la cookie a otro sitio', async () => {
    ponerCookie('token');
    const { http, red } = configurar('https://api.serviya.mx/api/v1');

    const respuesta = firstValueFrom(http.post('https://www.mercadopago.com.mx/algo', {}));
    const peticion = red.expectOne('https://www.mercadopago.com.mx/algo');
    peticion.flush({});
    await respuesta;

    expect(peticion.request.headers.has(ENCABEZADO_XSRF)).toBe(false);
    expect(peticion.request.withCredentials).toBe(false);
  });

  it('con la API en otro dominio también agrega el encabezado (el de Angular lo omite)', async () => {
    ponerCookie('token');
    const { http, red } = configurar('https://api.serviya.mx/api/v1');

    const respuesta = firstValueFrom(http.delete('https://api.serviya.mx/api/v1/bloqueos/abc'));
    const peticion = red.expectOne('https://api.serviya.mx/api/v1/bloqueos/abc');
    peticion.flush(null);
    await respuesta;

    expect(peticion.request.headers.get(ENCABEZADO_XSRF)).toBe('token');
    expect(peticion.request.withCredentials).toBe(true);
  });

  it('si aún no hay cookie la pide primero y luego manda la petición con el token', async () => {
    const { http, red } = configurar();

    const respuesta = firstValueFrom(http.put('/api/v1/prestadores/yo', {}));
    const pedirToken = red.expectOne('/api/v1/cuentas/xsrf');
    expect(pedirToken.request.withCredentials).toBe(true);
    ponerCookie('recien-emitido');
    pedirToken.flush(null);
    await siguienteVuelta();
    const peticion = red.expectOne('/api/v1/prestadores/yo');
    peticion.flush({});
    await respuesta;

    expect(peticion.request.headers.get(ENCABEZADO_XSRF)).toBe('recien-emitido');
  });

  it('si no se pudo obtener el token, la petición sale igual y la API decide', async () => {
    const { http, red } = configurar();

    const respuesta = firstValueFrom(http.patch('/api/v1/prestadores/yo', {}));
    red.expectOne('/api/v1/cuentas/xsrf').error(new ProgressEvent('error'));
    await siguienteVuelta();
    const peticion = red.expectOne('/api/v1/prestadores/yo');
    peticion.flush({});
    await respuesta;

    expect(peticion.request.headers.has(ENCABEZADO_XSRF)).toBe(false);
  });

  it('en el servidor (SSR) no toca las peticiones', async () => {
    ponerCookie('token');
    const { http, red } = configurar('/api/v1', 'server');

    const respuesta = firstValueFrom(http.post('/api/v1/cuentas/login', {}));
    const peticion = red.expectOne('/api/v1/cuentas/login');
    peticion.flush({});
    await respuesta;

    expect(peticion.request.headers.has(ENCABEZADO_XSRF)).toBe(false);
  });
});

describe('esDeLaApi', () => {
  it.each([
    ['/api/v1', '/api/v1/cuentas', true],
    ['/api/v1', '/api/v1', true],
    ['/api/v1', '/api/v10/cuentas', false],
    ['/api/v1', 'https://otro.test/api/v1/cuentas', false],
    ['https://api.serviya.mx/api/v1', 'https://api.serviya.mx/api/v1/cuentas', true],
    ['https://api.serviya.mx/api/v1', 'https://api.serviya.mx.otro.test/api/v1/cuentas', false],
    ['https://api.serviya.mx/api/v1', 'https://api.serviya.mx/api/v1?x=1', true],
  ])('con apiUrl %s, %s → %s', (apiUrl, url, esperado) => {
    expect(esDeLaApi(url, apiUrl)).toBe(esperado);
  });
});

describe('leerCookie', () => {
  it('lee y decodifica la cookie pedida', () => {
    expect(leerCookie('a=1; XSRF-TOKEN=x%2By; b=2', 'XSRF-TOKEN')).toBe('x+y');
  });

  it('no confunde cookies con nombres parecidos', () => {
    expect(leerCookie('OTRO-XSRF-TOKEN=malo', 'XSRF-TOKEN')).toBeNull();
  });

  it('una cookie mal codificada se ignora', () => {
    expect(leerCookie('XSRF-TOKEN=%E0%A4%A', 'XSRF-TOKEN')).toBeNull();
  });
});
