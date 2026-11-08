import municipios from '../../../../mocks/municipios.json';
import oficios from '../../../../mocks/oficios.json';
import { ok, problema, ruta, RutaSimulada } from '../simulacion';

/**
 * Catálogos de oficios y municipios (área `busqueda`). `ubicar` recibe la ubicación en el cuerpo y
 * no en la URL, porque es un dato personal (principio I). El prototipo no tiene polígonos: devuelve
 * siempre el primer municipio de la zona de prueba.
 */
export const rutasCatalogos: readonly RutaSimulada[] = [
  ruta('busqueda', 'GET', '/catalogos/oficios', () => ok(oficios)),
  ruta('busqueda', 'GET', '/catalogos/municipios', () => ok(municipios)),
  ruta('busqueda', 'POST', '/catalogos/municipios/ubicar', ({ cuerpo }) => {
    const { lat, lng } = (cuerpo ?? {}) as { lat?: unknown; lng?: unknown };
    if (typeof lat !== 'number' || typeof lng !== 'number') {
      return problema(400, 'Ubicación no válida.');
    }
    return ok(municipios[0]);
  }),
];
