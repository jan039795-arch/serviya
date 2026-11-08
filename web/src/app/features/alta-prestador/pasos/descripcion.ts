import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, maxLength, submit } from '@angular/forms/signals';
import { mensajeDeError } from '../../../shared/formularios/errores';
import { Campo, CampoControl } from '../../../shared/ui/campo';
import { AltaEstado } from '../alta-estado';
import { PiePaso } from './pie-paso';

const LARGO_MAXIMO = 600;

/** Paso 6: descripción pública ("0–600 caracteres; pasa por `ContactMasker`"). */
@Component({
  selector: 'app-paso-descripcion',
  imports: [FormField, Campo, CampoControl, PiePaso],
  template: `
    <h1>Cuéntales sobre tu trabajo</h1>
    <p>
      Qué haces, cómo trabajas y qué te distingue. No escribas teléfonos, correos ni redes sociales:
      se ocultan automáticamente.
    </p>

    <form novalidate (submit)="continuar($event)">
      <app-campo etiqueta="Descripción" opcional [error]="error(f.descripcion)">
        <textarea
          appControl
          rows="7"
          [formField]="f.descripcion"
          data-testid="descripcion"
        ></textarea>
      </app-campo>
      <p class="texto-suave contador" aria-live="polite">
        {{ largo() }} de {{ maximo }} caracteres
      </p>
      <app-pie-paso [paso]="6" />
    </form>
  `,
  styles: `
    .contador {
      margin-top: calc(-1 * var(--espacio-3));
      text-align: right;
    }
  `,
})
export class PasoDescripcion {
  private readonly estado = inject(AltaEstado);

  protected readonly maximo = LARGO_MAXIMO;
  protected readonly modelo = signal({ descripcion: this.estado.perfil()?.descripcion ?? '' });
  protected readonly f = form(this.modelo, (p) => {
    maxLength(p.descripcion, LARGO_MAXIMO, { message: 'Máximo 600 caracteres.' });
  });
  protected readonly error = mensajeDeError;
  protected readonly largo = computed(() => this.modelo().descripcion.length);

  protected async continuar(evento: Event): Promise<void> {
    evento.preventDefault();
    await submit(this.f, async () => {
      await this.estado.guardarYAvanzar({ paso: 6, descripcion: this.modelo().descripcion.trim() });
      return undefined;
    });
  }
}
