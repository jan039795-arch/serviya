import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Avisos } from '../../shared/ui/avisos';
import { ProblemDetails } from '../api/modelos';

export const TIPO_CUENTA_SUSPENDIDA = '/errores/cuenta-suspendida';

/** Error de la API ya traducido a mensajes para el usuario. */
export class ErrorApi extends Error {
  constructor(
    readonly status: number,
    mensaje: string,
    readonly tipo: string | null = null,
    readonly erroresPorCampo: Readonly<Record<string, readonly string[]>> = {},
    readonly faltantes: readonly string[] = [],
    readonly reintentarEnSegundos: number | null = null,
  ) {
    super(mensaje);
    this.name = 'ErrorApi';
  }
}

/** Estados que se avisan de forma global; los demás los maneja cada pantalla. */
const ESTADOS_CON_AVISO = new Set([0, 403, 429]);

/**
 * Convierte `ProblemDetails` (RFC 9457) en `ErrorApi` con textos en español y avisa los errores
 * de red, de permiso, de límite de uso y del servidor.
 */
export const erroresInterceptor: HttpInterceptorFn = (req, next) => {
  const avisos = inject(Avisos);
  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }
      const errorApi = aErrorApi(error);
      if (ESTADOS_CON_AVISO.has(errorApi.status) || errorApi.status >= 500) {
        avisos.mostrar('error', errorApi.message);
      }
      return throwError(() => errorApi);
    }),
  );
};

export function aErrorApi(error: HttpErrorResponse): ErrorApi {
  const problema = leerProblema(error.error);
  const reintentar = segundosDeEspera(error.headers?.get('Retry-After') ?? null);
  return new ErrorApi(
    error.status,
    mensajePara(error.status, problema, reintentar),
    problema?.type ?? null,
    problema?.errors ?? {},
    problema?.faltantes ?? [],
    reintentar,
  );
}

function mensajePara(
  status: number,
  problema: ProblemDetails | null,
  reintentar: number | null,
): string {
  const detalle = problema?.detail ?? problema?.title;
  switch (true) {
    case status === 0:
      return 'No hay conexión. Revisa tu internet e inténtalo de nuevo.';
    case status === 401:
      return 'Tu sesión terminó. Vuelve a entrar.';
    case status === 403 && problema?.type === TIPO_CUENTA_SUSPENDIDA:
      return detalle ? `Tu cuenta está suspendida: ${detalle}` : 'Tu cuenta está suspendida.';
    case status === 403:
      return 'No tienes permiso para hacer esto.';
    case status === 404:
      return detalle ?? 'No encontramos lo que buscas.';
    case status === 429:
      return reintentar
        ? `Hiciste demasiados intentos. Espera ${reintentar} segundos.`
        : 'Hiciste demasiados intentos. Espera un momento.';
    case status >= 500:
      return 'Algo salió mal de nuestro lado. Inténtalo más tarde.';
    default:
      return detalle ?? 'Revisa los datos marcados e inténtalo de nuevo.';
  }
}

/** Valida en el límite: el cuerpo del error llega como `unknown`. */
function leerProblema(cuerpo: unknown): ProblemDetails | null {
  if (typeof cuerpo !== 'object' || cuerpo === null) {
    return null;
  }
  const valor = cuerpo as Record<string, unknown>;
  const texto = (campo: string): string | undefined =>
    typeof valor[campo] === 'string' ? (valor[campo] as string) : undefined;
  return {
    type: texto('type'),
    title: texto('title'),
    detail: texto('detail'),
    status: typeof valor['status'] === 'number' ? valor['status'] : undefined,
    errors: leerErrores(valor['errors']),
    faltantes: Array.isArray(valor['faltantes'])
      ? valor['faltantes'].filter((f): f is string => typeof f === 'string')
      : undefined,
  };
}

function leerErrores(valor: unknown): Record<string, readonly string[]> | undefined {
  if (typeof valor !== 'object' || valor === null) {
    return undefined;
  }
  // Sin prototipo: una clave como `__proto__` que venga del servidor no altera el objeto.
  const errores: Record<string, readonly string[]> = Object.create(null);
  for (const [campo, mensajes] of Object.entries(valor)) {
    if (Array.isArray(mensajes)) {
      errores[campo] = mensajes.filter((m): m is string => typeof m === 'string');
    }
  }
  return errores;
}

function segundosDeEspera(encabezado: string | null): number | null {
  if (!encabezado) {
    return null;
  }
  const segundos = Number(encabezado);
  return Number.isFinite(segundos) && segundos > 0 ? Math.ceil(segundos) : null;
}
