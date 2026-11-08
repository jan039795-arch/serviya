import { AccesoContacto, OpcionesAcceso, OrigenAcceso, Pago, PaseMensual } from '../../api/modelos';
import { buscarPublicado, directorio } from '../estado/directorio';
import { guardarPagos, leerPagos } from '../estado/pagos';
import {
  AccesoInterno,
  accesoVigente,
  CONTACTOS_POR_PASE,
  contactosDelDia,
  creditosDisponibles,
  DIAS_REFERENCIA_EFECTIVO,
  DIAS_VIGENCIA,
  EstadoPagos,
  LIMITE_CONTACTOS_DIARIOS,
  nuevoAcceso,
  ofertaPase,
  PagoInterno,
  PaseInterno,
  paseActivo,
  PRECIO_CONTACTO_CENTAVOS,
  PRECIO_PASE_CENTAVOS,
  sumarDias,
} from '../estado/reglas-acceso';
import { aSesion, nuevoId, usuarioActual, UsuarioSimulado } from '../estado/usuarios';
import {
  creado,
  ok,
  problema,
  RespuestaSimulada,
  ruta,
  RutaSimulada,
  sinContenido,
} from '../simulacion';

/** Aviso de no garantía que se muestra antes de cada pago (FR-022, principio II). */
export const AVISO_PAGO = {
  version: '2026-11-borrador',
  texto: 'Pagas por acceso y contacto, no por el trabajo. No garantiza la contratación.',
} as const;

/** URL del checkout simulado del prototipo (en producción es Checkout Pro de Mercado Pago). */
const urlCheckout = (pagoId: string): string => `/pago/simulado?pagoId=${pagoId}`;

// ---------- DTOs: nunca se envían los campos internos ----------

function aAcceso(a: AccesoInterno): AccesoContacto {
  const { id, prestadorId, nombrePrestador, origen, estado, activadoEn, venceEn } = a;
  return { id, prestadorId, nombrePrestador, origen, estado, activadoEn, venceEn };
}

function aPago(p: PagoInterno): Pago {
  const { id, concepto, montoCentavos, descuentoCentavos, metodo, estado, creadoEn, confirmadoEn } =
    p;
  return { id, concepto, montoCentavos, descuentoCentavos, metodo, estado, creadoEn, confirmadoEn };
}

function aPase(p: PaseInterno): PaseMensual {
  const { id, estado, inicioEn, finEn, contactosIncluidos, contactosUsados } = p;
  const { renovacionAutomatica, renovacionCanceladaEn } = p;
  return {
    id,
    estado,
    inicioEn,
    finEn,
    contactosIncluidos,
    contactosUsados,
    renovacionAutomatica,
    renovacionCanceladaEn,
  };
}

// ---------- Utilidades ----------

type ManejadorCliente = (
  cliente: UsuarioSimulado,
  estado: EstadoPagos,
  parametros: Readonly<Record<string, string>>,
  cuerpo: Record<string, unknown>,
  consulta: URLSearchParams,
) => RespuestaSimulada;

function conCliente(manejar: ManejadorCliente): RutaSimulada['manejar'] {
  return ({ parametros, cuerpo, consulta }) => {
    const usuario = usuarioActual();
    if (!usuario) {
      return problema(401, 'No has iniciado sesión.');
    }
    const cuerpoObjeto = (cuerpo ?? {}) as Record<string, unknown>;
    return manejar(usuario, leerPagos(), parametros, cuerpoObjeto, consulta);
  };
}

function soloMayores(cliente: UsuarioSimulado): RespuestaSimulada | null {
  return aSesion(cliente).esMayorDeEdad
    ? null
    : problema(403, 'Necesitas 18 años o más.', {
        detail: 'Para pagar un contacto necesitas tener 18 años o más.',
      });
}

function avisoAceptado(cuerpo: Record<string, unknown>): RespuestaSimulada | null {
  return cuerpo['avisoVersion'] === AVISO_PAGO.version
    ? null
    : problema(422, 'Revisa el aviso antes de pagar.', {
        detail: 'El aviso cambió. Vuelve a cargar la página y revísalo antes de pagar.',
      });
}

// ---------- Opciones y solicitud de acceso ----------

