import { inject } from '@angular/core';
import { CanMatchFn, Router, UrlSegment, UrlTree } from '@angular/router';
import { Rol } from '../api/modelos';
import { SesionService } from './sesion';

/**
 * Guards de la app. Son `canMatch` para que una ruta restringida ni siquiera cargue su código
 * sin permiso. La seguridad real está en la API; esto solo ordena la navegación.
 */
export const conSesion: CanMatchFn = async (_ruta, segmentos) => {
  const sesion = inject(SesionService);
  const router = inject(Router);
  const usuario = await cargarSinFallar(sesion);
  return usuario ? true : aEntrar(router, segmentos);
};

export function conRol(rol: Rol): CanMatchFn {
  return async (_ruta, segmentos) => {
    const sesion = inject(SesionService);
    const router = inject(Router);
    const usuario = await cargarSinFallar(sesion);
    if (!usuario) {
      return aEntrar(router, segmentos);
    }
    return usuario.roles.includes(rol) ? true : router.createUrlTree(['/sin-permiso']);
  };
}

async function cargarSinFallar(sesion: SesionService): ReturnType<SesionService['cargar']> {
  try {
    return await sesion.cargar();
  } catch {
    // Sin conexión o error del servidor: ya se avisó al usuario; se trata como visitante.
    return null;
  }
}

function aEntrar(router: Router, segmentos: readonly UrlSegment[]): UrlTree {
  const volver = '/' + segmentos.map((segmento) => segmento.path).join('/');
  return router.createUrlTree(['/entrar'], { queryParams: { volver } });
}
