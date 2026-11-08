import { Component, input } from '@angular/core';

/** Mensaje para listas sin resultados, con una acción opcional proyectada. */
@Component({
  selector: 'app-estado-vacio',
  template: `
    <h2>{{ titulo() }}</h2>
    @if (mensaje()) {
      <p>{{ mensaje() }}</p>
    }
    <ng-content />
  `,
  styles: `
    :host {
      display: block;
      padding: var(--espacio-8) var(--espacio-4);
      text-align: center;
      color: var(--color-texto-suave);
    }
    h2 {
      color: var(--color-texto);
    }
  `,
})
export class EstadoVacio {
  readonly titulo = input.required<string>();
  readonly mensaje = input<string>();
}
