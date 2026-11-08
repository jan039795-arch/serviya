import { rutasAccesos } from './handlers/accesos';
import { rutasAdmin } from './handlers/admin';
import { rutasBusqueda } from './handlers/busqueda';
import { rutasCatalogos } from './handlers/catalogos';
import { rutasCuentas } from './handlers/cuentas';
import { rutasPrestadores } from './handlers/prestadores';
import { RutaSimulada } from './simulacion';

/**
 * Todas las rutas simuladas del prototipo. Este módulo se carga de forma diferida desde
 * `MockApiInterceptor`, así que los datos ficticios no entran al paquete inicial.
 */
export const rutasSimuladas: readonly RutaSimulada[] = [
  ...rutasCuentas,
  ...rutasCatalogos,
  ...rutasPrestadores,
  ...rutasBusqueda,
  ...rutasAccesos,
  ...rutasAdmin,
];
