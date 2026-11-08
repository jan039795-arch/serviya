import {
  DocumentoPropio,
  ElementoPortafolioPropio,
  GuardadoPaso,
  PerfilPropio,
  SolicitudUrlSubida,
  TipoDocumento,
} from '../../api/modelos';
import { diasDesde, esMayorDeEdad } from '../../../shared/fechas/edad';
import { PREFIJO_SUBIDA_SIMULADA } from '../../../shared/subida/subidor';
import { asegurarPerfil, guardarPerfil } from '../estado/perfiles';
import { nuevoId, usuarioActual, UsuarioSimulado } from '../estado/usuarios';
import {
  creado,
  ok,
  problema,
  RespuestaSimulada,
  ruta,
  RutaSimulada,
  sinContenido,
} from '../simulacion';

/** Documentos obligatorios para enviar a revisión (FR-006). */
export const DOCUMENTOS_OBLIGATORIOS: readonly TipoDocumento[] = [
  'IneFrente',
  'IneReverso',
  'Selfie',
  'ComprobanteDomicilio',
];

const NOMBRE_DOCUMENTO: Readonly<Record<TipoDocumento, string>> = {
  IneFrente: 'INE (frente)',
  IneReverso: 'INE (reverso)',
  Selfie: 'Selfie',
  ComprobanteDomicilio: 'Comprobante de domicilio',
  CartaAntecedentes: 'Carta de no antecedentes',
};

const MAXIMO_FOTOS = 20;
const MAXIMO_DIAS_COMPROBANTE = 90;
const ESTADOS_EDITABLES = new Set(['Borrador', 'RequiereCorreccion']);

interface ArchivoPendiente {
  readonly destino: SolicitudUrlSubida['destino'];
  readonly tipoContenido: string;
}

/** Archivos con URL de subida emitida y todavía sin registrar (solo en memoria). */
const archivosPendientes = new Map<string, ArchivoPendiente>();

type ManejadorConPerfil = (
  usuario: UsuarioSimulado,
  perfil: PerfilPropio,
  cuerpo: unknown,
  parametros: Readonly<Record<string, string>>,
) => RespuestaSimulada;

/** Exige sesión con rol de prestador y entrega su perfil (lo crea en Borrador si falta). */
function conPerfil(manejar: ManejadorConPerfil): RutaSimulada['manejar'] {
  return ({ cuerpo, parametros }) => {
    const usuario = usuarioActual();
    if (!usuario) {
      return problema(401, 'No has iniciado sesión.');
    }
    if (!usuario.roles.includes('Prestador')) {
      return problema(403, 'Tu cuenta no es de prestador.');
    }
    return manejar(usuario, asegurarPerfil(usuario.id, nuevoId()), cuerpo, parametros);
  };
}

function soloEditable(perfil: PerfilPropio): RespuestaSimulada | null {
  return ESTADOS_EDITABLES.has(perfil.estado)
    ? null
    : problema(409, 'Tu perfil no se puede modificar ahora.', {
        detail: 'Tu perfil está en revisión o ya fue revisado; no se puede modificar ahora.',
      });
}

function validarPaso(cambios: GuardadoPaso): Record<string, string[]> {
  const errores: Record<string, string[]> = {};
  const datos = cambios.datosPersonales;
  if (datos && (datos.nombreCompleto.trim().length < 3 || datos.nombreCompleto.length > 120)) {
    errores['nombreCompleto'] = ['Escribe tu nombre completo (de 3 a 120 caracteres).'];
  }
  if (cambios.descripcion !== undefined && cambios.descripcion.length > 600) {
    errores['descripcion'] = ['La descripción puede tener hasta 600 caracteres.'];
  }
  for (const oficio of cambios.oficios ?? []) {
    if (oficio.aniosExperiencia < 0 || oficio.aniosExperiencia > 60) {
      errores['oficios'] = ['Los años de experiencia van de 0 a 60.'];
    }
    if (oficio.tarifaMinCentavos > oficio.tarifaMaxCentavos) {
      errores['oficios'] = ['La tarifa mínima no puede ser mayor que la máxima.'];
    }
  }
  return errores;
}

function guardarPaso(usuario: UsuarioSimulado, perfil: PerfilPropio, cuerpo: unknown) {
  const bloqueo = soloEditable(perfil);
  if (bloqueo) {
    return bloqueo;
  }
  const cambios = (cuerpo ?? {}) as GuardadoPaso;
  const errores = validarPaso(cambios);
  if (Object.keys(errores).length > 0) {
    return problema(400, 'Revisa los datos marcados.', { errors: errores });
  }
  const { paso, ...resto } = cambios;
  return ok(
    guardarPerfil(usuario.id, {
      ...perfil,
      ...resto,
      pasoAlta: Math.max(perfil.pasoAlta, paso),
    }),
  );
}

