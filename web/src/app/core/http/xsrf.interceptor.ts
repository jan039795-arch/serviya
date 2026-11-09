import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { DOCUMENT, inject, Injectable, InjectionToken, PLATFORM_ID } from '@angular/core';
import { catchError, firstValueFrom, from, of, switchMap } from 'rxjs';
import { ENTORNO } from '../entorno';

export const COOKIE_XSRF = 'XSRF-TOKEN';
export const ENCABEZADO_XSRF = 'X-XSRF-TOKEN';

const METODOS_QUE_MODIFICAN = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

/** `false` al renderizar en el servidor (SSR): ahí no hay cookies del usuario ni se modifican datos. */
export const ES_NAVEGADOR = new InjectionToken<boolean>('ES_NAVEGADOR', {
  providedIn: 'root',
  factory: () => isPlatformBrowser(inject(PLATFORM_ID)),
});

/**
 * Protección CSRF con el par cookie `XSRF-TOKEN` / encabezado `X-XSRF-TOKEN` (research R2, T069).
 * El soporte de `HttpClient` omite las URL absolutas, y en producción la API está en
 * `api.serviya.mx`; por eso este interceptor propio:
 *
 * - solo toca peticiones a la API (`environment.apiUrl`); nunca manda el token ni la cookie a otro sitio;
 * - agrega `withCredentials` para que viaje la cookie de sesión;
 * - en `POST`, `PUT`, `PATCH` y `DELETE` agrega el encabezado; si la cookie aún no existe, la pide
 *   una vez a `GET /cuentas/xsrf`.
 *
 * Para que la app en `serviya.mx` pueda leer la cookie, la API la emite con `Domain=serviya.mx` (T059).
 */
export const xsrfInterceptor: HttpInterceptorFn = (req, next) => {
  const entorno = inject(ENTORNO);
  if (!inject(ES_NAVEGADOR) || !esDeLaApi(req.url, entorno.apiUrl)) {
    return next(req);
  }
  const conCredenciales = req.clone({ withCredentials: true });
  if (!METODOS_QUE_MODIFICAN.has(req.method)) {
    return next(conCredenciales);
  }
  const xsrf = inject(TokenXsrf);
  const token = xsrf.leer();
  if (token) {
    return next(conToken(conCredenciales, token));
  }
  return from(xsrf.asegurar()).pipe(
    switchMap((nuevo) => next(nuevo ? conToken(conCredenciales, nuevo) : conCredenciales)),
  );
};

function conToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { [ENCABEZADO_XSRF]: token } });
}

/** Lee la cookie y la pide a la API cuando hace falta. Una sola petición a la vez. */
@Injectable({ providedIn: 'root' })
export class TokenXsrf {
  private readonly http = inject(HttpClient);
  private readonly documento = inject(DOCUMENT);
  private readonly entorno = inject(ENTORNO);
  private pendiente: Promise<string | null> | null = null;

  leer(): string | null {
    return leerCookie(this.documento.cookie, COOKIE_XSRF);
  }

  /** Pide la cookie a `GET /cuentas/xsrf` si no existe. Nunca falla: si no se pudo, devuelve `null`. */
  asegurar(): Promise<string | null> {
    const actual = this.leer();
    if (actual) {
      return Promise.resolve(actual);
    }
    this.pendiente ??= firstValueFrom(
      this.http.get(`${this.entorno.apiUrl}/cuentas/xsrf`).pipe(catchError(() => of(null))),
    ).then(() => {
      this.pendiente = null;
      return this.leer();
    });
    return this.pendiente;
  }
}

/**
 * Al arrancar en el navegador se pide la cookie sin esperar la respuesta: la app se muestra de
 * inmediato y, si el token aún no llega, la primera petición que modifica datos lo espera.
 */
export function pedirTokenXsrfAlArrancar(): void {
  if (inject(ES_NAVEGADOR)) {
    void inject(TokenXsrf).asegurar();
  }
}

/** `true` si la URL es de la API: misma base y, después, fin, `/` o `?` (no `/api/v10`, no otro host). */
export function esDeLaApi(url: string, apiUrl: string): boolean {
  if (!url.startsWith(apiUrl)) {
    return false;
  }
  const siguiente = url.charAt(apiUrl.length);
  return siguiente === '' || siguiente === '/' || siguiente === '?';
}

export function leerCookie(cookies: string, nombre: string): string | null {
  for (const parte of cookies.split(';')) {
    const separador = parte.indexOf('=');
    if (separador < 0 || parte.slice(0, separador).trim() !== nombre) {
      continue;
    }
    try {
      return decodeURIComponent(parte.slice(separador + 1).trim()) || null;
    } catch {
      return null;
    }
  }
  return null;
}