function opciones(cliente: UsuarioSimulado, estado: EstadoPagos, prestadorId: string) {
  const ahora = new Date();
  const vigente = accesoVigente(estado, cliente.id, prestadorId);
  const pase = paseActivo(estado, cliente.id);
  const respuesta: OpcionesAcceso = {
    accesoVigente: vigente ? aAcceso(vigente) : undefined,
    pase: pase
      ? {
          contactosDisponibles: pase.contactosIncluidos - pase.contactosUsados,
          finEn: pase.finEn ?? '',
        }
      : undefined,
    creditosDisponibles: creditosDisponibles(estado, cliente.id).length,
    ofertaPase: ofertaPase(estado, cliente.id, ahora),
    aviso: AVISO_PAGO,
    precios: { contactoCentavos: PRECIO_CONTACTO_CENTAVOS, paseCentavos: PRECIO_PASE_CENTAVOS },
  };
  return ok(respuesta);
}

function solicitarAcceso(
  cliente: UsuarioSimulado,
  estado: EstadoPagos,
  cuerpo: Record<string, unknown>,
) {
  const ahora = new Date();
  const prestadorId = String(cuerpo['prestadorId'] ?? '');
  const via = cuerpo['via'] as OrigenAcceso;
  const bloqueo = soloMayores(cliente) ?? avisoAceptado(cuerpo);
  if (bloqueo) {
    return bloqueo;
  }
  const prestador = buscarPublicado(prestadorId);
  if (!prestador) {
    return problema(422, 'Este prestador no está disponible.');
  }
  if (accesoVigente(estado, cliente.id, prestadorId)) {
    return problema(409, 'Ya tienes acceso a este prestador.', {
      detail: 'Ya tienes acceso a este prestador; no se te cobra de nuevo.',
    });
  }
  if (contactosDelDia(estado, cliente.id, ahora) >= LIMITE_CONTACTOS_DIARIOS) {
    return {
      ...problema(429, 'Llegaste al límite de contactos de hoy.', {
        detail: 'Puedes contactar hasta 10 prestadores nuevos por día. Inténtalo mañana.',
      }),
    };
  }
  const base = {
    id: nuevoId(),
    clienteId: cliente.id,
    prestadorId,
    nombrePrestador: prestador.nombrePublico,
  };

  if (via === 'Pase') {
    const pase = paseActivo(estado, cliente.id);
    if (!pase || pase.contactosUsados >= pase.contactosIncluidos) {
      return problema(409, 'Ya usaste los contactos de tu pase.', {
        detail: `Ya usaste los ${CONTACTOS_POR_PASE} contactos de tu pase. Puedes pagar un contacto individual.`,
      });
    }
    const acceso = nuevoAcceso({ ...base, origen: 'Pase' }, ahora, true);
    // Los accesos del pase vencen cuando termina el pase (US4, escenario 6).
    const conVencimiento: AccesoInterno = { ...acceso, venceEn: pase.finEn };
    guardarPagos({
      ...estado,
      accesos: [...estado.accesos, conVencimiento],
      pases: estado.pases.map((p) =>
        p.id === pase.id ? { ...p, contactosUsados: p.contactosUsados + 1 } : p,
      ),
    });
    return creado({ acceso: aAcceso(conVencimiento) });
  }

  if (via === 'Credito') {
    const [credito] = creditosDisponibles(estado, cliente.id);
    if (!credito) {
      return problema(409, 'No tienes créditos disponibles.');
    }
    const acceso = nuevoAcceso({ ...base, origen: 'Credito' }, ahora, true);
    guardarPagos({
      ...estado,
      accesos: [...estado.accesos, acceso],
      creditos: estado.creditos.map((c) => (c.id === credito.id ? { ...c, estado: 'Usado' } : c)),
    });
    return creado({ acceso: aAcceso(acceso) });
  }

  const pago: PagoInterno = {
    id: nuevoId(),
    clienteId: cliente.id,
    concepto: 'ContactoIndividual',
    montoCentavos: PRECIO_CONTACTO_CENTAVOS,
    descuentoCentavos: 0,
    estado: 'Creado',
    creadoEn: ahora.toISOString(),
    accesoId: base.id,
  };
  const acceso = nuevoAcceso({ ...base, origen: 'Individual', pagoId: pago.id }, ahora, false);
  guardarPagos({ ...estado, accesos: [...estado.accesos, acceso], pagos: [...estado.pagos, pago] });
  return creado({ acceso: aAcceso(acceso), checkoutUrl: urlCheckout(pago.id) });
}

