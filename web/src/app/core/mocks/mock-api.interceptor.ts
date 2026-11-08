import {
  HttpErrorResponse,
  HttpEvent,
  HttpHeaders,
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { inject, InjectionToken } from '@angular/core';
import { from, Observable, switchMap, throwError, timer } from 'rxjs';
import { ENTORNO, hayAreasSimuladas } from '../entorno';
import { coincidir, RespuestaSimulada, RutaSimulada, rutaRelativa } from './simulacion';

/** Carga las rutas simuladas. Se importan de forma diferida para no inflar el paquete inicial. */
export const RUTAS_SIMULADAS = new InjectionToken<() => Promise<readonly RutaSimulada[]>>(
  'RUTAS_SIMULADAS',
  {
    providedIn: 'root',
    factory: () => () => import('./rutas').then((m) => m.rutasSimuladas),
  },
);

/** Milisegundos de espera de cada respuesta simulada, para que se vean los estados de carga. */
export const LATENCIA_SIMULADA = new InjectionToken<number>('LATENCIA_SIMULADA', {
  providedIn: 'root',
  factory: () => 300,
});

/**
 * Responde con datos ficticios las peticiones de las áreas activas en `environment.mocks`
 * (research R10). Las demás pasan a la API real sin cambios.
 */
export const mockApiInterceptor: HttpInterceptorFn = (req, next) => {
  const entorno = inject(ENTORNO);
  if (!hayAreasSimuladas(entorno)) {
    return next(req);
  }
  const ruta = rutaRelativa(req.url, entorno.apiUrl);
  if (ruta === null) {
    return next(req);
  }
  const cargarRutas = inject(RUTAS_SIMULADAS);
  const latencia = inject(LATENCIA_SIMULADA);

  return from(cargarRutas()).pipe(
    switchMap((rutas) => {
      const hallazgo = buscarRuta(rutas, req.method, ruta);
      if (!hallazgo || !entorno.mocks[hallazgo.definicion.area]) {
        return next(req);
      }
      const respuesta = hallazgo.definicion.manejar({
        metodo: req.method,
        ruta,
        parametros: hallazgo.parametros,
        consulta: new URLSearchParams(req.urlWithParams.split('?')[1] ?? ''),
        cuerpo: req.body,
      });
      return timer(latencia).pipe(switchMap(() => responder(req, respuesta)));
    }),
  );
};

interface Hallazgo {
  readonly definicion: RutaSimulada;
  readonly parametros: Record<string, string>;
}

function buscarRuta(rutas: readonly RutaSimulada[], metodo: string, ruta: string): Hallazgo | null {
  for (const definicion of rutas) {
    if (definicion.metodo !== metodo) {
      continue;
    }
    const parametros = coincidir(definicion.patron, ruta);
    if (parametros) {
      return { definicion, parametros };
    }
  }
  return null;
}

function responder(
  req: HttpRequest<unknown>,
  respuesta: RespuestaSimulada,
): Observable<HttpEvent<unknown>> {
  const headers = new HttpHeaders({ ...respuesta.encabezados });
  if (respuesta.status >= 400) {
    return throwError(
      () =>
        new HttpErrorResponse({
          status: respuesta.status,
          error: respuesta.cuerpo ?? null,
          headers,
          url: req.urlWithParams,
        }),
    );
  }
  return from([
    new HttpResponse({
      status: respuesta.status,
      body: respuesta.cuerpo ?? null,
      headers,
      url: req.urlWithParams,
    }),
  ]);
}
