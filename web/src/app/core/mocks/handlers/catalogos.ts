import municipios from '../../../../mocks/municipios.json';
import oficios from '../../../../mocks/oficios.json';
import { ok, ruta, RutaSimulada } from '../simulacion';

/** Catálogos de oficios y municipios (área `busqueda`; T029 agrega la búsqueda). */
export const rutasCatalogos: readonly RutaSimulada[] = [
  ruta('busqueda', 'GET', '/catalogos/oficios', () => ok(oficios)),
  ruta('busqueda', 'GET', '/catalogos/municipios', () => ok(municipios)),
];