// ---------- Pase mensual ----------

function comprarPase(
  cliente: UsuarioSimulado,
  estado: EstadoPagos,
  cuerpo: Record<string, unknown>,
) {
  const bloqueo = soloMayores(cliente) ?? avisoAceptado(cuerpo);
  if (bloqueo) {
    return bloqueo;
  }
  if (paseActivo(estado, cliente.id)) {
    return problema(409, 'Ya tienes un pase activo.');
  }
  const ahora = new Date();
  const oferta = ofertaPase(estado, cliente.id, ahora);
  const pase: PaseInterno = {
    id: nuevoId(),
    clienteId: cliente.id,
    estado: 'Pendiente',
    contactosIncluidos: CONTACTOS_POR_PASE,
    contactosUsados: 0,
    renovacionAutomatica: cuerpo['renovacionAutomatica'] === true,
  };
  const pago: PagoInterno = {
    id: nuevoId(),
    clienteId: cliente.id,
    concepto: 'PaseMensual',
    montoCentavos: oferta?.montoCentavos ?? PRECIO_PASE_CENTAVOS,
    descuentoCentavos: oferta?.descuentoCentavos ?? 0,
    estado: 'Creado',
    creadoEn: ahora.toISOString(),
    paseId: pase.id,
  };
  guardarPagos({
    ...estado,
    pases: [
      ...estado.pases.filter((p) => p.clienteId !== cliente.id || p.estado !== 'Pendiente'),
      { ...pase, pagoId: pago.id },
    ],
    pagos: [...estado.pagos, pago],
  });
  return creado({ pase: aPase(pase), checkoutUrl: urlCheckout(pago.id) });
}

function cancelarRenovacion(cliente: UsuarioSimulado, estado: EstadoPagos, paseId: string) {
  const pase = estado.pases.find((p) => p.id === paseId && p.clienteId === cliente.id);
  if (!pase) {
    return problema(404, 'No encontramos tu pase.');
  }
  guardarPagos({
    ...estado,
    pases: estado.pases.map((p) =>
      p.id === paseId
        ? { ...p, renovacionAutomatica: false, renovacionCanceladaEn: new Date().toISOString() }
        : p,
    ),
  });
  return sinContenido();
}

// ---------- Confirmación simulada del pago (en producción: webhook firmado) ----------

type ResultadoSimulado = 'aprobado' | 'pendiente' | 'rechazado' | 'acreditar';

function confirmarPago(
  estado: EstadoPagos,
  pagoId: string,
  resultado: ResultadoSimulado,
): RespuestaSimulada {
  const pago = estado.pagos.find((p) => p.id === pagoId);
  if (!pago) {
    return problema(404, 'No encontramos el pago.');
  }
  const ahora = new Date();
  const aprobado = resultado === 'aprobado' || resultado === 'acreditar';
  const pagoNuevo: PagoInterno = {
    ...pago,
    estado: aprobado ? 'Aprobado' : resultado === 'pendiente' ? 'Pendiente' : 'Rechazado',
    metodo:
      resultado === 'aprobado'
        ? 'credit_card'
        : resultado === 'rechazado'
          ? 'credit_card'
          : 'ticket',
    confirmadoEn: aprobado ? ahora.toISOString() : undefined,
    vencimientoReferencia:
      resultado === 'pendiente' ? sumarDias(ahora, DIAS_REFERENCIA_EFECTIVO) : undefined,
  };
  const activar = (a: AccesoInterno): AccesoInterno => {
    if (a.id !== pago.accesoId) {
      return a;
    }
    if (aprobado) {
      return {
        ...a,
        estado: 'Activo',
        activadoEn: ahora.toISOString(),
        venceEn: sumarDias(ahora, DIAS_VIGENCIA),
      };
    }
    return resultado === 'rechazado' ? { ...a, estado: 'Expirado' } : a;
  };
  const activarPase = (p: PaseInterno): PaseInterno => {
    if (p.id !== pago.paseId) {
      return p;
    }
    if (aprobado) {
      return {
        ...p,
        estado: 'Activo',
        inicioEn: ahora.toISOString(),
        finEn: sumarDias(ahora, DIAS_VIGENCIA),
      };
    }
    return resultado === 'rechazado' ? { ...p, estado: 'Cancelado' } : p;
  };
  guardarPagos({
    ...estado,
    pagos: estado.pagos.map((p) => (p.id === pagoId ? pagoNuevo : p)),
    accesos: estado.accesos.map(activar),
    pases: estado.pases.map(activarPase),
  });
  return sinContenido();
}

