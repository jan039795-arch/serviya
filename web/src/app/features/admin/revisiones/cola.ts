import { Component, inject, resource } from '@angular/core';
import { RouterLink } from '@angular/router';
import { haceCuanto } from '../../../shared/fechas/relativa';
import { mensajeGeneral } from '../../../shared/formularios/errores';
import { Boton } from '../../../shared/ui/boton';
import { Cargando } from '../../../shared/ui/cargando';
import { EstadoVacio } from '../../../shared/ui/estado-vacio';
import { RevisionesApi } from './revisiones-api';

/** Cola de perfiles por revisar, del envío más antiguo al más nuevo (FR-010, SC-003). */
@Component({
  selector: 'app-cola-revisiones',
  imports: [RouterLink, Boton, Cargando, EstadoVacio],
  template: `
    <section class="contenedor">
      <h1>Perfiles por revisar</h1>
      @if (pendientes.isLoading()) {
        <app-cargando texto="Cargando la cola…" />
      } @else if (pendientes.error()) {
        <p class="alerta alerta--error" role="alert">{{ mensajeError() }}</p>
        <button appBoton type="button" (click)="pendientes.reload()">Reintentar</button>
      } @else {
        @let lista = pendientes.hasValue() ? pendientes.value() : [];
        @if (lista.length === 0) {
          <app-estado-vacio titulo="No hay perfiles pendientes" mensaje="Todo está al día." />
        } @else {
          <p class="texto-suave">{{ lista.length }} en espera. Revisa primero los más antiguos.</p>
          <ul class="cola" data-testid="cola-revisiones">
            @for (revision of lista; track revision.prestadorId) {
              <li>
                <a [routerLink]="[revision.prestadorId]" class="cola__enlace">
                  <span class="cola__nombre">{{ revision.nombrePublico }}</span>
                  <span class="texto-suave">
                    Enviado {{ haceCuanto(revision.enviadoEn) }} ·
                    {{ revision.documentosPorRevisar }} documentos por revisar
                  </span>
                  @if (revision.esReenvio) {
                    <span class="etiqueta">Corrección reenviada</span>
                  }
                </a>
              </li>
            }
          </ul>
        }
      }
    </section>
  `,
  styles: `
    .cola {
      list-style: none;
      margin: 0;
      padding: 0;
    }
    .cola__enlace {
      display: grid;
      gap: var(--espacio-1);
      padding: var(--espacio-3) var(--espacio-4);
      margin-bottom: var(--espacio-2);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
      color: inherit;
      text-decoration: none;
    }
    .cola__enlace:hover {
      border-color: var(--color-primario);
    }
    .cola__nombre {
      font-weight: 600;
      color: var(--color-primario);
    }
    .etiqueta {
      justify-self: start;
      padding: 0 var(--espacio-2);
      border-radius: 999px;
      background: #fdf3e2;
      color: var(--color-aviso);
      font-size: var(--texto-chico);
      font-weight: 600;
    }
  `,
})
export class ColaRevisiones {
  private readonly api = inject(RevisionesApi);

  protected readonly pendientes = resource({ loader: () => this.api.pendientes() });
  protected readonly haceCuanto = haceCuanto;
  protected mensajeError(): string {
    return mensajeGeneral(this.pendientes.error());
  }
}
