import { Component, computed, effect, inject, input, resource, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { ErrorApi } from '../../core/http/errores.interceptor';
import { promedioGeneral } from '../../shared/calificaciones';
import { MxnPipe } from '../../shared/pipes/mxn';
import { DIAS_SEMANA, textoResenas, textoUnidad } from '../../shared/textos';
import { Boton } from '../../shared/ui/boton';
import { Cargando } from '../../shared/ui/cargando';
import { Estrellas } from '../../shared/ui/estrellas';
import { Insignia } from '../../shared/ui/insignia';
import { BusquedaApi } from './busqueda-api';

const RESENAS_INICIALES = 3;
const RESENAS_POR_CARGA = 5;

const EJES: readonly {
  readonly clave: 'calidad' | 'precioJusto' | 'puntualidad' | 'trato';
  readonly texto: string;
}[] = [
  { clave: 'calidad', texto: 'Calidad' },
  { clave: 'precioJusto', texto: 'Precio justo' },
  { clave: 'puntualidad', texto: 'Puntualidad y rapidez' },
  { clave: 'trato', texto: 'Trato' },
];

const formatoFecha = new Intl.DateTimeFormat('es-MX', { dateStyle: 'medium' });

/**
 * Perfil público (US2; T028). Solo muestra lo permitido por FR-017: nunca teléfono, correo,
 * dirección ni documentos. El contacto se desbloquea pagando (US3).
 */
@Component({
  selector: 'app-perfil-publico',
  imports: [RouterLink, MxnPipe, Boton, Cargando, Estrellas, Insignia],
  templateUrl: './perfil-publico.html',
  styleUrl: './perfil-publico.css',
})
export class PerfilPublico {
  private readonly api = inject(BusquedaApi);
  private readonly titulo = inject(Title);

  /** Parámetro de la ruta `prestador/:id`. */
  readonly id = input.required<string>();

  protected readonly perfil = resource({
    params: () => ({ id: this.id() }),
    loader: ({ params }) => this.api.perfil(params.id),
  });
  protected readonly cuantasResenas = signal(RESENAS_INICIALES);
  protected readonly resenas = resource({
    params: () => ({ id: this.id(), tamano: this.cuantasResenas() }),
    loader: ({ params }) => this.api.resenas(params.id, params.tamano),
  });

  protected readonly ejes = EJES;
  protected readonly textoUnidad = textoUnidad;
  protected readonly noDisponible = computed(() => {
    const error = this.perfil.error();
    return error instanceof ErrorApi && error.status === 404;
  });
  // `value()` lanza un error si el recurso falló (por ejemplo, 404): siempre se revisa `hasValue()`.
  protected readonly promedio = computed(() =>
    this.perfil.hasValue() ? promedioGeneral(this.perfil.value().promedios) : 0,
  );
  protected readonly totalResenas = computed(() =>
    textoResenas(this.perfil.hasValue() ? this.perfil.value().totalResenas : 0),
  );
  protected readonly hayMasResenas = computed(() => {
    if (!this.resenas.hasValue()) {
      return false;
    }
    const pagina = this.resenas.value();
    return pagina.items.length < pagina.total;
  });

  constructor() {
    effect(() => {
      if (this.perfil.hasValue()) {
        const perfil = this.perfil.value();
        const oficio = perfil.oficios[0]?.nombre ?? 'Prestador';
        this.titulo.setTitle(
          `${perfil.nombrePublico} · ${oficio} en ${perfil.municipio} · ServiYa`,
        );
      }
    });
  }

  protected nombreDia(dia: number): string {
    return DIAS_SEMANA.find((d) => d.dia === dia)?.nombre ?? '';
  }

  protected fecha(iso: string): string {
    return formatoFecha.format(new Date(iso));
  }

  protected promedioResena(calificaciones: Parameters<typeof promedioGeneral>[0]): number {
    return promedioGeneral(calificaciones);
  }

  protected verMasResenas(): void {
    this.cuantasResenas.update((n) => n + RESENAS_POR_CARGA);
  }
}
