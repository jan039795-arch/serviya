import { booleanAttribute, Component, computed, input, output, signal } from '@angular/core';
import { marcadoDe } from '../../shared/formularios/eventos';
import { MxnPipe } from '../../shared/pipes/mxn';
import { Boton } from '../../shared/ui/boton';

export interface OfertaPase {
  readonly montoCentavos: number;
  readonly descuentoCentavos: number;
}

/**
 * Tarjeta del pase mensual (FR-021, FR-027, FR-028): precio, oferta con descuento y renovación
 * opcional solo con tarjeta. Se usa al contactar y en la pantalla del pase.
 */
@Component({
  selector: 'app-compra-pase',
  imports: [MxnPipe, Boton],
  template: `
    <article class="opcion-pago" data-testid="opcion-pase">
      <h2>Pase mensual</h2>
      @if (oferta(); as descuento) {
        <p class="precio">
          <span class="tachado">{{ precioCentavos() | mxn }}</span>
          <strong>{{ descuento.montoCentavos | mxn }}</strong>
        </p>
        <p class="alerta alerta--exito" data-testid="oferta-pase">
          Te descontamos {{ descuento.descuentoCentavos | mxn }} por los contactos que pagaste en
          los últimos 30 días.
        </p>
      } @else {
        <p class="precio">
          <strong>{{ precioCentavos() | mxn }}</strong>
        </p>
      }
      <ul>
        <li>Hasta 30 contactos nuevos durante 30 días.</li>
        <li>Chat y teléfono de cada prestador que contactes, mientras dure el pase.</li>
        <li>Conviene si vas a contactar a 4 o más prestadores.</li>
      </ul>
      <label class="opcion">
        <input
          type="checkbox"
          [checked]="renovar()"
          (change)="renovar.set(marcadoDe($event))"
          data-testid="renovacion"
        />
        <span>
          Renovar cada mes con tarjeta. Te avisamos 3 días antes del cobro y puedes cancelar en un
          clic.
        </span>
      </label>
      <button
        appBoton
        anchoCompleto
        [variante]="destacado() ? 'primario' : 'secundario'"
        type="button"
        [disabled]="ocupado()"
        (click)="comprar.emit(renovar())"
        data-testid="comprar-pase"
      >
        Comprar pase por {{ montoFinal() | mxn }}
      </button>
    </article>
  `,
  styles: `
    .precio {
      display: flex;
      align-items: baseline;
      gap: var(--espacio-2);
      font-size: var(--texto-grande);
    }
    .tachado {
      color: var(--color-texto-suave);
      font-size: var(--texto-base);
      text-decoration: line-through;
    }
    ul {
      padding-left: var(--espacio-6);
    }
  `,
})
export class CompraPase {
  readonly precioCentavos = input.required<number>();
  readonly oferta = input<OfertaPase>();
  readonly ocupado = input(false, { transform: booleanAttribute });
  readonly destacado = input(false, { transform: booleanAttribute });
  readonly comprar = output<boolean>();

  protected readonly renovar = signal(false);
  protected readonly marcadoDe = marcadoDe;
  protected readonly montoFinal = computed(
    () => this.oferta()?.montoCentavos ?? this.precioCentavos(),
  );
}
