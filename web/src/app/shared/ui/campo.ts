import { booleanAttribute, Component, computed, Directive, inject, input } from '@angular/core';

let siguienteId = 0;

/**
 * Campo de formulario con etiqueta, ayuda y error ligados al control con `for`,
 * `aria-describedby` y `aria-invalid`. El control se marca con la directiva `appControl`:
 *
 * ```html
 * <app-campo etiqueta="Teléfono" ayuda="10 dígitos" [error]="errorTelefono()">
 *   <input appControl type="tel" />
 * </app-campo>
 * ```
 */
@Component({
  selector: 'app-campo',
  template: `
    <label class="campo__etiqueta" [for]="idControl">
      {{ etiqueta() }}
      @if (opcional()) {
        <span class="campo__opcional">(opcional)</span>
      }
    </label>
    @if (ayuda()) {
      <p class="campo__ayuda" [id]="idAyuda">{{ ayuda() }}</p>
    }
    <ng-content />
    @if (error()) {
      <p class="campo__error" [id]="idError">{{ error() }}</p>
    }
  `,
  host: { class: 'campo', '[class.campo--con-error]': '!!error()' },
  styles: `
    :host {
      display: grid;
      gap: var(--espacio-1);
      margin-bottom: var(--espacio-4);
    }
    .campo__etiqueta {
      font-weight: 600;
    }
    .campo__opcional,
    .campo__ayuda {
      color: var(--color-texto-suave);
      font-weight: 400;
      font-size: var(--texto-chico);
    }
    .campo__ayuda,
    .campo__error {
      margin: 0;
    }
    .campo__error {
      color: var(--color-error);
      font-size: var(--texto-chico);
      font-weight: 600;
    }
    :host(.campo--con-error) ::ng-deep [appControl] {
      border-color: var(--color-error);
      border-width: 2px;
    }
  `,
})
export class Campo {
  readonly etiqueta = input.required<string>();
  readonly ayuda = input<string>();
  readonly error = input<string | null>();
  readonly opcional = input(false, { transform: booleanAttribute });

  private readonly id = `campo-${++siguienteId}`;
  readonly idControl = `${this.id}-control`;
  readonly idAyuda = `${this.id}-ayuda`;
  readonly idError = `${this.id}-error`;

  readonly describedBy = computed(() => {
    const ids = [this.ayuda() ? this.idAyuda : null, this.error() ? this.idError : null];
    const texto = ids.filter(Boolean).join(' ');
    return texto || null;
  });
}

/** Marca el `<input>`, `<select>` o `<textarea>` de un `app-campo`. */
@Directive({
  selector: 'input[appControl], select[appControl], textarea[appControl]',
  host: {
    '[id]': 'campo.idControl',
    '[attr.aria-describedby]': 'campo.describedBy()',
    '[attr.aria-invalid]': 'campo.error() ? "true" : null',
  },
})
export class CampoControl {
  readonly campo = inject(Campo);
}
