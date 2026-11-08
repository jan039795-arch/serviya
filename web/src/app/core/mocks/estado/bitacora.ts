import { guardarLocal, leerLocal } from '../almacen-local';

/**
 * Bitácora simulada de acciones administrativas (FR-016, FR-042). Como en la API real, solo se
 * agregan registros y no llevan datos personales.
 */
export interface RegistroBitacora {
  readonly actorId: string;
  readonly accion: string;
  readonly entidadTipo: string;
  readonly entidadId: string;
  readonly creadoEn: string;
}

const CLAVE = 'bitacora';

export function registrarEnBitacora(
  actorId: string,
  accion: string,
  entidadTipo: string,
  entidadId: string,
): void {
  const registro: RegistroBitacora = {
    actorId,
    accion,
    entidadTipo,
    entidadId,
    creadoEn: new Date().toISOString(),
  };
  guardarLocal(CLAVE, [...leerBitacora(), registro]);
}

export function leerBitacora(): readonly RegistroBitacora[] {
  return leerLocal<RegistroBitacora[]>(CLAVE) ?? [];
}
