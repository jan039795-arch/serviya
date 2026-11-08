import { Component, computed, inject, OnInit } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { Boton } from '../../shared/ui/boton';
import { Cargando } from '../../shared/ui/cargando';
import { AltaEstado } from './alta-estado';
import { PASOS_ALTA, TOTAL_PASOS } from './pasos';

/** Contenedor del alta: avance, mensajes comunes y el paso actual (T020). */
@Component({
  selector: 'app-alta',
  imports: [RouterOutlet, RouterLink, Boton, Cargando],
  template: `
    <section class="contenedor">
      @if (paso(); as actual) {
        <p class="avance-texto" data-testid="avance">
          Paso {{ actual.numero }} de {{ total }} · {{ actual.titulo }}
        </p>
        <progress
          class="avance"
          [max]="total"
          [value]="actual.numero"
          [attr.aria-label]="'Paso ' + actual.numero + ' de ' + total"
        ></progress>
      }

      @if (estado.errorCarga(); as error) {
        <p class="alerta alerta--error" role="alert">{{ error }}</p>
        <button appBoton type="button" (click)="estado.cargar()">Reintentar</button>
      } @else if (!estado.perfil()) {
        <app-cargando texto="Cargando tu perfil…" />
      } @else {
        @if (paso() && !estado.editable()) {
          <p class="alerta alerta--info">
            Tu perfil ya fue enviado y no se puede modificar por ahora.
            <a routerLink="/alta/estado">Ver el estado de tu perfil</a>
          </p>
        }
        @if (estado.errorGuardado(); as error) {
          <p class="alerta alerta--error" role="alert" data-testid="error-guardado">{{ error }}</p>
        }
        <router-outlet />
      }
    </section>
  `,
  styles: `
    .avance-texto {
      margin: 0 0 var(--espacio-1);
      color: var(--color-texto-suave);
      font-size: var(--texto-chico);
    }
    .avance {
      width: 100%;
      height: 0.5rem;
      margin-bottom: var(--espacio-4);
      accent-color: var(--color-primario);
    }
  `,
})
export class Alta implements OnInit {
  protected readonly estado = inject(AltaEstado);
  private readonly router = inject(Router);

  protected readonly total = TOTAL_PASOS;
  private readonly url = toSignal(
    this.router.events.pipe(
      filter((evento) => evento instanceof NavigationEnd),
      map((evento) => evento.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );
  protected readonly paso = computed(() => {
    const numero = Number(/^\/alta\/(\d+)/.exec(this.url())?.[1]);
    return PASOS_ALTA.find((p) => p.numero === numero) ?? null;
  });

  ngOnInit(): void {
    void this.estado.cargar();
  }
}
