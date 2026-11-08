import { Entorno } from './entorno';

/** Producción: nunca se simulan datos. */
export const environment: Entorno = {
  produccion: true,
  apiUrl: 'https://api.serviya.mx/api/v1',
  mocks: { cuentas: false, prestadores: false, busqueda: false, accesos: false, admin: false },
};
