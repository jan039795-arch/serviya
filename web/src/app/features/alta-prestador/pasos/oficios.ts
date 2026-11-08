import { Component, computed, inject, signal } from '@angular/core';
import {
  applyEach,
  form,
  FormField,
  max,
  min,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { OficioDelPrestador } from '../../../core/api/modelos';
import { mensajeDeError } from '../../../shared/formularios/errores';
import { AltaEstado } from '../alta-estado';
import { PiePaso } from './pie-paso';

interface OficioElegible {
  oficioId: string;
  nombre: string;
  activo: boolean;
  anios: number | null;
}

/** Paso 2: oficios con años de experiencia ("AniosExperiencia (0–60)"). */
@Component({
  selector: 'app-paso-oficios',
  imports: [FormField, PiePaso],
  template: `
    <h1>¿Qué oficios ofreces?</h1>
    <p>Elige uno o varios y escribe cuántos años de experiencia tienes en cada uno.</p>

    <form novalidate (submit)="continuar($event)">
      <fieldset class="opciones">
        <legend class="solo-lectores">Oficios</legend>
        @for (item of modelo().items; track item.oficioId; let i = $index) {
          <div class="oficio" [class.oficio--activo]="item.activo">
            <label class="opcion">
              <input
                type="checkbox"
                [formField]="f.items[i].activo"
                [attr.data-testid]="'oficio-' + i"
              />
              {{ item.nombre }}
            </label>
            @if (item.activo) {
              <label class="anios">
                <span>Años de experiencia</span>
                <input
                  type="number"
                  inputmode="numeric"
                  [formField]="f.items[i].anios"
                  [attr.data-testid]="'anios-' + i"
                />
              </label>
              @if (error(f.items[i].anios); as mensaje) {
                <p class="texto-error">{{ mensaje }}</p>
              }
            }
          </div>
        }
      </fieldset>
      @if (enviado() && sinSeleccion()) {
        <p class="alerta alerta--error" role="alert">Elige al menos un oficio.</p>
      }
      <app-pie-paso [paso]="2" />
    </form>
  `,
  styles: `
    .oficio {
      padding: var(--espacio-1) var(--espacio-3);
      margin-bottom: var(--espacio-2);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
    }
    .oficio--activo {
      border-color: var(--color-primario);
      background: #f3f7fb;
    }
    .anios {
      display: flex;
      align-items: center;
      gap: var(--espacio-3);
      margin: 0 0 var(--espacio-2) 2.125rem;
    }
    .anios input {
      width: 6rem;
    }
  `,
})
export class PasoOficios {
  private readonly estado = inject(AltaEstado);
  private readonly guardados = this.estado.perfil()?.oficios ?? [];

  protected readonly modelo = signal<{ items: OficioElegible[] }>({
    items: this.estado.oficios().map((oficio) => {
      const guardado = this.guardados.find((o) => o.oficioId === oficio.id);
      return {
        oficioId: oficio.id,
        nombre: oficio.nombre,
        activo: !!guardado,
        anios: guardado?.aniosExperiencia ?? null,
      };
    }),
  });

  protected readonly f = form(this.modelo, (p) => {
    applyEach(p.items, (item) => {
      required(item.anios, {
        message: 'Escribe tus años de experiencia.',
        when: ({ valueOf }) => valueOf(item.activo),
      });
      min(item.anios, 0, {
        message: 'Los años van de 0 a 60.',
        when: ({ valueOf }) => valueOf(item.activo),
      });
      max(item.anios, 60, {
        message: 'Los años van de 0 a 60.',
        when: ({ valueOf }) => valueOf(item.activo),
      });
    });
    validate(p, ({ value }) =>
      value().items.some((item) => item.activo)
        ? undefined
        : { kind: 'seleccion', message: 'Elige al menos un oficio.' },
    );
  });

  protected readonly error = mensajeDeError;
  protected readonly enviado = signal(false);
  protected readonly sinSeleccion = computed(() => !this.modelo().items.some((i) => i.activo));

  protected async continuar(evento: Event): Promise<void> {
    evento.preventDefault();
    this.enviado.set(true);
    await submit(this.f, async () => {
      const oficios: OficioDelPrestador[] = this.modelo()
        .items.filter((item) => item.activo)
        .map((item) => {
          const previo = this.guardados.find((o) => o.oficioId === item.oficioId);
          return {
            oficioId: item.oficioId,
            aniosExperiencia: item.anios ?? 0,
            tarifaMinCentavos: previo?.tarifaMinCentavos ?? 0,
            tarifaMaxCentavos: previo?.tarifaMaxCentavos ?? 0,
            unidadTarifa: previo?.unidadTarifa ?? 'Servicio',
          };
        });
      await this.estado.guardarYAvanzar({ paso: 2, oficios });
      return undefined;
    });
  }
}
