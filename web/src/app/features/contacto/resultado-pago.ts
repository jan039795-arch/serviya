import { Component, computed, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Pago } from '../../core/api/modelos';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { MxnPipe } from '../../shared/pipes/mxn';
import { Boton } from '../../shared/ui/boton';
import { Cargando } from '../../shared/ui/cargando';
import { ContactoApi } from './contacto-api';

const INTERVALO_MS = 3000;
const INTENTOS_MAXIMOS = 20;

/**
 * Regreso desde la pasarela (`/pago/resultado?pagoId=`). El acceso solo se activa por webhook, así
 * que mientras el pago siga "Creado" se consulta cada 3 s, hasta 1 minuto (SC-005).
 */
@Component({
  selector: 'app-resultado-pago',
  imports: [RouterLink, MxnPipe, Boton, Cargando],
  template: `
    <section class="contenedor" aria-live="polite">
      @if (error(); as mensaje) {
        <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
        <button appBoton type="button" (click)="consultar()">Reintentar</button>
      } @else if (pago(); as datos) {
        @switch (datos.estado) {
          @case ('Aprobado') {
            <h1 data-testid="resultado-titulo">¡Pago aprobado!</h1>
            <p>Pagaste {{ datos.montoCentavos | mxn }}. Te enviamos el comprobante por correo.</p>
            @if (esPase()) {
              <p>Tu pase ya está activo.</p>
              <a appBoton anchoCompleto routerLink="/pase">Ver mi pase</a>
            } @else {
              <p>Ya puedes ver el teléfono y escribirle al prestador.</p>
              <a appBoton anchoCompleto routerLink="/mis-contactos" data-testid="ver-contactos"
                >Ver mis contactos</a
              >
            }
          }
          @case ('Pendiente') {
            <h1 data-testid="resultado-titulo">Pago pendiente</h1>
            <p>
              Genera tu pago en OXXO o con transferencia SPEI usando la referencia que te dio la
              pasarela. En cuanto se acredite, tu acceso se activa solo; no tienes que hacer nada
              más.
            </p>
            <p class="alerta alerta--aviso">
              Los pagos en efectivo pueden tardar de 2 a 3 días hábiles en acreditarse. Si la
              referencia vence sin pagarse, no se te cobra nada.
            </p>
            <a appBoton anchoCompleto routerLink="/mis-contactos">Ver mis contactos</a>
          }
          @case ('Creado') {
            @if (seAgotoLaEspera()) {
              <h1 data-testid="resultado-titulo">Seguimos esperando la confirmación</h1>
              <p>
                Si ya pagaste, tu acceso se activará en unos minutos. Puedes revisarlo en tus
                contactos.
              </p>
              <a appBoton anchoCompleto routerLink="/mis-contactos">Ver mis contactos</a>
            } @else {
              <app-cargando texto="Confirmando tu pago…" />
            }
          }
          @default {
            <h1 data-testid="resultado-titulo">No se completó el pago</h1>
            <p>No se te cobró nada. Puedes intentarlo de nuevo con otro método.</p>
            <a appBoton anchoCompleto routerLink="/">Volver a buscar</a>
          }
        }
      } @else {
        <app-cargando texto="Revisando tu pago…" />
      }
    </section>
  `,
})
export class ResultadoPago implements OnInit {
  private readonly api = inject(ContactoApi);
  private readonly destroyRef = inject(DestroyRef);

  /** `?pagoId=` que agrega la pasarela al regresar; puede faltar si alguien abre la URL a mano. */
  readonly pagoId = input<string>();

  protected readonly pago = signal<Pago | null>(null);
  protected readonly error = signal<string | null>(null);
  private readonly intentos = signal(0);
  protected readonly seAgotoLaEspera = computed(() => this.intentos() >= INTENTOS_MAXIMOS);
  protected readonly esPase = computed(() => this.pago()?.concepto === 'PaseMensual');
  private temporizador: ReturnType<typeof setTimeout> | null = null;
  /** Evita programar otra consulta si la respuesta llega después de salir de la pantalla. */
  private destruido = false;

  ngOnInit(): void {
    this.destroyRef.onDestroy(() => {
      this.destruido = true;
      if (this.temporizador) {
        clearTimeout(this.temporizador);
      }
    });
    void this.consultar();
  }

  protected async consultar(): Promise<void> {
    const pagoId = this.pagoId();
    if (!pagoId) {
      this.error.set('No encontramos el pago. Revisa tus contactos o tu historial.');
      return;
    }
    this.error.set(null);
    try {
      const pago = await this.api.pago(pagoId);
      if (this.destruido) {
        return;
      }
      this.pago.set(pago);
      this.intentos.update((n) => n + 1);
      if (pago.estado === 'Creado' && !this.seAgotoLaEspera()) {
        this.temporizador = setTimeout(() => void this.consultar(), INTERVALO_MS);
      }
    } catch (error: unknown) {
      if (!this.destruido) {
        this.error.set(mensajeGeneral(error));
      }
    }
  }
}