/** Acceso al contacto y pagos (contracts/api.md, sección Acceso al contacto y pagos). */
export const rutasAccesos: readonly RutaSimulada[] = [
  ruta(
    'accesos',
    'GET',
    '/accesos/opciones',
    conCliente((cliente, estado, _p, _c, consulta) =>
      opciones(cliente, estado, consulta.get('prestadorId') ?? ''),
    ),
  ),
  ruta(
    'accesos',
    'POST',
    '/accesos',
    conCliente((cliente, estado, _p, cuerpo) => solicitarAcceso(cliente, estado, cuerpo)),
  ),
  ruta(
    'accesos',
    'GET',
    '/accesos/yo',
    conCliente((cliente, estado) =>
      ok(
        estado.accesos
          .filter((a) => a.clienteId === cliente.id)
          .sort((a, b) => b.creadoEn.localeCompare(a.creadoEn))
          .map(aAcceso),
      ),
    ),
  ),
  ruta(
    'accesos',
    'GET',
    '/accesos/:id/contacto',
    conCliente((cliente, estado, parametros) => {
      const acceso = estado.accesos.find(
        (a) => a.id === parametros['id'] && a.clienteId === cliente.id,
      );
      if (!acceso || acceso.estado !== 'Activo') {
        return problema(403, 'Este acceso no está activo.', {
          detail: 'El teléfono solo se muestra mientras tu acceso está activo.',
        });
      }
      const telefono = directorio().find((p) => p.id === acceso.prestadorId)?.telefono ?? '';
      return ok({ telefono, whatsapp: telefono });
    }),
  ),
  ruta(
    'accesos',
    'GET',
    '/pagos/yo',
    conCliente((cliente, estado) =>
      ok(estado.pagos.filter((p) => p.clienteId === cliente.id).map(aPago)),
    ),
  ),
  ruta(
    'accesos',
    'GET',
    '/pagos/:id',
    conCliente((cliente, estado, parametros) => {
      const pago = estado.pagos.find(
        (p) => p.id === parametros['id'] && p.clienteId === cliente.id,
      );
      return pago ? ok(aPago(pago)) : problema(404, 'No encontramos el pago.');
    }),
  ),
  ruta(
    'accesos',
    'POST',
    '/pases',
    conCliente((cliente, estado, _p, cuerpo) => comprarPase(cliente, estado, cuerpo)),
  ),
  ruta(
    'accesos',
    'GET',
    '/pases/yo',
    conCliente((cliente, estado) => {
      const pase = estado.pases.find(
        (p) => p.clienteId === cliente.id && (p.estado === 'Activo' || p.estado === 'Pendiente'),
      );
      return pase ? ok(aPase(pase)) : problema(404, 'No tienes un pase.');
    }),
  ),
  ruta(
    'accesos',
    'POST',
    '/pases/:id/cancelar-renovacion',
    conCliente((cliente, estado, parametros) =>
      cancelarRenovacion(cliente, estado, parametros['id']),
    ),
  ),
  // Solo del prototipo: hacen lo que en producción hace el webhook de Mercado Pago.
  ruta(
    'accesos',
    'POST',
    '/dev/pagos/:id/simular',
    conCliente((_c, estado, parametros, cuerpo) =>
      confirmarPago(estado, parametros['id'], cuerpo['resultado'] as ResultadoSimulado),
    ),
  ),
  ruta(
    'accesos',
    'POST',
    '/dev/accesos/:id/acreditar',
    conCliente((cliente, estado, parametros) => {
      const pago = estado.pagos.find(
        (p) => p.accesoId === parametros['id'] && p.clienteId === cliente.id,
      );
      return pago ? confirmarPago(estado, pago.id, 'acreditar') : problema(404, 'Sin pago.');
    }),
  ),
  ruta(
    'accesos',
    'POST',
    '/dev/accesos/:id/vencer',
    conCliente((cliente, estado, parametros) => {
      guardarPagos({
        ...estado,
        accesos: estado.accesos.map((a) =>
          a.id === parametros['id'] && a.clienteId === cliente.id
            ? { ...a, venceEn: new Date(Date.now() - 1000).toISOString() }
            : a,
        ),
      });
      return sinContenido();
    }),
  ),
];
