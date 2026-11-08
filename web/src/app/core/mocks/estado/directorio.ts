import municipios from '../../../../mocks/municipios.json';
import oficios from '../../../../mocks/oficios.json';
import prestadores from '../../../../mocks/prestadores.json';
import {
  EstadoPerfil,
  FotoPublica,
  FranjaDisponibilidad,
  Insignia,
  OficioDelPrestador,
  PerfilPropio,
  PerfilPublico,
  TarjetaPrestador,
  UnidadTarifa,
} from '../../api/modelos';
import { todosLosPerfiles } from './perfiles';
import { promedioGeneral, Promedios } from './ranking';

/**
 * Directorio interno del prototipo: une los prestadores de `prestadores.json` con los perfiles que
 * se aprueban en el prototipo. Contiene datos privados (teléfono) que **nunca** salen tal cual:
 * las respuestas públicas se arman campo por campo con `aTarjeta` y `aPerfilPublico`.
 */
export interface PrestadorDirectorio {
  readonly id: string;
  readonly estado: EstadoPerfil;
  readonly nombrePublico: string;
  readonly colonia: string;
  readonly municipioId: string;
  readonly descripcion: string;
  readonly oficios: readonly OficioDelPrestador[];
  readonly cobertura: readonly string[];
  readonly disponibilidad: readonly FranjaDisponibilidad[];
  readonly insignias: readonly Insignia[];
  readonly promedios: Promedios;
  readonly totalResenas: number;
  readonly fotos: readonly FotoPublica[];
  readonly videoUrl?: string;
  /** Privado: solo para `GET /accesos/{id}/contacto` con acceso activo (T032). */
  readonly telefono: string;
}

const SIN_CALIFICACIONES: Promedios = { calidad: 0, precioJusto: 0, puntualidad: 0, trato: 0 };

function desdeSemilla(): PrestadorDirectorio[] {
  return prestadores.map((p) => ({
    id: p.id,
    estado: p.estado as EstadoPerfil,
    nombrePublico: p.nombrePublico,
    colonia: p.colonia,
    municipioId: p.municipioId,
    descripcion: p.descripcion,
    oficios: p.oficios.map((o) => ({ ...o, unidadTarifa: o.unidadTarifa as UnidadTarifa })),
    cobertura: p.cobertura,
    disponibilidad: p.disponibilidad,
    insignias: p.insignias as Insignia[],
    promedios: p.promedios,
    totalResenas: p.totalResenas,
    fotos: p.fotos,
    telefono: p.telefonoFicticio,
  }));
}

function desdePerfil(perfil: PerfilPropio): PrestadorDirectorio {
  const aprobados = perfil.portafolio.filter((e) => e.estadoModeracion === 'Aprobado');
  return {
    id: perfil.id,
    estado: perfil.estado,
    nombrePublico: perfil.nombrePublico ?? '',
    colonia: perfil.datosPersonales?.coloniaBase ?? '',
    municipioId: perfil.cobertura[0] ?? '',
    descripcion: perfil.descripcion,
    oficios: perfil.oficios,
    cobertura: perfil.cobertura,
    disponibilidad: perfil.disponibilidad,
    insignias: perfil.insignias,
    promedios: SIN_CALIFICACIONES,
    totalResenas: 0,
    fotos: aprobados
      .filter((e) => e.tipo === 'Foto' && e.url)
      .map((e) => ({ url: e.url ?? '', descripcion: e.descripcion })),
    videoUrl: undefined,
    telefono: perfil.datosPersonales?.telefono ?? '',
  };
}

/** Todos los prestadores; los del prototipo reemplazan a los de la semilla con el mismo Id. */
export function directorio(): readonly PrestadorDirectorio[] {
  const propios = todosLosPerfiles().map(([, perfil]) => desdePerfil(perfil));
  const idsPropios = new Set(propios.map((p) => p.id));
  return [...desdeSemilla().filter((p) => !idsPropios.has(p.id)), ...propios];
}

export function publicados(): readonly PrestadorDirectorio[] {
  return directorio().filter((p) => p.estado === 'Aprobado');
}

export function buscarPublicado(id: string): PrestadorDirectorio | undefined {
  return publicados().find((p) => p.id === id);
}

function nombreMunicipio(id: string): string {
  return municipios.find((m) => m.id === id)?.nombre ?? '';
}

function nombreOficio(id: string): string {
  return oficios.find((o) => o.id === id)?.nombre ?? '';
}

/** DTO público de la tarjeta de búsqueda: solo los campos permitidos (FR-017). */
export function aTarjeta(p: PrestadorDirectorio, oficioId?: string): TarjetaPrestador {
  const delOficio = oficioId ? p.oficios.filter((o) => o.oficioId === oficioId) : p.oficios;
  const tarifas = (delOficio.length > 0 ? delOficio : p.oficios).map((o) => o.tarifaMinCentavos);
  return {
    id: p.id,
    nombrePublico: p.nombrePublico,
    colonia: p.colonia,
    municipio: nombreMunicipio(p.municipioId),
    oficios: p.oficios.map((o) => nombreOficio(o.oficioId)),
    tarifaDesde: tarifas.length > 0 ? Math.min(...tarifas) : 0,
    promedios: { ...p.promedios },
    totalResenas: p.totalResenas,
    insignias: [...p.insignias],
    fotoPrincipalUrl: p.fotos[0]?.url,
  };
}

/** DTO público del perfil: nunca teléfono, correo, dirección ni documentos (FR-015, FR-017). */
export function aPerfilPublico(p: PrestadorDirectorio): PerfilPublico {
  return {
    id: p.id,
    nombrePublico: p.nombrePublico,
    colonia: p.colonia,
    municipio: nombreMunicipio(p.municipioId),
    descripcion: p.descripcion,
    oficios: p.oficios.map((o) => ({
      oficioId: o.oficioId,
      nombre: nombreOficio(o.oficioId),
      aniosExperiencia: o.aniosExperiencia,
      tarifaMinCentavos: o.tarifaMinCentavos,
      tarifaMaxCentavos: o.tarifaMaxCentavos,
      unidadTarifa: o.unidadTarifa,
    })),
    disponibilidad: p.disponibilidad.map(({ dia, desde, hasta }) => ({ dia, desde, hasta })),
    videoUrl: p.videoUrl,
    fotos: p.fotos.map(({ url, descripcion }) => ({ url, descripcion })),
    insignias: [...p.insignias],
    promedios: { ...p.promedios },
    totalResenas: p.totalResenas,
  };
}

export function promedioGlobal(lista: readonly PrestadorDirectorio[]): number {
  const conResenas = lista.filter((p) => p.totalResenas > 0);
  if (conResenas.length === 0) {
    return 0;
  }
  return conResenas.reduce((suma, p) => suma + promedioGeneral(p.promedios), 0) / conResenas.length;
}
