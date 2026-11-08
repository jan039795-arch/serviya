import { Component, computed, inject, input, linkedSignal, resource } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CatalogosApi } from '../../core/api/catalogos-api';
import { Insignia } from '../../core/api/modelos';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { Boton } from '../../shared/ui/boton';
import { Cargando } from '../../shared/ui/cargando';
import { EstadoVacio } from '../../shared/ui/estado-vacio';
import { valorDe } from '../../shared/recursos';
import { BusquedaApi, FiltrosBusqueda } from './busqueda-api';
import { TarjetaPrestador } from './tarjeta-prestador';

const POR_PAGINA = 10;

interface FiltrosEditables {
  calificacionMin: string;
  insignia: string;
  tarifaMaxPesos: string;
}

/**
 * Resultados de búsqueda (US2; T027). Los filtros viven en la URL para poder compartir o volver a
 * la misma búsqueda.
 */
@Component({
  selector: 'app-resultados',
  imports: [RouterLink, Boton, Cargando, EstadoVacio, TarjetaPrestador],
  templateUrl: './resultados.html',
  styles: `
    .resumen {
      margin-bottom: var(--espacio-3);
    }
    details {
      margin-bottom: var(--espacio-4);
      padding: var(--espacio-2) var(--espacio-3);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
    }
    summary {
      min-height: var(--tactil-min);
      display: flex;
      align-items: center;
      font-weight: 600;
      cursor: pointer;
    }
    .filtros {
      display: grid;
      gap: var(--espacio-2);
      padding-bottom: var(--espacio-2);
    }
    .filtros label {
      display: grid;
      gap: var(--espacio-1);
    }
    .lista {
      display: grid;
      gap: var(--espacio-3);
      margin: 0 0 var(--espacio-4);
      padding: 0;
      list-style: none;
    }
  `,
})
export class Resultados {
  private readonly api = inject(BusquedaApi);
  private readonly catalogos = inject(CatalogosApi);
  private readonly router = inject(Router);

  // Parámetros de la URL (`withComponentInputBinding`).
  readonly oficio = input<string>();
  readonly municipio = input<string>();
  readonly calificacionMin = input<string>();
  readonly insignia = input<string>();
  readonly tarifaMax = input<string>();

  private readonly oficios = resource({ loader: () => this.catalogos.oficios() });
  private readonly municipios = resource({ loader: () => this.catalogos.municipios() });

  /** Cuántas páginas se muestran; vuelve a 1 cuando cambia la búsqueda. */
  protected readonly paginas = linkedSignal({
    source: () => [
      this.oficio(),
      this.municipio(),
      this.calificacionMin(),
      this.insignia(),
      this.tarifaMax(),
    ],
    computation: () => 1,
  });

  private readonly filtros = computed<FiltrosBusqueda>(() => ({
    oficio: this.oficio(),
    municipio: this.municipio(),
    calificacionMin: numeroPositivo(this.calificacionMin()),
    insignia: insigniaValida(this.insignia()),
    tarifaMax: numeroPositivo(this.tarifaMax()),
    tamano: this.paginas() * POR_PAGINA,
  }));

  protected readonly resultados = resource({
    params: () => this.filtros(),
    loader: ({ params }) => this.api.buscar(params),
  });

  protected readonly editables = linkedSignal<FiltrosEditables>(() => ({
    calificacionMin: this.calificacionMin() ?? '',
    insignia: this.insignia() ?? '',
    tarifaMaxPesos: this.tarifaMax() ? String(Number(this.tarifaMax()) / 100) : '',
  }));

  protected readonly nombreOficio = computed(
    () => valorDe(this.oficios)?.find((o) => o.slug === this.oficio())?.nombre ?? 'Prestadores',
  );
  protected readonly nombreMunicipio = computed(
    () => valorDe(this.municipios)?.find((m) => m.id === this.municipio())?.nombre,
  );
  protected readonly hayFiltros = computed(
    () => !!(this.calificacionMin() || this.insignia() || this.tarifaMax()),
  );
  protected readonly hayMas = computed(() => {
    const pagina = valorDe(this.resultados);
    return !!pagina && pagina.items.length < pagina.total;
  });

  protected mensajeError(): string {
    return mensajeGeneral(this.resultados.error());
  }

  protected editar(campo: keyof FiltrosEditables, evento: Event): void {
    const valor = (evento.target as HTMLInputElement | HTMLSelectElement).value;
    this.editables.update((actual) => ({ ...actual, [campo]: valor }));
  }

  protected aplicar(evento: Event): void {
    evento.preventDefault();
    const { calificacionMin, insignia, tarifaMaxPesos } = this.editables();
    const pesos = Number(tarifaMaxPesos);
    void this.router.navigate([], {
      queryParams: {
        calificacionMin: calificacionMin || null,
        insignia: insignia || null,
        tarifaMax: pesos > 0 ? Math.round(pesos * 100) : null,
      },
      queryParamsHandling: 'merge',
    });
  }

  protected limpiar(): void {
    void this.router.navigate([], {
      queryParams: { calificacionMin: null, insignia: null, tarifaMax: null },
      queryParamsHandling: 'merge',
    });
  }

  protected verMas(): void {
    this.paginas.update((n) => n + 1);
  }
}

function numeroPositivo(valor: string | undefined): number | undefined {
  const numero = Number(valor);
  return Number.isFinite(numero) && numero > 0 ? numero : undefined;
}

const INSIGNIAS: readonly Insignia[] = ['IdentidadVerificada', 'AntecedentesPresentados'];

/** Solo se envía a la API una insignia conocida; cualquier otro valor de la URL se ignora. */
function insigniaValida(valor: string | undefined): Insignia | undefined {
  return INSIGNIAS.find((insignia) => insignia === valor);
}
