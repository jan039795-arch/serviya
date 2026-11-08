import { booleanAttribute, Directive, input } from '@angular/core';

export type VarianteBoton = 'primario' | 'secundario' | 'texto' | 'peligro';

/**
 * Estilo de botón sobre elementos nativos (`<button appBoton>` o `<a appBoton>`), para conservar
 * su semántica y su comportamiento con teclado. Los estilos están en `styles.css`.
 */
@Directive({
  selector: 'button[appBoton], a[appBoton]',
  host: {
    class: 'boton',
    '[class.boton--secundario]': 'variante() === "secundario"',
    '[class.boton--texto]': 'variante() === "texto"',
    '[class.boton--peligro]': 'variante() === "peligro"',
    '[class.boton--ancho]': 'anchoCompleto()',
  },
})
export class Boton {
  readonly variante = input<VarianteBoton>('primario');
  readonly anchoCompleto = input(false, { transform: booleanAttribute });
}
