import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, submit, validate } from '@angular/forms/signals';
import { AltaEstado } from '../alta-estado';
import { PiePaso } from './pie-paso';

interface MunicipioElegible {
  municipioId: string;
  nombre: string;
  activo: boolean;
}

/** Paso 4: municipios o alcaldías donde trabaja (research R5). Al menos uno. */
@Component({
  selector: 'app-paso-cobertura',
  imports: [FormField, PiePaso],
  template: `
    <h1>¿Dónde trabajas?</h1>
    <p>Elige los municipios o alcaldías a los que puedes ir. Te buscarán ahí.</p>

    <form novalidate (submit)="continuar($event)">
      <fieldset class="opciones">
        <legend class="solo-lectores">Municipios</legend>
        @for (item of modelo().items; track item.municipioId; let i = $index) {
          <label class="opcion">
            <input
              type="checkbox"
              [formField]="f.items[i].activo"
              [attr.data-testid]="'municipio-' + i"
            />
            {{ item.nombre }}
          </label>
        }
      </fieldset>
      @if (enviado() && sinSeleccion()) {
        <p class="alerta alerta--error" role="alert">Elige al menos un municipio.</p>
      }
      <app-pie-paso [paso]="4" />
    </form>
  `,
})
export class PasoCobertura {
  private readonly estado = inject(AltaEstado);
  private readonly guardados = new Set(this.estado.perfil()?.cobertura ?? []);

  protected readonly modelo = signal<{ items: MunicipioElegible[] }>({
    items: this.estado.municipios().map((municipio) => ({
      municipioId: municipio.id,
      nombre: municipio.nombre,
      activo: this.guardados.has(municipio.id),
    })),
  });
  protected readonly f = form(this.modelo, (p) => {
    validate(p, ({ value }) =>
      value().items.some((i) => i.activo)
        ? undefined
        : { kind: 'seleccion', message: 'Elige al menos un municipio.' },
    );
  });
  protected readonly enviado = signal(false);
  protected readonly sinSeleccion = computed(() => !this.modelo().items.some((i) => i.activo));

  protected async continuar(evento: Event): Promise<void> {
    evento.preventDefault();
    this.enviado.set(true);
    await submit(this.f, async () => {
      const cobertura = this.modelo()
        .items.filter((i) => i.activo)
        .map((i) => i.municipioId);
      await this.estado.guardarYAvanzar({ paso: 4, cobertura });
      return undefined;
    });
  }
}
