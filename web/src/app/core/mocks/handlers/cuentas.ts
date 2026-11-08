import { Sesion } from '../../api/modelos';
import sesiones from '../../../../mocks/sesiones.json';
import { borrarLocal, guardarLocal, leerLocal } from '../almacen-local';
import { ok, problema, ruta, RutaSimulada, sinContenido } from '../simulacion';

/** Perfiles de prueba que se pueden elegir con el selector de rol del prototipo. */
export type PerfilSimulado = keyof typeof sesiones;

const CLAVE_SESION = 'sesion';

function esPerfil(valor: unknown): valor is PerfilSimulado {
  return typeof valor === 'string' && Object.hasOwn(sesiones, valor);
}

/** Sesión simulada actual, o `null` si se navega como visitante. */
export function sesionActual(): Sesion | null {
  const perfil = leerLocal<string>(CLAVE_SESION);
  return esPerfil(perfil) ? (sesiones[perfil] as Sesion) : null;
}

export function iniciarSesionSimulada(perfil: PerfilSimulado): void {
  guardarLocal(CLAVE_SESION, perfil);
}

export function cerrarSesionSimulada(): void {
  borrarLocal(CLAVE_SESION);
}

/**
 * Rutas de Cuentas del prototipo. T019 agrega registro, verificación, login y recuperación.
 */
export const rutasCuentas: readonly RutaSimulada[] = [
  ruta('cuentas', 'GET', '/cuentas/yo', () => {
    const sesion = sesionActual();
    return sesion ? ok(sesion) : problema(401, 'No has iniciado sesión.');
  }),
  ruta('cuentas', 'POST', '/cuentas/logout', () => {
    cerrarSesionSimulada();
    return sinContenido();
  }),
  // Solo del prototipo: cambia el perfil desde el selector de rol.
  ruta('cuentas', 'POST', '/dev/simular-sesion', ({ cuerpo }) => {
    const perfil = (cuerpo as { perfil?: unknown } | null)?.perfil;
    if (perfil === null || perfil === 'visitante') {
      cerrarSesionSimulada();
      return sinContenido();
    }
    if (!esPerfil(perfil)) {
      return problema(400, 'Perfil de prueba desconocido.');
    }
    iniciarSesionSimulada(perfil);
    return sinContenido();
  }),
];
