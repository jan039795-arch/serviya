/**
 * Lectura y escritura seguras en `localStorage` para el prototipo.
 * Si no hay almacenamiento (SSR, modo privado o bloqueado), se trabaja solo en memoria.
 */
const PREFIJO = 'serviya.prototipo.';

export function leerLocal<T>(clave: string): T | null {
  try {
    const valor = globalThis.localStorage?.getItem(PREFIJO + clave);
    return valor === null || valor === undefined ? null : (JSON.parse(valor) as T);
  } catch {
    return null;
  }
}

export function guardarLocal(clave: string, valor: unknown): void {
  try {
    globalThis.localStorage?.setItem(PREFIJO + clave, JSON.stringify(valor));
  } catch {
    // Sin almacenamiento disponible: el estado vive solo mientras la pestaña esté abierta.
  }
}

export function borrarLocal(clave: string): void {
  try {
    globalThis.localStorage?.removeItem(PREFIJO + clave);
  } catch {
    // Igual que arriba: no hay nada que borrar.
  }
}

/** Borra todos los datos del prototipo (sesión, perfiles, pagos…) de este navegador. */
export function borrarTodoLocal(): void {
  try {
    const almacen = globalThis.localStorage;
    if (!almacen) {
      return;
    }
    const claves = Array.from({ length: almacen.length }, (_, i) => almacen.key(i)).filter(
      (clave): clave is string => !!clave && clave.startsWith(PREFIJO),
    );
    for (const clave of claves) {
      almacen.removeItem(clave);
    }
  } catch {
    // Sin almacenamiento disponible: no hay nada que borrar.
  }
}
