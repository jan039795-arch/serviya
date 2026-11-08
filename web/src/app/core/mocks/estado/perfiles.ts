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
