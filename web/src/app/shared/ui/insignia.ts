import { Component, computed, input } from '@angular/core';
import { Insignia as TipoInsignia } from '../../core/api/modelos';

const TEXTOS: Readonly<Record<TipoInsignia, string>> = {
  IdentidadVerificada: 'Identidad verificada',
  AntecedentesPresentados: 'Antecedentes presentados',
};

/** Insignia de confianza del prestador (constitución, principio III). */
@Component({
  selector: 'app-insignia',
  template: `
    <svg aria-hidden="true" viewBox="0 0 24 24" width="16" height="16">
      <path d="m5 12 5 5 9-10" />
    </svg>
    {{ texto() }}
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: var(--espacio-1);
      padding: 2px var(--espacio-2);
      border-radius: 999px;
      background: #e3f1e6;
      color: var(--color-exito);
      font-size: var(--texto-chico);
      font-weight: 600;
    }
    svg {
      fill: none;
      stroke: currentColor;
      stroke-width: 3;
    }
  `,
})
export class Insignia {
  readonly tipo = input.required<TipoInsignia>();
  protected readonly texto = computed(() => TEXTOS[this.tipo()]);
}
