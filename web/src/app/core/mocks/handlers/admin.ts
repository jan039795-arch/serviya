import {
  DecisionDocumento,
  DecisionModeracion,
  DecisionPerfil,
  DocumentoPropio,
  Insignia,
  PerfilPropio,
  PerfilRevision,
  RevisionPendiente,
} from '../../api/modelos';
import { registrarEnBitacora } from '../estado/bitacora';
import { buscarPerfil, envioDe, guardarPerfil, todosLosPerfiles } from '../estado/perfiles';
import { archivoFicticio, asegurarSemillaRevisiones } from '../estado/revisiones';
import { usuarioActual, UsuarioSimulado } from '../estado/usuarios';
import { ok, problema, RespuestaSimulada, ruta, RutaSimulada, sinContenido } from '../simulacion';
import { DOCUMENTOS_OBLIGATORIOS } from './prestadores';

const MINUTOS_URL_DOCUMENTO = 5;

interface CuerpoDecision {
  readonly decision?: string;
  readonly motivo?: string;
}

type ManejadorAdmin = (
  admin: UsuarioSimulado,
  parametros: Readonly<Record<string, string>>,
  cuerpo: CuerpoDecision,
  consulta: URLSearchParams,
) => RespuestaSimulada;

/** Exige sesión con rol `Admin` (la API real además registra todo en la bitácora). */
function soloAdmin(manejar: ManejadorAdmin): RutaSimulada['manejar'] {
  return ({ parametros, cuerpo, consulta }) => {
    const usuario = usuarioActual();
    if (!usuario) {
      return problema(401, 'No has iniciado sesión.');
    }
    if (!usuario.roles.includes('Admin')) {
      return problema(403, 'Solo para administradores.');
    }
    asegurarSemillaRevisiones();
    return manejar(usuario, parametros, (cuerpo ?? {}) as CuerpoDecision, consulta);
  };
}

function motivoObligatorio(cuerpo: CuerpoDecision): RespuestaSimulada | null {
  return cuerpo.decision !== 'Aprobar' && !cuerpo.motivo?.trim()
    ? problema(400, 'Escribe el motivo.', {
        errors: { motivo: ['El motivo es obligatorio al rechazar o pedir corrección.'] },
      })
    : null;
}

function aRevision(usuarioId: string, perfil: PerfilPropio): PerfilRevision {
  return {
    prestadorId: perfil.id,
    nombrePublico: perfil.nombrePublico ?? 'Sin nombre',
    estado: perfil.estado,
    enviadoEn: envioDe(usuarioId)?.enviadoEn,
    datosPersonales: perfil.datosPersonales,
    oficios: perfil.oficios,
    cobertura: perfil.cobertura,
    disponibilidad: perfil.disponibilidad,
    descripcion: perfil.descripcion,
    documentos: perfil.documentos.map((d) => ({ ...d, tipoContenido: 'image/svg+xml' })),
    portafolio: perfil.portafolio,
    insignias: perfil.insignias,
  };
}

function cola(consulta: URLSearchParams): RespuestaSimulada {
  const estado = consulta.get('estado') ?? 'EnRevision';
  const pendientes: RevisionPendiente[] = todosLosPerfiles()
    .filter(([, perfil]) => perfil.estado === estado)
    .map(([usuarioId, perfil]) => {
      const envio = envioDe(usuarioId);
      return {
        prestadorId: perfil.id,
        nombrePublico: perfil.nombrePublico ?? 'Sin nombre',
        enviadoEn: envio?.enviadoEn ?? new Date().toISOString(),
        esReenvio: envio?.esReenvio ?? false,
        documentosPorRevisar: perfil.documentos.filter((d) => d.estado === 'Cargado').length,
      };
    })
    .sort((a, b) => a.enviadoEn.localeCompare(b.enviadoEn));
  return ok(pendientes);
}

