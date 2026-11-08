import { Component } from '@angular/core';

/** Contenedor con borde y separación para agrupar contenido. */
@Component({
  selector: 'app-tarjeta',
  template: `<ng-content />`,
  styles: `
    :host {
      display: block;
      padding: var(--espacio-4);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
      background: var(--color-fondo);
    }
  `,
})
export class Tarjeta {}
