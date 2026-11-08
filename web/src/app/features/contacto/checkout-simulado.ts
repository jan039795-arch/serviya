import { Component, inject, input, resource, signal } from '@angular/core';
import { Router } from '@angular/router';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { MxnPipe } from '../../shared/pipes/mxn';
import { Boton } from '../../shared/ui/boton';
import { Cargando } from '../../shared/ui/cargando';
import { ACCIONES_PROTOTIPO, ResultadoSimulado } from '../../core/prototipo/acciones';
import { ContactoApi } from './contacto-api';

const CONCEPTOS: Readonly<Record<string, string>> = {
  ContactoIndividual: 'Contacto individual',
  PaseMensual: 'Pase mensual',
  RenovacionPase: 'Renovación del pase',
  CuotaAdministracion: 'Cuota de administración',
};

/**
 * Solo prototipo: reemplaza a Checkout Pro de Mercado Pago (T031). Cada botón simula lo que
 * después llegaría por el webhook firmado.
 */
@Component({
  selector: 'app-checkout-simulado',
  imports: [MxnPipe, Boton, Cargando],
  template: `
    <section class="contenedor">
      <p class="alerta alerta--info">
        Prototipo: esta pantalla simula la pasarela de pago. No se cobra nada.
      </p>
      @if (pago.hasValue()) {
        @let datos = pago.value();
        <h1>Pagar {{ datos.montoCentavos | mxn }}</h1>
        <p>{{ conceptos[datos.concepto] }}</p>
        @if (error(); as mensaje) {
          <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
        }
        <div class="metodos">
          <button
            appBoton
            anchoCompleto
            type="button"
            (click)="simular('aprobado')"
            [disabled]="ocupado()"
            data-testid="pagar-tarjeta"
          >
            Pagar con tarjeta (se aprueba)
          </button>
          <button
            appBoton
            variante="secundario"
            anchoCompleto
            type="button"
            (click)="simular('pendiente')"
            [disabled]="ocupado()"
            data-testid="pagar-oxxo"
          >
            Generar referencia para OXXO
          </button>
          <button
            appBoton
            variante="peligro"
            anchoCompleto
            type="button"
            (click)="simular('rechazado')"
            [disabled]="ocupado()"
            data-testid="pagar-rechazado"
          >
            Tarjeta rechazada
          </button>
        </div>
      } @else if (pago.error()) {
        <p class="alerta alerta--error" role="alert">No encontramos este pago.</p>
      } @else {
        <app-cargando />
      }
    </section>
  `,
  styles: `
    .metodos {
      display: grid;
      gap: var(--espacio-3);
    }
  `,
})
export class CheckoutSimulado {
  private readonly api = inject(ContactoApi);
  private readonly acciones = inject(ACCIONES_PROTOTIPO);
  private readonly router = inject(Router);

  /** `?pagoId=` (el mismo dato que Mercado Pago recibe como `external_reference`). */
  readonly pagoId = input.required<string>();

  protected readonly conceptos = CONCEPTOS;
  protected readonly pago = resource({
    params: () => ({ id: this.pagoId() }),
    loader: ({ params }) => this.api.pago(params.id),
  });
  protected readonly ocupado = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async simular(resultado: ResultadoSimulado): Promise<void> {
    this.ocupado.set(true);
    this.error.set(null);
    try {
      await this.acciones.simularPago(this.pagoId(), resultado);
      await this.router.navigate(['/pago/resultado'], { queryParams: { pagoId: this.pagoId() } });
    } catch (error: unknown) {
      this.error.set(mensajeGeneral(error));
    } finally {
      this.ocupado.set(false);
    }
  }
}
