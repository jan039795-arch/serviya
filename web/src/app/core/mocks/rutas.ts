import { rutasAdmin } from './handlers/admin';
import { rutasCatalogos } from './handlers/catalogos';
import { rutasCuentas } from './handlers/cuentas';
import { rutasPrestadores } from './handlers/prestadores';
import { RutaSimulada } from './simulacion';

/**
 * Todas las rutas simuladas del prototipo. Este módulo se carga de forma diferida desde
 * `MockApiInterceptor`, así que los datos ficticios no entran al paquete inicial.
 * Cada pantalla agrega aquí su manejador (T029, T032 y T033).
 */
export const rutasSimuladas: readonly RutaSimulada[] = [
  ...rutasCuentas,
  ...rutasCatalogos,
  ...rutasPrestadores,
  ...rutasAdmin,
];
