import { Component, input } from '@angular/core';

/** Indicador de carga que se anuncia a los lectores de pantalla. */
@Component({
  selector: 'app-cargando',
  template: `
    <span class="giro" aria-hidden="true"></span>
    <span>{{ texto() }}</span>
  `,
  host: { role: 'status' },
  styles: `
    :host {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--espacio-2);
      padding: var(--espacio-6);
      color: var(--color-texto-suave);
    }
    .giro {
      width: 1.25rem;
      height: 1.25rem;
      border: 3px solid #d5dbe1;
      border-top-color: var(--color-primario);
      border-radius: 50%;
      animation: girar 0.8s linear infinite;
    }
    @keyframes girar {
      to {
        transform: rotate(360deg);
      }
    }
  `,
})
export class Cargando {
  readonly texto = input('Cargando…');
}
