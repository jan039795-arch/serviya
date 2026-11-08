import { AreasSimuladas } from '../../../environments/entorno';
import { ProblemDetails } from '../api/modelos';

/** Petición que recibe un manejador simulado. La ruta ya no incluye `apiUrl`. */
export interface PeticionSimulada {
  readonly metodo: string;
  readonly ruta: string;
  readonly parametros: Readonly<Record<string, string>>;
  readonly consulta: URLSearchParams;
  readonly cuerpo: unknown;
}

export interface RespuestaSimulada {
  readonly status: number;
  readonly cuerpo?: unknown;
  readonly encabezados?: Readonly<Record<string, string>>;
}

export type Manejador = (peticion: PeticionSimulada) => RespuestaSimulada;

export type MetodoHttp = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

/** Una ruta del contrato (contracts/api.md) con su área y su manejador. */
export interface RutaSimulada {
  readonly area: keyof AreasSimuladas;
  readonly metodo: MetodoHttp;
  /** Patrón como `/prestadores/:id/resenas`. */
  readonly patron: string;
  readonly manejar: Manejador;
}

export function ruta(
  area: keyof AreasSimuladas,
  metodo: MetodoHttp,
  patron: string,
  manejar: Manejador,
): RutaSimulada {
  return { area, metodo, patron, manejar };
}

export function ok(cuerpo?: unknown, status = 200): RespuestaSimulada {
  return { status, cuerpo };
}

export function creado(cuerpo?: unknown): RespuestaSimulada {
  return { status: 201, cuerpo };
}

export function sinContenido(): RespuestaSimulada {
  return { status: 204 };
}

/** Error en formato `ProblemDetails`, igual que la API real. */
export function problema(
  status: number,
  title: string,
  extra: Omit<ProblemDetails, 'status' | 'title'> = {},
): RespuestaSimulada {
  return {
    status,
    cuerpo: { type: 'about:blank', title, status, ...extra } satisfies ProblemDetails,
    encabezados: { 'Content-Type': 'application/problem+json' },
  };
}

/**
 * Compara un patrón con una ruta y devuelve sus parámetros, o `null` si no coincide.
 * `coincidir('/prestadores/:id', '/prestadores/abc')` → `{ id: 'abc' }`.
 */
export function coincidir(patron: string, rutaPedida: string): Record<string, string> | null {
  const partesPatron = patron.split('/').filter(Boolean);
  const partesRuta = rutaPedida.split('/').filter(Boolean);
  if (partesPatron.length !== partesRuta.length) {
    return null;
  }
  const parametros: Record<string, string> = {};
  for (const [indice, parte] of partesPatron.entries()) {
    const valor = partesRuta[indice];
    if (parte.startsWith(':')) {
      parametros[parte.slice(1)] = decodeURIComponent(valor);
    } else if (parte !== valor) {
      return null;
    }
  }
  return parametros;
}

/**
 * Devuelve la ruta relativa a `apiUrl` (sin cadena de consulta), o `null` si la URL no es de la API.
 */
export function rutaRelativa(url: string, apiUrl: string): string | null {
  if (!url.startsWith(apiUrl)) {
    return null;
  }
  const resto = url.slice(apiUrl.length);
  if (resto !== '' && !resto.startsWith('/') && !resto.startsWith('?')) {
    return null;
  }
  const ruta = resto.split('?')[0];
  return ruta === '' ? '/' : ruta;
}
