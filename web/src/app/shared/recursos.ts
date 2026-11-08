import { Resource } from '@angular/core';

/**
 * Valor de un `resource` o `undefined`. Leer `value()` cuando el recurso falló lanza un error y
 * puede congelar la pantalla, así que en código y plantillas se usa esta función.
 */
export function valorDe<T>(recurso: Resource<T>): T | undefined {
  return recurso.hasValue() ? recurso.value() : undefined;
}