function emitirUrlSubida(perfil: PerfilPropio, cuerpo: unknown): RespuestaSimulada {
  const solicitud = (cuerpo ?? {}) as SolicitudUrlSubida;
  if (solicitud.destino === 'Documento' && !perfil.consentimientoSensibleVersion) {
    return problema(422, 'Falta tu consentimiento.', {
      detail: 'Antes de subir documentos, acepta el uso de tus datos sensibles.',
    });
  }
  const archivoId = nuevoId();
  archivosPendientes.set(archivoId, {
    destino: solicitud.destino,
    tipoContenido: solicitud.tipoContenido,
  });
  return ok({
    archivoId,
    urlSubida: `${PREFIJO_SUBIDA_SIMULADA}${archivoId}`,
    expiraEn: new Date(Date.now() + 10 * 60_000).toISOString(),
  });
}

function registrarDocumento(usuario: UsuarioSimulado, perfil: PerfilPropio, cuerpo: unknown) {
  const bloqueo = soloEditable(perfil);
  if (bloqueo) {
    return bloqueo;
  }
  const { archivoId, tipo, fechaExpedicion } = (cuerpo ?? {}) as {
    archivoId: string;
    tipo: TipoDocumento;
    fechaExpedicion?: string;
  };
  if (archivosPendientes.get(archivoId)?.destino !== 'Documento') {
    return problema(422, 'No encontramos el archivo subido. Vuelve a subirlo.');
  }
  const pideFecha = tipo === 'ComprobanteDomicilio' || tipo === 'CartaAntecedentes';
  if (pideFecha && !fechaExpedicion) {
    return problema(400, 'Falta la fecha de expedición.', {
      errors: { fechaExpedicion: ['Escribe la fecha de expedición del documento.'] },
    });
  }
  const dias = fechaExpedicion ? diasDesde(fechaExpedicion) : null;
  if (tipo === 'ComprobanteDomicilio' && (dias === null || dias > MAXIMO_DIAS_COMPROBANTE)) {
    return problema(400, 'El comprobante es muy antiguo.', {
      errors: { fechaExpedicion: ['El comprobante debe tener máximo 3 meses.'] },
    });
  }
  archivosPendientes.delete(archivoId);
  const documento: DocumentoPropio = {
    id: archivoId,
    tipo,
    estado: 'Cargado',
    ...(fechaExpedicion ? { fechaExpedicion } : {}),
  };
  const otros = perfil.documentos.filter((d) => d.tipo !== tipo);
  guardarPerfil(usuario.id, { ...perfil, documentos: [...otros, documento] });
  return creado(documento);
}

function agregarPortafolio(usuario: UsuarioSimulado, perfil: PerfilPropio, cuerpo: unknown) {
  const { archivoId, tipo, descripcion } = (cuerpo ?? {}) as {
    archivoId: string;
    tipo: 'Foto' | 'Video';
    descripcion: string;
  };
  if (archivosPendientes.get(archivoId)?.destino !== 'Portafolio') {
    return problema(422, 'No encontramos el archivo subido. Vuelve a subirlo.');
  }
  const fotos = perfil.portafolio.filter((e) => e.tipo === 'Foto').length;
  const videos = perfil.portafolio.filter((e) => e.tipo === 'Video').length;
  if ((tipo === 'Foto' && fotos >= MAXIMO_FOTOS) || (tipo === 'Video' && videos >= 1)) {
    return problema(422, 'Llegaste al límite.', {
      detail: tipo === 'Foto' ? 'Puedes subir hasta 20 fotos.' : 'Puedes subir un solo video.',
    });
  }
  if ((descripcion ?? '').length > 200) {
    return problema(400, 'Revisa la descripción.', {
      errors: { descripcion: ['La descripción puede tener hasta 200 caracteres.'] },
    });
  }
  archivosPendientes.delete(archivoId);
  const numero = (fotos % 6) + 1;
  const elemento: ElementoPortafolioPropio = {
    id: archivoId,
    tipo,
    descripcion: descripcion ?? '',
    estadoModeracion: 'Pendiente',
    url: tipo === 'Foto' ? `mocks/archivos/trabajo-ficticio-${numero}.svg` : undefined,
  };
  guardarPerfil(usuario.id, { ...perfil, portafolio: [...perfil.portafolio, elemento] });
  return creado(elemento);
}

