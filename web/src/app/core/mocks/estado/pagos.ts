import sesiones from '../../../../mocks/sesiones.json';
import { guardarLocal, leerLocal } from '../almacen-local';
import { aplicarVencimientos, EstadoPagos } from './reglas-acceso';

/**
 * Accesos, pagos, pases y créditos del prototipo en `localStorage`. El cliente de prueba empieza
 * con un crédito para que se vea esa opción al contactar.
 */
const CLAVE = 'pagos';

function estadoInicial(): EstadoPagos {
  return {
    accesos: [],
    pagos: [],
    pases: [],
    creditos: [
      {
        id: 'credito-semilla-1',
        clienteId: sesiones.cliente.id,
        motivo: 'SinRespuesta48h',
        estado: 'Disponible',
        creadoEn: '2026-11-01T12:00:00Z',
      },
    ],
  };
}

/** Lee el estado ya con los vencimientos aplicados a la hora actual. */
export function leerPagos(ahora: Date = new Date()): EstadoPagos {
  const estado = leerLocal<EstadoPagos>(CLAVE) ?? estadoInicial();
  return aplicarVencimientos(estado, ahora);
}

export function guardarPagos(estado: EstadoPagos): EstadoPagos {
  guardarLocal(CLAVE, estado);
  return estado;
}
