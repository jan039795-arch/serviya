import oficios from '../../../../mocks/oficios.json';
import resenas from '../../../../mocks/resenas.json';
import { Insignia, Pagina, ResenaPublica, TarjetaPrestador } from '../../api/modelos';
import {
  aPerfilPublico,
  aTarjeta,
  buscarPublicado,
  PrestadorDirectorio,
  promedioGlobal,
  publicados,
} from '../estado/directorio';
import { promedioGeneral, puntajeBayesiano } from '../estado/ranking';
import { ok, problema, RespuestaSimulada, ruta, RutaSimulada } from '../simulacion';

const TAMANO_PREDETERMINADO = 10;
const TAMANO_MAXIMO = 50;
/** Ventaja para quien tiene su base en el municipio buscado (cercanía, FR-019). */
const BONO_CERCANIA = 0.15;

function entero(valor: string | null, predeterminado: number, maximo: number): number {
  const numero = Number(valor);
  return Number.isInteger(numero) && numero > 0 ? Math.min(numero, maximo) : predeterminado;
}

function paginar<T>(lista: readonly T[], consulta: URLSearchParams): Pagina<T> {
  const pagina = entero(consulta.get('pagina'), 1, 1000);
  const tamano = entero(consulta.get('tamano'), TAMANO_PREDETERMINADO, TAMANO_MAXIMO);
  const inicio = (pagina - 1) * tamano;
  return { items: lista.slice(inicio, inicio + tamano), total: lista.length, pagina, tamano };
}

interface Filtros {
  readonly oficioId?: string;
  readonly municipioId?: string;
  readonly calificacionMin?: number;
  readonly insignia?: Insignia;
  readonly tarifaMaxCentavos?: number;
}

function leerFiltros(consulta: URLSearchParams): Filtros {
  const oficio = consulta.get('oficio');
  const calificacion = Number(consulta.get('calificacionMin'));
  const tarifa = Number(consulta.get('tarifaMax'));
  return {
    oficioId: oficios.find((o) => o.slug === oficio || o.id === oficio)?.id,
    municipioId: consulta.get('municipio') ?? undefined,
    calificacionMin: calificacion > 0 ? calificacion : undefined,
    insignia: (consulta.get('insignia') as Insignia | null) ?? undefined,
    tarifaMaxCentavos: tarifa > 0 ? tarifa : undefined,
  };
}

function cumple(p: PrestadorDirectorio, filtros: Filtros): boolean {
  const oficiosBuscados = filtros.oficioId
    ? p.oficios.filter((o) => o.oficioId === filtros.oficioId)
    : p.oficios;
  return (
    oficiosBuscados.length > 0 &&
    (!filtros.municipioId || p.cobertura.includes(filtros.municipioId)) &&
    (!filtros.calificacionMin || promedioGeneral(p.promedios) >= filtros.calificacionMin) &&
    (!filtros.insignia || p.insignias.includes(filtros.insignia)) &&
    (!filtros.tarifaMaxCentavos ||
      oficiosBuscados.some((o) => o.tarifaMinCentavos <= (filtros.tarifaMaxCentavos ?? 0)))
  );
}

function buscar(consulta: URLSearchParams): RespuestaSimulada {
  if (consulta.get('oficio') && !leerFiltros(consulta).oficioId) {
    return ok(paginar<TarjetaPrestador>([], consulta));
  }
  const filtros = leerFiltros(consulta);
  const lista = publicados();
  const global = promedioGlobal(lista);
  const puntaje = (p: PrestadorDirectorio): number =>
    puntajeBayesiano(promedioGeneral(p.promedios), p.totalResenas, global) +
    (filtros.municipioId && p.municipioId === filtros.municipioId ? BONO_CERCANIA : 0);
  const tarjetas = lista
    .filter((p) => cumple(p, filtros))
    .sort((a, b) => puntaje(b) - puntaje(a))
    .map((p) => aTarjeta(p, filtros.oficioId));
  return ok(paginar(tarjetas, consulta));
}

function resenasDe(prestadorId: string, consulta: URLSearchParams): RespuestaSimulada {
  if (!buscarPublicado(prestadorId)) {
    return problema(404, 'Este perfil no está disponible.');
  }
  const lista: ResenaPublica[] = resenas
    .filter((r) => r.prestadorId === prestadorId)
    .map(({ id, autor, calificaciones, comentario, fotosUrls, publicadaEn, respuesta }) => ({
      id,
      autor,
      calificaciones,
      comentario,
      fotosUrls,
      publicadaEn,
      respuesta,
    }))
    .sort((a, b) => b.publicadaEn.localeCompare(a.publicadaEn));
  return ok(paginar(lista, consulta));
}

/** Búsqueda y perfiles públicos (contracts/api.md, sección Búsqueda y perfiles públicos). */
export const rutasBusqueda: readonly RutaSimulada[] = [
  ruta('busqueda', 'GET', '/prestadores', ({ consulta }) => buscar(consulta)),
  ruta('busqueda', 'GET', '/prestadores/:id', ({ parametros }) => {
    const prestador = buscarPublicado(parametros['id']);
    // Pendiente, rechazado, suspendido o eliminado: 404, igual que si no existiera (US2, escenario 3).
    return prestador
      ? ok(aPerfilPublico(prestador))
      : problema(404, 'Este perfil no está disponible.');
  }),
  ruta('busqueda', 'GET', '/prestadores/:id/resenas', ({ parametros, consulta }) =>
    resenasDe(parametros['id'], consulta),
  ),
];
