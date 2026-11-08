import { Entorno } from './entorno';

/** Prototipo optimizado para probar en el celular: todo con datos ficticios. */
export const environment: Entorno = {
  produccion: false,
  apiUrl: '/api/v1',
  mocks: { cuentas: true, prestadores: true, busqueda: true, accesos: true, admin: true },
};
