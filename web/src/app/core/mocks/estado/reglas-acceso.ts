import {
  AccesoContacto,
  ConceptoPago,
  EstadoAcceso,
  EstadoPago,
  OrigenAcceso,
  PaseMensual,
} from '../../api/modelos';

/**
 * Reglas de acceso y pago del prototipo (FR-020 a FR-029). Son funciones puras sobre el estado para
 * poder probarlas; la API real las implementa en el módulo Pagos (T097–T117).
 */
export const PRECIO_CONTACTO_CENTAVOS = 3900;
export const PRECIO_PASE_CENTAVOS = 12900;
/**
 * El descuento al pasar al pase nunca lo deja por debajo del precio de un contacto: con 4 o más
 * contactos pagados en 30 días el pase sale gratis si no (decisión del 2026-11-08).
 */
export const PRECIO_MINIMO_PASE_CENTAVOS = PRECIO_CONTACTO_CENTAVOS;
export const DIAS_VIGENCIA = 30;
export const CONTACTOS_POR_PASE = 30;
export const LIMITE_CONTACTOS_DIARIOS = 10;
export const DIAS_REFERENCIA_EFECTIVO = 3;

const MS_POR_DIA = 86_400_000;

export interface AccesoInterno extends AccesoContacto {
  readonly clienteId: string;
  readonly pagoId?: string;
  readonly creadoEn: string;
}

export interface PagoInterno {
  readonly id: string;
  readonly clienteId: string;
  readonly concepto: ConceptoPago;
  readonly montoCentavos: number;
  readonly descuentoCentavos: number;
  readonly metodo?: string;
  readonly estado: EstadoPago;
  readonly creadoEn: string;
  readonly confirmadoEn?: string;
  readonly accesoId?: string;
  readonly paseId?: string;
  /** Fecha límite para pagar una referencia de OXXO o SPEI. */
  readonly vencimientoReferencia?: string;
}

export interface PaseInterno extends PaseMensual {
  readonly clienteId: string;
  readonly pagoId?: string;
}

export interface CreditoInterno {
  readonly id: string;
  readonly clienteId: string;
  readonly motivo:
    'SinRespuesta48h' | 'PrestadorSuspendido' | 'PrestadorEliminado' | 'PagoDuplicado';
  readonly estado: 'Disponible' | 'Usado' | 'CambiadoPorReembolso';
  readonly creadoEn: string;
}

export interface EstadoPagos {
  readonly accesos: readonly AccesoInterno[];
  readonly pagos: readonly PagoInterno[];
  readonly pases: readonly PaseInterno[];
  readonly creditos: readonly CreditoInterno[];
}

export function sumarDias(fecha: Date, dias: number): string {
  return new Date(fecha.getTime() + dias * MS_POR_DIA).toISOString();
}

/** Vence accesos y pases cumplidos y expira referencias no pagadas (FR-024; US3, escenarios 5 y 6). */
export function aplicarVencimientos(estado: EstadoPagos, ahora: Date): EstadoPagos {
  const momento = ahora.toISOString();
  const pagosVencidos = new Set(
    estado.pagos
      .filter(
        (p) =>
          p.estado === 'Pendiente' && p.vencimientoReferencia && p.vencimientoReferencia < momento,
      )
      .map((p) => p.id),
  );
  const vencerAcceso = (a: AccesoInterno): EstadoAcceso => {
    if (a.estado === 'Activo' && a.venceEn && a.venceEn < momento) {
      return 'Vencido';
    }
    if (a.estado === 'Pendiente' && a.pagoId && pagosVencidos.has(a.pagoId)) {
      return 'Expirado';
    }
    return a.estado;
  };
  return {
    ...estado,
    pagos: estado.pagos.map((p) => (pagosVencidos.has(p.id) ? { ...p, estado: 'Expirado' } : p)),
    accesos: estado.accesos.map((a) => ({ ...a, estado: vencerAcceso(a) })),
    pases: estado.pases.map((p) =>
      p.estado === 'Activo' && p.finEn && p.finEn < momento ? { ...p, estado: 'Vencido' } : p,
    ),
  };
}

/** Acceso `Pendiente` o `Activo` al prestador: no se cobra dos veces (FR-025). */
export function accesoVigente(
  estado: EstadoPagos,
  clienteId: string,
  prestadorId: string,
): AccesoInterno | undefined {
  return estado.accesos.find(
    (a) =>
      a.clienteId === clienteId &&
      a.prestadorId === prestadorId &&
      (a.estado === 'Pendiente' || a.estado === 'Activo'),
  );
}

export function paseActivo(estado: EstadoPagos, clienteId: string): PaseInterno | undefined {
  return estado.pases.find((p) => p.clienteId === clienteId && p.estado === 'Activo');
}

export function creditosDisponibles(estado: EstadoPagos, clienteId: string): CreditoInterno[] {
  return estado.creditos.filter((c) => c.clienteId === clienteId && c.estado === 'Disponible');
}

/**
 * Oferta del pase al ir por el segundo contacto individual en 30 días: 129 menos lo ya pagado en
 * contactos individuales de ese periodo (FR-027), sin bajar de $39 (`PRECIO_MINIMO_PASE_CENTAVOS`).
 */
export function ofertaPase(
  estado: EstadoPagos,
  clienteId: string,
  ahora: Date,
): { readonly montoCentavos: number; readonly descuentoCentavos: number } | undefined {
  if (paseActivo(estado, clienteId)) {
    return undefined;
  }
  const desde = new Date(ahora.getTime() - DIAS_VIGENCIA * MS_POR_DIA).toISOString();
  const pagado = estado.pagos
    .filter(
      (p) =>
        p.clienteId === clienteId &&
        p.concepto === 'ContactoIndividual' &&
        p.estado === 'Aprobado' &&
        (p.confirmadoEn ?? p.creadoEn) >= desde,
    )
    .reduce((suma, p) => suma + p.montoCentavos, 0);
  if (pagado === 0) {
    return undefined;
  }
  const descuento = Math.min(pagado, PRECIO_PASE_CENTAVOS - PRECIO_MINIMO_PASE_CENTAVOS);
  return { montoCentavos: PRECIO_PASE_CENTAVOS - descuento, descuentoCentavos: descuento };
}

/** Contactos nuevos del cliente en el día calendario de `ahora` (FR-029). */
export function contactosDelDia(estado: EstadoPagos, clienteId: string, ahora: Date): number {
  const dia = ahora.toDateString();
  return estado.accesos.filter(
    (a) => a.clienteId === clienteId && new Date(a.creadoEn).toDateString() === dia,
  ).length;
}

export function nuevoAcceso(
  datos: Pick<AccesoInterno, 'id' | 'clienteId' | 'prestadorId' | 'nombrePrestador' | 'pagoId'> & {
    readonly origen: OrigenAcceso;
  },
  ahora: Date,
  activo: boolean,
): AccesoInterno {
  return {
    ...datos,
    estado: activo ? 'Activo' : 'Pendiente',
    creadoEn: ahora.toISOString(),
    ...(activo
      ? { activadoEn: ahora.toISOString(), venceEn: sumarDias(ahora, DIAS_VIGENCIA) }
      : {}),
  };
}
