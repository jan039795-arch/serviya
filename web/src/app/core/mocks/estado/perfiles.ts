import { PerfilPropio } from '../../api/modelos';
import { guardarLocal, leerLocal } from '../almacen-local';

/**
 * Perfiles de prestador del prototipo, por Id de usuario. Se guardan en `localStorage` para
 * comprobar que el alta se puede interrumpir y retomar (US1, escenario 2).
 */
const CLAVE_PERFILES = 'perfiles';

type MapaPerfiles = Readonly<Record<string, PerfilPropio>>;

function leerTodos(): MapaPerfiles {
  return leerLocal<MapaPerfiles>(CLAVE_PERFILES) ?? {};
}

export function perfilDe(usuarioId: string): PerfilPropio | null {
  return leerTodos()[usuarioId] ?? null;
}

export function todosLosPerfiles(): readonly (readonly [string, PerfilPropio])[] {
  return Object.entries(leerTodos());
}

export function guardarPerfil(usuarioId: string, perfil: PerfilPropio): PerfilPropio {
  guardarLocal(CLAVE_PERFILES, { ...leerTodos(), [usuarioId]: perfil });
  return perfil;
}

export function perfilNuevo(id: string): PerfilPropio {
  return {
    id,
    estado: 'Borrador',
    pasoAlta: 0,
    oficios: [],
    cobertura: [],
    disponibilidad: [],
    descripcion: '',
    documentos: [],
    portafolio: [],
    insignias: [],
  };
}

/** Crea el perfil en `Borrador` si el usuario todavía no tiene uno. */
export function asegurarPerfil(usuarioId: string, perfilId: string): PerfilPropio {
  return perfilDe(usuarioId) ?? guardarPerfil(usuarioId, perfilNuevo(perfilId));
}

// ---------- Envíos a revisión ----------

export interface EnvioRevision {
  readonly enviadoEn: string;
  readonly esReenvio: boolean;
}

const CLAVE_ENVIOS = 'envios-revision';

function leerEnvios(): Readonly<Record<string, EnvioRevision>> {
  return leerLocal<Record<string, EnvioRevision>>(CLAVE_ENVIOS) ?? {};
}

/** Registra cuándo se envió (o reenvió tras una corrección) un perfil a revisión. */
export function marcarEnviado(usuarioId: string, esReenvio: boolean): void {
  guardarEnvio(usuarioId, { enviadoEn: new Date().toISOString(), esReenvio });
}

export function guardarEnvio(usuarioId: string, envio: EnvioRevision): void {
  guardarLocal(CLAVE_ENVIOS, { ...leerEnvios(), [usuarioId]: envio });
}

export function envioDe(usuarioId: string): EnvioRevision | null {
  return leerEnvios()[usuarioId] ?? null;
}

/** Busca un perfil por su Id de prestador (no por el de usuario). */
export function buscarPerfil(
  prestadorId: string,
): { readonly usuarioId: string; readonly perfil: PerfilPropio } | null {
  const encontrado = todosLosPerfiles().find(([, perfil]) => perfil.id === prestadorId);
  return encontrado ? { usuarioId: encontrado[0], perfil: encontrado[1] } : null;
}
