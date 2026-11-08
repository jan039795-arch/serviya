import prestadores from '../../../mocks/prestadores.json';
import sesiones from '../../../mocks/sesiones.json';
import { esUrlDePago } from '../../features/contacto/checkout';
import { guardarPagos, leerPagos } from './estado/pagos';
import {
  AccesoInterno,
  aplicarVencimientos,
  EstadoPagos,
  ofertaPase,
  PagoInterno,
} from './estado/reglas-acceso';
import { iniciarSesion } from './estado/usuarios';
import { AVISO_PAGO, rutasAccesos } from './handlers/accesos';
import { coincidir, MetodoHttp, RespuestaSimulada } from './simulacion';

const CLIENTE = sesiones.cliente.id;
const [juan, maria] = prestadores;
const ahora = new Date('2026-11-08T12:00:00Z');
const vacio: EstadoPagos = { accesos: [], pagos: [], pases: [], creditos: [] };

function pago(parcial: Partial<PagoInterno>): PagoInterno {
  return {
    id: 'p1',
    clienteId: CLIENTE,
    concepto: 'ContactoIndividual',
    montoCentavos: 3900,
    descuentoCentavos: 0,
    estado: 'Aprobado',
    creadoEn: '2026-11-01T12:00:00Z',
    confirmadoEn: '2026-11-01T12:00:00Z',
    ...parcial,
  };
}

function acceso(parcial: Partial<AccesoInterno>): AccesoInterno {
  return {
    id: 'a1',
    clienteId: CLIENTE,
    prestadorId: juan.id,
    nombrePrestador: juan.nombrePublico,
    origen: 'Individual',
    estado: 'Activo',
    creadoEn: '2026-11-01T12:00:00Z',
    ...parcial,
  };
}

function pedir(
  metodo: MetodoHttp,
  ruta: string,
  cuerpo: unknown = null,
  consulta = '',
): RespuestaSimulada {
  for (const definicion of rutasAccesos) {
    const parametros = coincidir(definicion.patron, ruta);
    if (parametros && definicion.metodo === metodo) {
      return definicion.manejar({
        metodo,
        ruta,
        parametros,
        consulta: new URLSearchParams(consulta),
        cuerpo,
      });
    }
  }
  throw new Error(`Sin ruta para ${metodo} ${ruta}`);
}

const solicitud = (prestadorId: string, via: string) => ({
  prestadorId,
  via,
  avisoVersion: AVISO_PAGO.version,
});

describe('reglas de acceso', () => {
  it('vence accesos cumplidos y expira referencias no pagadas (FR-024)', () => {
    const estado: EstadoPagos = {
      ...vacio,
      pagos: [
        pago({ id: 'oxxo', estado: 'Pendiente', vencimientoReferencia: '2026-11-07T00:00:00Z' }),
      ],
      accesos: [
        acceso({ id: 'vencido', venceEn: '2026-11-08T11:59:00Z' }),
        acceso({ id: 'vigente', venceEn: '2026-12-01T00:00:00Z' }),
        acceso({ id: 'pendiente', estado: 'Pendiente', pagoId: 'oxxo' }),
      ],
    };

    const resultado = aplicarVencimientos(estado, ahora);

    expect(resultado.accesos.map((a) => a.estado)).toEqual(['Vencido', 'Activo', 'Expirado']);
    expect(resultado.pagos[0].estado).toBe('Expirado');
  });

  it('ofrece el pase descontando los contactos pagados en 30 días (FR-027)', () => {
    expect(ofertaPase({ ...vacio, pagos: [pago({})] }, CLIENTE, ahora)).toEqual({
      montoCentavos: 9000,
      descuentoCentavos: 3900,
    });
  });

  it('no ofrece descuento sin pagos recientes ni con un pase activo', () => {
    const viejo = pago({ confirmadoEn: '2026-09-01T00:00:00Z', creadoEn: '2026-09-01T00:00:00Z' });

    expect(ofertaPase(vacio, CLIENTE, ahora)).toBeUndefined();
    expect(ofertaPase({ ...vacio, pagos: [viejo] }, CLIENTE, ahora)).toBeUndefined();
  });
});

