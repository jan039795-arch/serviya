import { booleanAttribute, Component, computed, input } from '@angular/core';

const MAXIMO = 5;
const formato = new Intl.NumberFormat('es-MX', {
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
});

/** Calificación de solo lectura. Los lectores de pantalla oyen, por ejemplo, "4.5 de 5". */
@Component({
  selector: 'app-estrellas',
  template: `
    @if (valor() > 0) {
      <span class="estrellas" role="img" [attr.aria-label]="etiqueta()">
        <span class="estrellas__fondo" aria-hidden="true">★★★★★</span>
        <span class="estrellas__relleno" aria-hidden="true" [style.width.%]="porcentaje()"
          >★★★★★</span
        >
      </span>
      @if (mostrarNumero()) {
        <span class="estrellas__numero" aria-hidden="true">{{ texto() }}</span>
      }
    } @else {
      <span class="estrellas__vacio">Sin calificaciones</span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: var(--espacio-1);
    }
    .estrellas {
      position: relative;
      display: inline-block;
      font-size: 1.125rem;
      line-height: 1;
      letter-spacing: 1px;
    }
    .estrellas__fondo {
      color: #c4ccd4;
    }
    .estrellas__relleno {
      position: absolute;
      inset: 0 auto 0 0;
      overflow: hidden;
      white-space: nowrap;
      color: #a35f00; /* 5.0:1 sobre blanco */
    }
    .estrellas__numero {
      font-weight: 600;
    }
    .estrellas__vacio {
      color: var(--color-texto-suave);
      font-size: var(--texto-chico);
    }
  `,
})
export class Estrellas {
  readonly valor = input.required<number>();
  readonly mostrarNumero = input(true, { transform: booleanAttribute });

  protected readonly acotado = computed(() => Math.min(Math.max(this.valor(), 0), MAXIMO));
  protected readonly porcentaje = computed(() => (this.acotado() / MAXIMO) * 100);
  protected readonly texto = computed(() => formato.format(this.acotado()));
  protected readonly etiqueta = computed(() => `${this.texto()} de ${MAXIMO}`);
}
