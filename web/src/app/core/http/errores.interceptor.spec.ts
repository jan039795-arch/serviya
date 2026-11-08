import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { Avisos } from '../../shared/ui/avisos';
import {
  aErrorApi,
  ErrorApi,
  erroresInterceptor,
  TIPO_CUENTA_SUSPENDIDA,
} from './errores.interceptor';

describe('aErrorApi', () => {
  it('conserva los errores por campo y los faltantes de un 422', () => {
    const error = aErrorApi(
      new HttpErrorResponse({
        status: 422,
        error: {
          title: 'Faltan datos',
          detail: 'Completa tu perfil.',
          errors: { telefono: ['Escribe 10 dígitos.'] },
          faltantes: ['IneFrente', 'Selfie'],
        },
      }),
    );

    expect(error.message).toBe('Completa tu perfil.');
    expect(error.erroresPorCampo).toEqual({ telefono: ['Escribe 10 dígitos.'] });
    expect(error.faltantes).toEqual(['IneFrente', 'Selfie']);
  });

  it('usa un texto claro sin conexión', () => {
    const error = aErrorApi(new HttpErrorResponse({ status: 0 }));

    expect(error.message).toBe('No hay conexión. Revisa tu internet e inténtalo de nuevo.');
  });

  it('lee Retry-After en un 429', () => {
    const error = aErrorApi(
      new HttpErrorResponse({ status: 429, headers: new HttpHeaders({ 'Retry-After': '30' }) }),
    );

    expect(error.reintentarEnSegundos).toBe(30);
    expect(error.message).toBe('Hiciste demasiados intentos. Espera 30 segundos.');
  });

  it('distingue la cuenta suspendida y muestra el motivo', () => {
    const error = aErrorApi(
      new HttpErrorResponse({
        status: 403,
        error: { type: TIPO_CUENTA_SUSPENDIDA, detail: 'Reportes repetidos.' },
      }),
    );

    expect(error.tipo).toBe(TIPO_CUENTA_SUSPENDIDA);
    expect(error.message).toBe('Tu cuenta está suspendida: Reportes repetidos.');
  });

  it('no muestra detalles técnicos de un error del servidor', () => {
    const error = aErrorApi(
      new HttpErrorResponse({ status: 500, error: { detail: 'NullReferenceException' } }),
    );

    expect(error.message).toBe('Algo salió mal de nuestro lado. Inténtalo más tarde.');
  });

  it('ignora cuerpos que no son ProblemDetails', () => {
    const error = aErrorApi(new HttpErrorResponse({ status: 400, error: '<html>' }));

    expect(error.message).toBe('Revisa los datos marcados e inténtalo de nuevo.');
    expect(error.erroresPorCampo).toEqual({});
  });
});

describe('erroresInterceptor', () => {
  function configurar(): { http: HttpClient; red: HttpTestingController; avisos: Avisos } {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([erroresInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    return {
      http: TestBed.inject(HttpClient),
      red: TestBed.inject(HttpTestingController),
      avisos: TestBed.inject(Avisos),
    };
  }

  it('avisa los errores del servidor y entrega un ErrorApi', async () => {
    const { http, red, avisos } = configurar();

    const resultado = firstValueFrom(http.get('/api/v1/x')).catch((e: unknown) => e);
    red.expectOne('/api/v1/x').flush(null, { status: 503, statusText: 'No disponible' });

    expect(await resultado).toBeInstanceOf(ErrorApi);
    expect(avisos.todos().map((a) => a.tipo)).toEqual(['error']);
  });

  it('no avisa los errores de validación: los muestra la pantalla', async () => {
    const { http, red, avisos } = configurar();

    const resultado = firstValueFrom(http.post('/api/v1/x', {})).catch((e: unknown) => e);
    red
      .expectOne('/api/v1/x')
      .flush({ detail: 'Falta el nombre.' }, { status: 400, statusText: '' });

    expect((await resultado) as ErrorApi).toMatchObject({
      status: 400,
      message: 'Falta el nombre.',
    });
    expect(avisos.todos()).toEqual([]);
  });
});
