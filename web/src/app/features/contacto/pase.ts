import { Component, computed, inject, resource, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ErrorApi } from '../../core/http/errores.interceptor';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { Boton } from '../../shared/ui/boton';
import { Cargando } from '../../shared/ui/cargando';
import { Checkout } from './checkout';
import { CompraPase } from './compra-pase';
import { ContactoApi } from './contacto-api';

const formatoFecha = new Intl.DateTimeFormat('es-MX', { dateStyle: 'long' });

/**
 * Mi pase (US4; T033): contactos disponibles, vigencia y cancelación de la renovación en una sola
 * acción, conservando el acceso hasta el fin del periodo pagado (FR-028).
 */
@Component({
  selector: 'app-pase',
  imports: [RouterLink, Boton, Cargando, CompraPase],
  template: `
    <section class="contenedor">
      <h1>Mi pase mensual</h1>
      @if (error(); as mensaje) {
        <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
      }

      @if (pase.isLoading() && !pase.hasValue()) {
        <app-cargando texto="Cargando tu pase…" />
      } @else if (pase.hasValue()) {
        @let datos = pase.value();
        @if (datos.estado === 'Pendiente') {
          <p class="alerta alerta--aviso">Tu pase se activa en cuanto se acredite el pago.</p>
        } @else {
          <p class="disponibles" data-testid="contactos-disponibles">
            <strong>{{ datos.contactosIncluidos - datos.contactosUsados }}</strong> de
            {{ datos.contactosIncluidos }} contactos disponibles
          </p>
          <p>Vigente hasta el {{ fecha(datos.finEn) }}.</p>

          @if (datos.renovacionAutomatica) {
            <div class="renovacion">
              <p>Se renueva cada mes con tu tarjeta. Te avisaremos 3 días antes de cobrar.</p>
              <button
                appBoton
                variante="secundario"
                type="button"
                (click)="cancelar(datos.id)"
                [disabled]="ocupado()"
                data-testid="cancelar-renovacion"
              >
                Cancelar renovación
              </button>
            </div>
          } @else if (datos.renovacionCanceladaEn) {
            <p class="alerta alerta--exito" role="status" data-testid="renovacion-cancelada">
              Cancelaste la renovación. No te volveremos a cobrar y conservas tu pase hasta el
              {{ fecha(datos.finEn) }}.
            </p>
          } @else {
            <p class="texto-suave">Es de pago único: no se renueva solo.</p>
          }
        }
        <a appBoton variante="secundario" anchoCompleto routerLink="/">Buscar prestadores</a>
      } @else if (sinPase()) {
        <p>No tienes un pase activo.</p>
        @if (opciones.hasValue()) {
          @let general = opciones.value();
          <div class="aviso-pago" role="note" data-testid="aviso-pago">
            <strong>Importante:</strong> {{ general.aviso.texto }}
          </div>
          <app-compra-pase
            [precioCentavos]="general.precios.paseCentavos"
            [oferta]="general.ofertaPase"
            destacado
            [ocupado]="ocupado()"
            (comprar)="comprar($event, general.aviso.version)"
          />
        } @else {
          <app-cargando />
        }
      } @else {
        <p class="alerta alerta--error" role="alert">No pudimos cargar tu pase.</p>
        <button appBoton type="button" (click)="pase.reload()">Reintentar</button>
      }
    </section>
  `,
  styles: `
    .disponibles {
      font-size: var(--texto-grande);
    }
    .renovacion {
      margin-bottom: var(--espacio-4);
      padding: var(--espacio-4);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
    }
  `,
})
export class Pase {
  private readonly api = inject(ContactoApi);
  private readonly checkout = inject(Checkout);

  protected readonly pase = resource({ loader: () => this.api.miPase() });
  /** Aviso, precio y oferta vigentes vienen de la API, no de constantes del frontend. */
  protected readonly opciones = resource({ loader: () => this.api.opciones() });
  protected readonly ocupado = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly sinPase = computed(() => {
    const error = this.pase.error();
    return error instanceof ErrorApi && error.status === 404;
  });

  protected fecha(iso?: string): string {
    return iso ? formatoFecha.format(new Date(iso)) : '';
  }

  /** Una sola acción, sin pasos extra (FR-028; US4, escenario 5). */
  protected async cancelar(paseId: string): Promise<void> {
    await this.ejecutar(async () => {
      await this.api.cancelarRenovacion(paseId);
      this.pase.reload();
    });
  }

  protected async comprar(renovacionAutomatica: boolean, avisoVersion: string): Promise<void> {
    await this.ejecutar(async () => {
      const { checkoutUrl } = await this.api.comprarPase(renovacionAutomatica, avisoVersion);
      await this.checkout.ir(checkoutUrl);
    });
  }

  private async ejecutar(accion: () => Promise<void>): Promise<void> {
    this.error.set(null);
    this.ocupado.set(true);
    try {
      await accion();
    } catch (error: unknown) {
      this.error.set(mensajeGeneral(error));
    } finally {
      this.ocupado.set(false);
    }
  }
}
