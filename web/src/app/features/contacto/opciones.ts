import { Component, computed, inject, input, resource, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { OrigenAcceso } from '../../core/api/modelos';
import { ErrorApi } from '../../core/http/errores.interceptor';
import { SesionService } from '../../core/sesion/sesion';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { MxnPipe } from '../../shared/pipes/mxn';
import { valorDe } from '../../shared/recursos';
import { Avisos } from '../../shared/ui/avisos';
import { Boton } from '../../shared/ui/boton';
import { Cargando } from '../../shared/ui/cargando';
import { BusquedaApi } from '../busqueda/busqueda-api';
import { Checkout } from './checkout';
import { CompraPase } from './compra-pase';
import { ContactoApi } from './contacto-api';

const formatoFecha = new Intl.DateTimeFormat('es-MX', { dateStyle: 'long' });

/**
 * Opciones para contactar a un prestador (US3 y US4; T030). Antes de pagar se muestra qué incluye,
 * la vigencia, la regla de crédito y el aviso de no garantía (FR-022).
 */
@Component({
  selector: 'app-opciones-contacto',
  imports: [RouterLink, MxnPipe, Boton, Cargando, CompraPase],
  templateUrl: './opciones.html',
  styleUrl: './contacto.css',
})
export class OpcionesContacto {
  private readonly api = inject(ContactoApi);
  private readonly busqueda = inject(BusquedaApi);
  private readonly checkout = inject(Checkout);
  private readonly router = inject(Router);
  private readonly avisos = inject(Avisos);
  protected readonly sesion = inject(SesionService);

  /** Parámetro de la ruta `contactar/:prestadorId`. */
  readonly prestadorId = input.required<string>();

  protected readonly prestador = resource({
    params: () => ({ id: this.prestadorId() }),
    loader: ({ params }) => this.busqueda.perfil(params.id),
  });
  protected readonly opciones = resource({
    params: () => ({ id: this.prestadorId() }),
    loader: ({ params }) => this.api.opciones(params.id),
  });

  protected readonly ocupado = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly nombre = computed(
    () => valorDe(this.prestador)?.nombrePublico ?? 'este prestador',
  );
  protected readonly noDisponible = computed(() => {
    const error = this.prestador.error();
    return error instanceof ErrorApi && error.status === 404;
  });
  protected readonly menorDeEdad = computed(() => this.sesion.usuario()?.esMayorDeEdad === false);
  protected readonly vigenteHasta = computed(() => {
    const vence = valorDe(this.opciones)?.accesoVigente?.venceEn;
    return vence ? formatoFecha.format(new Date(vence)) : null;
  });
  protected readonly paseHasta = computed(() => {
    const fin = valorDe(this.opciones)?.pase?.finEn;
    return fin ? formatoFecha.format(new Date(fin)) : null;
  });

  protected async pagarContacto(): Promise<void> {
    await this.ejecutar(async (version) => {
      const { checkoutUrl } = await this.api.solicitarAcceso(
        this.prestadorId(),
        'Individual',
        version,
      );
      if (checkoutUrl) {
        await this.checkout.ir(checkoutUrl);
      }
    });
  }

  protected async usar(via: Extract<OrigenAcceso, 'Pase' | 'Credito'>): Promise<void> {
    await this.ejecutar(async (version) => {
      await this.api.solicitarAcceso(this.prestadorId(), via, version);
      this.avisos.mostrar('exito', `¡Listo! Ya puedes contactar a ${this.nombre()}.`);
      await this.router.navigate(['/mis-contactos']);
    });
  }

  protected async comprarPase(renovacionAutomatica: boolean): Promise<void> {
    await this.ejecutar(async (version) => {
      const { checkoutUrl } = await this.api.comprarPase(renovacionAutomatica, version);
      await this.checkout.ir(checkoutUrl);
    });
  }

  private async ejecutar(accion: (avisoVersion: string) => Promise<void>): Promise<void> {
    const opciones = valorDe(this.opciones);
    if (!opciones) {
      return;
    }
    this.error.set(null);
    this.ocupado.set(true);
    try {
      await accion(opciones.aviso.version);
    } catch (error: unknown) {
      this.error.set(mensajeGeneral(error));
      this.opciones.reload();
    } finally {
      this.ocupado.set(false);
    }
  }
}
