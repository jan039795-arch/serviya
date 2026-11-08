import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, Injectable, signal } from '@angular/core';

export type TipoAviso = 'info' | 'exito' | 'error';

export interface Aviso {
  readonly id: number;
  readonly tipo: TipoAviso;
  readonly texto: string;
}

const DURACION_AVISO_MS = 6000;

/** Avisos breves para el usuario (errores de red, confirmaciones). */
@Injectable({ providedIn: 'root' })
export class Avisos {
  private readonly lista = signal<readonly Aviso[]>([]);
  private siguienteId = 1;

  readonly todos = this.lista.asReadonly();

  mostrar(tipo: TipoAviso, texto: string, duracionMs = DURACION_AVISO_MS): void {
    const repetido = this.lista().some((aviso) => aviso.tipo === tipo && aviso.texto === texto);
    if (repetido) {
      return;
    }
    const aviso: Aviso = { id: this.siguienteId++, tipo, texto };
    this.lista.update((lista) => [...lista, aviso]);
    setTimeout(() => this.cerrar(aviso.id), duracionMs);
  }

  cerrar(id: number): void {
    this.lista.update((lista) => lista.filter((aviso) => aviso.id !== id));
  }
}

/** Región donde se anuncian los avisos; los errores se leen de inmediato con `role="alert"`. */
@Component({
  selector: 'app-region-avisos',
  template: `
    <div class="avisos" aria-live="polite" role="status">
      @for (aviso of informativos(); track aviso.id) {
        <ng-container
          [ngTemplateOutlet]="plantilla"
          [ngTemplateOutletContext]="{ $implicit: aviso }"
        />
      }
    </div>
    <div class="avisos" role="alert">
      @for (aviso of errores(); track aviso.id) {
        <ng-container
          [ngTemplateOutlet]="plantilla"
          [ngTemplateOutletContext]="{ $implicit: aviso }"
        />
      }
    </div>
    <ng-template #plantilla let-aviso>
      <div [class]="'aviso aviso--' + aviso.tipo" data-testid="aviso">
        <p>{{ aviso.texto }}</p>
        <button type="button" class="aviso__cerrar" (click)="avisos.cerrar(aviso.id)">
          <span aria-hidden="true">×</span>
          <span class="solo-lectores">Cerrar aviso</span>
        </button>
      </div>
    </ng-template>
  `,
  imports: [NgTemplateOutlet],
  styles: `
    :host {
      position: fixed;
      inset: auto var(--espacio-4) calc(var(--alto-navegacion, 0px) + var(--espacio-4));
      z-index: 20;
      display: grid;
      gap: var(--espacio-2);
      pointer-events: none;
    }
    .avisos {
      display: grid;
      gap: var(--espacio-2);
    }
    .aviso {
      pointer-events: auto;
      display: flex;
      align-items: center;
      gap: var(--espacio-2);
      max-width: var(--ancho-max);
      margin: 0 auto;
      width: 100%;
      padding: var(--espacio-2) var(--espacio-2) var(--espacio-2) var(--espacio-4);
      border-radius: var(--radio);
      color: #fff;
      background: var(--color-texto);
      box-shadow: 0 4px 12px rgb(0 0 0 / 20%);
    }
    .aviso p {
      margin: 0;
      flex: 1;
    }
    .aviso--exito {
      background: var(--color-exito);
    }
    .aviso--error {
      background: var(--color-error);
    }
    .aviso__cerrar {
      min-width: var(--tactil-min);
      border: 0;
      background: transparent;
      color: inherit;
      font-size: 1.5rem;
      cursor: pointer;
    }
  `,
})
export class RegionAvisos {
  protected readonly avisos = inject(Avisos);
  protected readonly informativos = computed(() =>
    this.avisos.todos().filter((aviso) => aviso.tipo !== 'error'),
  );
  protected readonly errores = computed(() =>
    this.avisos.todos().filter((aviso) => aviso.tipo === 'error'),
  );
}
