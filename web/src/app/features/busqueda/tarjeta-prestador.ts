import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TarjetaPrestador as Tarjeta } from '../../core/api/modelos';
import { promedioGeneral } from '../../shared/calificaciones';
import { MxnPipe } from '../../shared/pipes/mxn';
import { textoResenas } from '../../shared/textos';
import { Estrellas } from '../../shared/ui/estrellas';
import { Insignia } from '../../shared/ui/insignia';

/** Tarjeta de un resultado de búsqueda (solo campos públicos). */
@Component({
  selector: 'app-tarjeta-prestador',
  imports: [RouterLink, MxnPipe, Estrellas, Insignia],
  template: `
    <a
      [routerLink]="['/prestador', tarjeta().id]"
      class="tarjeta"
      [attr.data-testid]="'tarjeta-' + tarjeta().id"
    >
      @if (tarjeta().fotoPrincipalUrl; as foto) {
        <img [src]="foto" alt="" width="96" height="96" loading="lazy" />
      } @else {
        <span class="iniciales" aria-hidden="true">{{ tarjeta().nombrePublico.charAt(0) }}</span>
      }
      <div class="datos">
        <h3>{{ tarjeta().nombrePublico }}</h3>
        <p class="oficios">{{ tarjeta().oficios.join(' · ') }}</p>
        <p class="texto-suave">{{ tarjeta().colonia }}, {{ tarjeta().municipio }}</p>
        <p class="calificacion">
          <app-estrellas [valor]="promedio()" />
          @if (tarjeta().totalResenas > 0) {
            <span class="texto-suave">({{ resenas() }})</span>
          }
        </p>
        @if (tarjeta().tarifaDesde > 0) {
          <p>
            Desde <strong>{{ tarjeta().tarifaDesde | mxn }}</strong>
          </p>
        }
        @for (insignia of tarjeta().insignias; track insignia) {
          <app-insignia [tipo]="insignia" />
        }
      </div>
    </a>
  `,
  styles: `
    .tarjeta {
      display: flex;
      gap: var(--espacio-3);
      padding: var(--espacio-3);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
      color: inherit;
      text-decoration: none;
    }
    .tarjeta:hover {
      border-color: var(--color-primario);
    }
    img,
    .iniciales {
      flex: none;
      width: 96px;
      height: 96px;
      border-radius: var(--radio);
      object-fit: cover;
    }
    .iniciales {
      display: grid;
      place-items: center;
      background: #e8f0f8;
      color: var(--color-primario-oscuro);
      font-size: 2rem;
      font-weight: 700;
    }
    .datos {
      display: grid;
      gap: 2px;
      align-content: start;
      min-width: 0;
    }
    h3 {
      margin: 0;
      color: var(--color-primario);
      font-size: var(--texto-base);
    }
    p {
      margin: 0;
    }
    .oficios {
      font-weight: 600;
    }
    .calificacion {
      display: flex;
      align-items: center;
      gap: var(--espacio-1);
    }
    app-insignia {
      justify-self: start;
    }
  `,
})
export class TarjetaPrestador {
  readonly tarjeta = input.required<Tarjeta>();
  protected readonly promedio = computed(() => promedioGeneral(this.tarjeta().promedios));
  protected readonly resenas = computed(() => textoResenas(this.tarjeta().totalResenas));
}