/** Busca el documento en todos los perfiles; los Ids de documento son únicos. */
function buscarDocumento(
  documentoId: string,
): { usuarioId: string; perfil: PerfilPropio; documento: DocumentoPropio } | null {
  for (const [usuarioId, perfil] of todosLosPerfiles()) {
    const documento = perfil.documentos.find((d) => d.id === documentoId);
    if (documento) {
      return { usuarioId, perfil, documento };
    }
  }
  return null;
}

function verDocumento(admin: UsuarioSimulado, documentoId: string): RespuestaSimulada {
  const hallazgo = buscarDocumento(documentoId);
  if (!hallazgo) {
    return problema(404, 'No encontramos el documento.');
  }
  registrarEnBitacora(admin.id, 'VerDocumento', 'Documento', documentoId);
  return ok({
    url: archivoFicticio(hallazgo.documento.tipo),
    expiraEn: new Date(Date.now() + MINUTOS_URL_DOCUMENTO * 60_000).toISOString(),
  });
}

const ESTADO_POR_DECISION: Readonly<Record<DecisionDocumento, DocumentoPropio['estado']>> = {
  Aprobar: 'Aprobado',
  RequiereCorreccion: 'RequiereCorreccion',
  Rechazar: 'Rechazado',
};

function decidirDocumento(
  admin: UsuarioSimulado,
  documentoId: string,
  cuerpo: CuerpoDecision,
): RespuestaSimulada {
  const sinMotivo = motivoObligatorio(cuerpo);
  if (sinMotivo) {
    return sinMotivo;
  }
  const decision = cuerpo.decision as DecisionDocumento;
  if (!ESTADO_POR_DECISION[decision]) {
    return problema(400, 'Decisión no válida.');
  }
  const hallazgo = buscarDocumento(documentoId);
  if (!hallazgo) {
    return problema(404, 'No encontramos el documento.');
  }
  const { usuarioId, perfil, documento } = hallazgo;
  const actualizado: DocumentoPropio = {
    ...documento,
    estado: ESTADO_POR_DECISION[decision],
    motivo: decision === 'Aprobar' ? undefined : cuerpo.motivo?.trim(),
  };
  guardarPerfil(usuarioId, {
    ...perfil,
    documentos: perfil.documentos.map((d) => (d.id === documentoId ? actualizado : d)),
  });
  registrarEnBitacora(admin.id, `${decision}Documento`, 'Documento', documentoId);
  return sinContenido();
}

function insigniasAlAprobar(perfil: PerfilPropio): Insignia[] {
  const carta = perfil.documentos.find((d) => d.tipo === 'CartaAntecedentes');
  return carta?.estado === 'Aprobado'
    ? ['IdentidadVerificada', 'AntecedentesPresentados']
    : ['IdentidadVerificada'];
}