describe('accesos simulados', () => {
  beforeEach(() => {
    localStorage.clear();
    iniciarSesion(sesiones.cliente.email);
  });

  it('un pago individual deja el acceso pendiente con URL de pago', () => {
    const respuesta = pedir('POST', '/accesos', solicitud(juan.id, 'Individual'));

    expect(respuesta.status).toBe(201);
    expect(respuesta.cuerpo).toMatchObject({
      acceso: { estado: 'Pendiente' },
      checkoutUrl: expect.stringContaining('/pago/simulado'),
    });
  });

  it('no cobra dos veces un acceso vigente (FR-025)', () => {
    pedir('POST', '/accesos', solicitud(juan.id, 'Individual'));

    expect(pedir('POST', '/accesos', solicitud(juan.id, 'Individual')).status).toBe(409);
  });

  it('exige el aviso vigente antes de pagar (FR-022)', () => {
    const respuesta = pedir('POST', '/accesos', {
      ...solicitud(juan.id, 'Individual'),
      avisoVersion: 'vieja',
    });

    expect(respuesta.status).toBe(422);
  });

  it('el teléfono solo sale con el acceso activo', () => {
    const { cuerpo } = pedir('POST', '/accesos', solicitud(juan.id, 'Individual'));
    const accesoId = (cuerpo as { acceso: { id: string } }).acceso.id;

    expect(pedir('GET', `/accesos/${accesoId}/contacto`).status).toBe(403);

    const pagoId = leerPagos().pagos[0].id;
    pedir('POST', `/dev/pagos/${pagoId}/simular`, { resultado: 'aprobado' });

    expect(pedir('GET', `/accesos/${accesoId}/contacto`).cuerpo).toMatchObject({
      telefono: juan.telefonoFicticio,
    });
  });

  it('el crédito desbloquea sin pago y se consume', () => {
    expect(pedir('POST', '/accesos', solicitud(maria.id, 'Credito')).status).toBe(201);
    expect(pedir('POST', '/accesos', solicitud(juan.id, 'Credito')).status).toBe(409);
  });

  it('con el pase agotado pide pagar un contacto individual (US4, escenario 2)', () => {
    const estado = leerPagos();
    guardarPagos({
      ...estado,
      pases: [
        {
          id: 'pase1',
          clienteId: CLIENTE,
          estado: 'Activo',
          inicioEn: '2026-11-01T00:00:00Z',
          finEn: '2099-01-01T00:00:00Z',
          contactosIncluidos: 30,
          contactosUsados: 30,
          renovacionAutomatica: false,
        },
      ],
    });

    const respuesta = pedir('POST', '/accesos', solicitud(juan.id, 'Pase'));

    expect(respuesta.status).toBe(409);
    expect(JSON.stringify(respuesta.cuerpo)).toContain('contacto individual');
  });

  it('limita a 10 contactos nuevos por día (FR-029)', () => {
    const hoy = new Date().toISOString();
    guardarPagos({
      ...leerPagos(),
      accesos: Array.from({ length: 10 }, (_, i) =>
        acceso({ id: `a${i}`, prestadorId: `otro-${i}`, creadoEn: hoy }),
      ),
    });

    expect(pedir('POST', '/accesos', solicitud(juan.id, 'Individual')).status).toBe(429);
  });

  it('cancelar la renovación es una sola acción y conserva el pase', () => {
    const { cuerpo } = pedir('POST', '/pases', {
      renovacionAutomatica: true,
      avisoVersion: AVISO_PAGO.version,
    });
    const paseId = (cuerpo as { pase: { id: string } }).pase.id;
    pedir('POST', `/dev/pagos/${leerPagos().pagos[0].id}/simular`, { resultado: 'aprobado' });

    expect(pedir('POST', `/pases/${paseId}/cancelar-renovacion`).status).toBe(204);
    expect(pedir('GET', '/pases/yo').cuerpo).toMatchObject({
      estado: 'Activo',
      renovacionAutomatica: false,
    });
  });
});

describe('esUrlDePago', () => {
  it('solo acepta https de Mercado Pago', () => {
    expect(esUrlDePago('https://www.mercadopago.com.mx/checkout/v1/redirect?pref_id=1')).toBe(true);
    expect(esUrlDePago('http://www.mercadopago.com.mx/checkout')).toBe(false);
    expect(esUrlDePago('https://mercadopago.com.mx.sitio-falso.com/')).toBe(false);
    expect(esUrlDePago('javascript:alert(1)')).toBe(false);
  });
});
