import prestadores from '../../../../mocks/prestadores.json';
import revisiones from '../../../../mocks/revisiones.json';
import { DocumentoPropio, PerfilPropio, TipoDocumento, UnidadTarifa } from '../../api/modelos';
import { guardarLocal, leerLocal } from '../almacen-local';
import { guardarEnvio, guardarPerfil, perfilNuevo } from './perfiles';

/**
 * Cola de revisión inicial del prototipo: los perfiles de `revisiones.json` se cargan una sola vez
 * en el mismo almacén que los perfiles que se registran, para que el administrador los trate igual.
 */
const CLAVE_SEMILLA = 'semilla-revisiones';

const ARCHIVO_POR_TIPO: Readonly<Record<TipoDocumento, string>> = {
  IneFrente: 'mocks/archivos/documento-ficticio-ine-frente.svg',
  IneReverso: 'mocks/archivos/documento-ficticio-ine-reverso.svg',
  Selfie: 'mocks/archivos/documento-ficticio-selfie.svg',
  ComprobanteDomicilio: 'mocks/archivos/documento-ficticio-domicilio.svg',
  CartaAntecedentes: 'mocks/archivos/documento-ficticio-antecedentes.svg',
};

/**
 * Archivo ficticio que se muestra al "ver" un documento. En el prototipo las subidas son
 * simuladas, así que todo documento se ve con la imagen ficticia de su tipo.
 */
export function archivoFicticio(tipo: TipoDocumento): string {
  return ARCHIVO_POR_TIPO[tipo];
}

export function usuarioDeSemilla(prestadorId: string): string {
  return `semilla-${prestadorId}`;
}

export function asegurarSemillaRevisiones(): void {
  if (leerLocal<boolean>(CLAVE_SEMILLA)) {
    return;
  }
  for (const revision of revisiones) {
    const usuarioId = usuarioDeSemilla(revision.prestadorId);
    // Si el prestador también está en prestadores.json, se toman de ahí sus oficios y su zona.
    const publico = prestadores.find((p) => p.id === revision.prestadorId);
    const perfil: PerfilPropio = {
      ...perfilNuevo(revision.prestadorId),
      oficios: (publico?.oficios ?? []).map((o) => ({
        ...o,
        unidadTarifa: o.unidadTarifa as UnidadTarifa,
      })),
      cobertura: publico?.cobertura ?? [],
      disponibilidad: publico?.disponibilidad ?? [],
      descripcion: publico?.descripcion ?? '',
      estado: 'EnRevision',
      pasoAlta: 10,
      nombrePublico: revision.nombrePublico,
      datosPersonales: revision.datosPersonales,
      consentimientoSensibleVersion: '2026-11-borrador',
      documentos: revision.documentos.map(({ id, tipo, fechaExpedicion }): DocumentoPropio => ({
        id,
        tipo: tipo as TipoDocumento,
        estado: 'Cargado',
        ...(fechaExpedicion ? { fechaExpedicion } : {}),
      })),
      portafolio: revision.portafolio.map((elemento) => ({
        ...elemento,
        tipo: elemento.tipo as 'Foto' | 'Video',
        estadoModeracion: 'Pendiente' as const,
      })),
    };
    guardarPerfil(usuarioId, perfil);
    guardarEnvio(usuarioId, { enviadoEn: revision.enviadoEn, esReenvio: false });
  }
  guardarLocal(CLAVE_SEMILLA, true);
}