function decidirPerfil(
  admin: UsuarioSimulado,
  prestadorId: string,
  cuerpo: CuerpoDecision,
): RespuestaSimulada {
  const sinMotivo = cuerpo.decision === 'Rechazar' ? motivoObligatorio(cuerpo) : null;
  if (sinMotivo) {
    return sinMotivo;
  }
  const hallazgo = buscarPerfil(prestadorId);
  if (!hallazgo) {
    return problema(404, 'No encontramos el perfil.');
  }
  const { usuarioId, perfil } = hallazgo;
  if (perfil.estado !== 'EnRevision') {
    return problema(409, 'Este perfil ya no está en revisión.');
  }
  const decision = cuerpo.decision as DecisionPerfil;
  if (decision === 'Aprobar') {
    const pendientes = DOCUMENTOS_OBLIGATORIOS.filter(
      (tipo) => perfil.documentos.find((d) => d.tipo === tipo)?.estado !== 'Aprobado',
    );
    if (pendientes.length > 0) {
      return problema(422, 'Faltan documentos por aprobar.', {
        detail: 'Para aprobar el perfil, primero aprueba todos los documentos obligatorios.',
      });
    }
    guardarPerfil(usuarioId, {
      ...perfil,
      estado: 'Aprobado',
      motivoEstado: undefined,
      insignias: insigniasAlAprobar(perfil),
    });
  } else if (decision === 'RequiereCorreccion') {
    if (!perfil.documentos.some((d) => d.estado === 'RequiereCorreccion')) {
      return problema(422, 'Marca qué documento debe corregirse.', {
        detail: 'Para pedir correcciones, marca al menos un documento como "requiere corrección".',
      });
    }
    guardarPerfil(usuarioId, {
      ...perfil,
      estado: 'RequiereCorreccion',
      motivoEstado: cuerpo.motivo?.trim() || 'Revisa los documentos señalados.',
    });
  } else if (decision === 'Rechazar') {
    guardarPerfil(usuarioId, {
      ...perfil,
      estado: 'Rechazado',
      motivoEstado: cuerpo.motivo?.trim(),
    });
  } else {
    return problema(400, 'Decisión no válida.');
  }
  registrarEnBitacora(admin.id, `${decision}Perfil`, 'Prestador', prestadorId);
  return sinContenido();
}

function decidirPortafolio(
  admin: UsuarioSimulado,
  elementoId: string,
  cuerpo: CuerpoDecision,
): RespuestaSimulada {
  const decision = cuerpo.decision as DecisionModeracion;
  const sinMotivo = decision === 'Rechazar' ? motivoObligatorio(cuerpo) : null;
  if (sinMotivo) {
    return sinMotivo;
  }
  for (const [usuarioId, perfil] of todosLosPerfiles()) {
    if (perfil.portafolio.some((e) => e.id === elementoId)) {
      guardarPerfil(usuarioId, {
        ...perfil,
        portafolio: perfil.portafolio.map((e) =>
          e.id === elementoId
            ? {
                ...e,
                estadoModeracion: decision === 'Aprobar' ? 'Aprobado' : 'Rechazado',
                motivoRechazo: decision === 'Aprobar' ? undefined : cuerpo.motivo?.trim(),
              }
            : e,
        ),
      });
      registrarEnBitacora(admin.id, `${decision}Portafolio`, 'ElementoPortafolio', elementoId);
      return sinContenido();
    }
  }
  return problema(404, 'No encontramos la foto o el video.');
}

/** Rutas de revisión del administrador (contracts/api.md, sección Administración). */
export const rutasAdmin: readonly RutaSimulada[] = [
  ruta(
    'admin',
    'GET',
    '/admin/revisiones',
    soloAdmin((_a, _p, _c, consulta) => cola(consulta)),
  ),
  ruta(
    'admin',
    'GET',
    '/admin/prestadores/:id',
    soloAdmin((_a, parametros) => {
      const hallazgo = buscarPerfil(parametros['id']);
      return hallazgo
        ? ok(aRevision(hallazgo.usuarioId, hallazgo.perfil))
        : problema(404, 'No encontramos el perfil.');
    }),
  ),
  ruta(
    'admin',
    'POST',
    '/admin/documentos/:id/ver',
    soloAdmin((admin, parametros) => verDocumento(admin, parametros['id'])),
  ),
  ruta(
    'admin',
    'POST',
    '/admin/documentos/:id/decision',
    soloAdmin((admin, parametros, cuerpo) => decidirDocumento(admin, parametros['id'], cuerpo)),
  ),
  ruta(
    'admin',
    'POST',
    '/admin/prestadores/:id/decision',
    soloAdmin((admin, parametros, cuerpo) => decidirPerfil(admin, parametros['id'], cuerpo)),
  ),
  ruta(
    'admin',
    'POST',
    '/admin/portafolio/:id/decision',
    soloAdmin((admin, parametros, cuerpo) => decidirPortafolio(admin, parametros['id'], cuerpo)),
  ),
];
