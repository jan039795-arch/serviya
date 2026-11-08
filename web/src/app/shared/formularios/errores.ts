import { FieldTree } from '@angular/forms/signals';
import { ErrorApi } from '../../core/http/errores.interceptor';

/** Primer mensaje de error de un campo, solo después de que el usuario lo tocó o envió. */
export function mensajeDeError<T>(campo: FieldTree<T>): string | null {
  const estado = campo();
  if (!estado.touched() || estado.valid()) {
    return null;
  }
  return estado.errors()[0]?.message ?? 'Revisa este dato.';
}

/** Primer error que la API devolvió para un campo (`ProblemDetails.errors`). */
export function errorDelServidor(error: unknown, campo: string): string | null {
  return error instanceof ErrorApi ? (error.erroresPorCampo[campo]?.[0] ?? null) : null;
}

/** Mensaje general para mostrar arriba del formulario. */
export function mensajeGeneral(error: unknown): string {
  return error instanceof Error && error.message
    ? error.message
    : 'Algo salió mal. Inténtalo de nuevo.';
}
