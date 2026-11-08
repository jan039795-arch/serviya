import { Component, inject, resource, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AccesoContacto, ContactoDesbloqueado, EstadoAcceso } from '../../core/api/modelos';
import { ENTORNO, hayAreasSimuladas } from '../../core/entorno';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { Boton } from '../../shared/ui/boton';
import { Cargando } from '../../shared/ui/cargando';
import { EstadoVacio } from '../../shared/ui/estado-vacio';
import { ContactoApi } from './contacto-api';

const TEXTO_ESTADO: Readonly<Record<EstadoAcceso, string>> = {
  Pendiente: 'Pago pendiente',
  Activo: 'Activo',
  Vencido: 'Vencido',
  Expirado: 'Pago no realizado',
  Reembolsado: 'Reembolsado',
};

const formatoFecha = new Intl.DateTimeFormat('es-MX', { dateStyle: 'medium' });

/**
 * Mis contactos (T031): estado y vencimiento de cada acceso. El teléfono se pide a la API solo al
 * pulsar "Ver teléfono" y solo existe mientras el acceso está activo (FR-020, FR-024).
 */
@Component({
  selector: 'app-mis-accesos',
  imports: [RouterLink, Boton, Cargando, EstadoVacio],
  template: `
    <section class="contenedor">
      <h1>Mis contactos</h1>
      @if (accesos.error()) {
        <p class="alerta alerta--error" role="alert">No pudimos cargar tus contactos.</p>
        <button appBoton type="button" (click)="accesos.reload()">Reintentar</button>
      } @else if (accesos.hasValue()) {
        @let lista = accesos.value();
        @if (lista.length === 0) {
          <app-estado-vacio
            titulo="Todavía no contactas a nadie"
            mensaje="Busca un prestador y contáctalo desde su perfil."
          >
            <a appBoton routerLink="/">Buscar</a>
          </app-estado-vacio>
        } @else {
          <ul class="lista" data-testid="mis-accesos">
            @for (acceso of lista; track acceso.id) {
              <li [attr.data-testid]="'acceso-' + acceso.prestadorId">
                <p>
                  <a [routerLink]="['/prestador', acceso.prestadorId]"
                    ><strong>{{ acceso.nombrePrestador }}</strong></a
                  >
                  <span [class]="'estado estado--' + acceso.estado">{{
                    textoEstado[acceso.estado]
                  }}</span>
                </p>
                @switch (acceso.estado) {
                  @case ('Activo') {
                    <p class="texto-suave">
                      {{ acceso.origen === 'Pase' ? 'Con tu pase' : 'Acceso' }} hasta el
                      {{ fecha(acceso.venceEn) }}
                    </p>
                    @if (contactos()[acceso.id]; as contacto) {
                      <p class="telefono" data-testid="telefono">{{ contacto.telefono }}</p>
                      <div class="acciones">
                        <a appBoton [href]="'tel:' + soloDigitos(contacto.telefono)">Llamar</a>
                        @if (contacto.whatsapp) {
                          <a
                            appBoton
                            variante="secundario"
                            [href]="'https://wa.me/' + soloDigitos(contacto.whatsapp)"
                            target="_blank"
                            rel="noopener noreferrer"
                          >
                            WhatsApp
                          </a>
                        }
                      </div>
                    } @else {
                      <button
                        appBoton
                        variante="secundario"
                        type="button"
                        (click)="verTelefono(acceso)"
                        data-testid="ver-telefono"
                      >
                        Ver teléfono
                      </button>
                    }
                    @if (prototipo) {
                      <button
                        appBoton
                        variante="texto"
                        type="button"
                        (click)="simularVencimiento(acceso)"
                        data-testid="simular-vencimiento"
                      >
                        Prototipo: simular que pasaron 30 días
                      </button>
                    }
                  }
                  @case ('Pendiente') {
                    <p>Se activa en cuanto se acredite tu pago en OXXO o SPEI.</p>
                    @if (prototipo) {
                      <button
                        appBoton
                        variante="texto"
                        type="button"
                        (click)="simularAcreditacion(acceso)"
                        data-testid="simular-acreditacion"
                      >
                        Prototipo: simular que se acreditó el pago
                      </button>
                    }
                  }
                  @case ('Vencido') {
                    <p>El acceso terminó: el chat queda en solo lectura y el teléfono se oculta.</p>
                    <a
                      appBoton
                      variante="secundario"
                      [routerLink]="['/contactar', acceso.prestadorId]"
                      >Contactar de nuevo</a
                    >
                  }
                  @case ('Expirado') {
                    <p>La referencia venció sin pagarse. No se te cobró nada.</p>
                    <a
                      appBoton
                      variante="secundario"
                      [routerLink]="['/contactar', acceso.prestadorId]"
                      >Intentar de nuevo</a
                    >
                  }
                }
              </li>
            }
          </ul>
        }
      } @else {
        <app-cargando texto="Cargando tus contactos…" />
      }
      @if (error(); as mensaje) {
        <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
      }
    </section>
  `,
  styleUrl: './contacto.css',
})
export class MisAccesos {
  private readonly api = inject(ContactoApi);
  protected readonly prototipo = hayAreasSimuladas(inject(ENTORNO));

  protected readonly textoEstado = TEXTO_ESTADO;
  protected readonly accesos = resource({ loader: () => this.api.misAccesos() });
  protected readonly contactos = signal<Readonly<Record<string, ContactoDesbloqueado>>>({});
  protected readonly error = signal<string | null>(null);

  protected fecha(iso?: string): string {
    return iso ? formatoFecha.format(new Date(iso)) : '';
  }

  protected soloDigitos(telefono: string): string {
    return telefono.replace(/\D/g, '');
  }

  protected async verTelefono(acceso: AccesoContacto): Promise<void> {
    this.error.set(null);
    try {
      const contacto = await this.api.contacto(acceso.id);
      this.contactos.update((actuales) => ({ ...actuales, [acceso.id]: contacto }));
    } catch (error: unknown) {
      this.error.set(mensajeGeneral(error));
    }
  }

  protected async simularAcreditacion(acceso: AccesoContacto): Promise<void> {
    await this.api.acreditarAcceso(acceso.id);
    this.accesos.reload();
  }

  protected async simularVencimiento(acceso: AccesoContacto): Promise<void> {
    await this.api.vencerAcceso(acceso.id);
    this.contactos.update((actuales) =>
      Object.fromEntries(Object.entries(actuales).filter(([id]) => id !== acceso.id)),
    );
    this.accesos.reload();
  }
}
