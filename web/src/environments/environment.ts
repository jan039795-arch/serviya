import { Entorno } from './entorno';

/** Desarrollo: todo simulado hasta que cada área se conecte a la API real. */
export const environment: Entorno = {
  produccion: false,
  apiUrl: '/api/v1',
  mocks: { cuentas: true, prestadores: true, busqueda: true, accesos: true, admin: true },
};