/** Lista legible de lo que falta para enviar a revisión (422 de `enviar-revision`). */
export function faltantesParaRevision(perfil: PerfilPropio): string[] {
  const faltantes: string[] = [];
  const datos = perfil.datosPersonales;
  if (!datos?.nombreCompleto || !datos.telefono || !datos.direccion || !datos.coloniaBase) {
    faltantes.push('Datos personales completos');
  }
  if (perfil.oficios.length === 0) {
    faltantes.push('Al menos un oficio');
  } else if (perfil.oficios.some((o) => o.tarifaMinCentavos <= 0)) {
    faltantes.push('Tarifas de referencia de cada oficio');
  }
  if (perfil.cobertura.length === 0) {
    faltantes.push('Al menos un municipio de cobertura');
  }
  if (!perfil.consentimientoSensibleVersion) {
    faltantes.push('Consentimiento para datos sensibles');
  }
  for (const tipo of DOCUMENTOS_OBLIGATORIOS) {
    const documento = perfil.documentos.find((d) => d.tipo === tipo);
    if (
      !documento ||
      documento.estado === 'RequiereCorreccion' ||
      documento.estado === 'Rechazado'
    ) {
      faltantes.push(NOMBRE_DOCUMENTO[tipo]);
    }
  }
  return faltantes;
}

function enviarRevision(usuario: UsuarioSimulado, perfil: PerfilPropio): RespuestaSimulada {
  const bloqueo = soloEditable(perfil);
  if (bloqueo) {
    return bloqueo;
  }
  const fecha = perfil.datosPersonales?.fechaNacimiento ?? usuario.fechaNacimiento;
  if (!esMayorDeEdad(fecha)) {
    return problema(422, 'Necesitas 18 años o más.', {
      detail: 'Para publicar un perfil necesitas tener 18 años o más.',
      faltantes: ['Tener 18 años o más'],
    });
  }
  const faltantes = faltantesParaRevision(perfil);
  if (faltantes.length > 0) {
    return problema(422, 'Te falta completar algunos datos.', {
      detail: 'Te falta completar algunos datos antes de enviar tu perfil.',
      faltantes,
    });
  }
  guardarPerfil(usuario.id, { ...perfil, estado: 'EnRevision', motivoEstado: undefined });
  return sinContenido();
}

/** Rutas del alta del prestador (contracts/api.md, sección Alta del prestador). */
export const rutasPrestadores: readonly RutaSimulada[] = [
  ruta(
    'prestadores',
    'GET',
    '/prestadores/yo',
    conPerfil((_u, perfil) => ok(perfil)),
  ),
  ruta(
    'prestadores',
    'PATCH',
    '/prestadores/yo',
    conPerfil((usuario, perfil, cuerpo) => guardarPaso(usuario, perfil, cuerpo)),
  ),
  ruta(
    'prestadores',
    'POST',
    '/prestadores/yo/consentimiento-sensible',
    conPerfil((usuario, perfil, cuerpo) => {
      const version = (cuerpo as { version?: string } | null)?.version ?? '';
      guardarPerfil(usuario.id, { ...perfil, consentimientoSensibleVersion: version });
      return sinContenido();
    }),
  ),
  ruta(
    'prestadores',
    'POST',
    '/prestadores/yo/archivos/url-subida',
    conPerfil((_u, perfil, cuerpo) => emitirUrlSubida(perfil, cuerpo)),
  ),
  ruta(
    'prestadores',
    'POST',
    '/prestadores/yo/documentos',
    conPerfil((usuario, perfil, cuerpo) => registrarDocumento(usuario, perfil, cuerpo)),
  ),
  ruta(
    'prestadores',
    'POST',
    '/prestadores/yo/portafolio',
    conPerfil((usuario, perfil, cuerpo) => agregarPortafolio(usuario, perfil, cuerpo)),
  ),
  ruta(
    'prestadores',
    'DELETE',
    '/prestadores/yo/portafolio/:id',
    conPerfil((usuario, perfil, _cuerpo, parametros) => {
      const portafolio = perfil.portafolio.filter((e) => e.id !== parametros['id']);
      guardarPerfil(usuario.id, { ...perfil, portafolio });
      return sinContenido();
    }),
  ),
  ruta(
    'prestadores',
    'POST',
    '/prestadores/yo/enviar-revision',
    conPerfil((usuario, perfil) => enviarRevision(usuario, perfil)),
  ),
];
