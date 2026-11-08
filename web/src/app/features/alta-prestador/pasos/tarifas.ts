import { Component, inject, signal } from '@angular/core';
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
import { RouterLink } from '@angular/router';
import { UnidadTarifa } from '../../../core/api/modelos';
import { mensajeDeError } from '../../../shared/formularios/errores';
import { AltaEstado } from '../alta-estado';
import { PiePaso } from './pie-paso';

const TARIFA_MAXIMA_PESOS = 100_000;

interface TarifaEditable {
  oficioId: string;
  nombre: string;
  aniosExperiencia: number;
  minimo: number | null;
  maximo: number | null;
  unidad: UnidadTarifa;
}

/** Paso 3: tarifas de referencia por oficio. Solo orientan; el precio lo acuerdan las partes. */
@Component({
  selector: 'app-paso-tarifas',
  imports: [FormField, RouterLink, PiePaso],
  template: `
    <h1>Tarifas de referencia</h1>
    <p>
      Ayudan al cliente a darse una idea. El precio final lo acuerdan ustedes; ServiYa no cobra
      comisión por tu trabajo.
    </p>

    @if (modelo().items.length === 0) {
      <p class="alerta alerta--aviso">
        Primero elige tus oficios. <a routerLink="/alta/2">Ir al paso 2</a>
      </p>
    } @else {
      <form novalidate (submit)="continuar($event)">
        @for (item of modelo().items; track item.oficioId; let i = $index) {
          <fieldset class="tarifa">
            <legend>{{ item.nombre }}</legend>
            <div class="rango">
              <label>
                <span>Desde ($)</span>
                <input
                  type="number"
                  inputmode="numeric"
                  [formField]="f.items[i].minimo"
                  [attr.data-testid]="'minimo-' + i"
                />
              </label>
              <label>
                <span>Hasta ($)</span>
                <input
                  type="number"
                  inputmode="numeric"
                  [formField]="f.items[i].maximo"
                  [attr.data-testid]="'maximo-' + i"
                />
              </label>
              <label>
                <span>Por</span>
                <select [formField]="f.items[i].unidad">
                  <option value="Hora">hora</option>
                  <option value="Dia">día</option>
                  <option value="Servicio">servicio</option>
                </select>
              </label>
            </div>
            @if (error(f.items[i].minimo) ?? error(f.items[i].maximo); as mensaje) {
              <p class="texto-error">{{ mensaje }}</p>
            }
          </fieldset>
        }
        <app-pie-paso [paso]="3" />
      </form>
    }
  `,
  styles: `
    .tarifa {
      margin: 0 0 var(--espacio-4);
      padding: var(--espacio-3);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
    }
    .tarifa legend {
      padding: 0 var(--espacio-1);
      font-weight: 600;
    }
    .rango {
      display: grid;
      grid-template-columns: 1fr 1fr 1fr;
      gap: var(--espacio-2);
    }
    .rango label {
      display: grid;
      gap: var(--espacio-1);
      font-size: var(--texto-chico);
    }
  `,
})
export class PasoTarifas {
  private readonly estado = inject(AltaEstado);

  protected readonly modelo = signal<{ items: TarifaEditable[] }>({
    items: (this.estado.perfil()?.oficios ?? []).map((oficio) => ({
      oficioId: oficio.oficioId,
      nombre: this.estado.oficios().find((o) => o.id === oficio.oficioId)?.nombre ?? 'Oficio',
      aniosExperiencia: oficio.aniosExperiencia,
      minimo: oficio.tarifaMinCentavos > 0 ? oficio.tarifaMinCentavos / 100 : null,
      maximo: oficio.tarifaMaxCentavos > 0 ? oficio.tarifaMaxCentavos / 100 : null,
      unidad: oficio.unidadTarifa,
    })),
  });

  protected readonly f = form(this.modelo, (p) => {
    applyEach(p.items, (item) => {
      required(item.minimo, { message: 'Escribe desde cuánto cobras.' });
      min(item.minimo, 1, { message: 'La tarifa debe ser mayor a $0.' });
      required(item.maximo, { message: 'Escribe hasta cuánto cobras.' });
      max(item.maximo, TARIFA_MAXIMA_PESOS, { message: 'Revisa la tarifa máxima.' });
      validate(item.maximo, ({ value, valueOf }) => {
        const minimo = valueOf(item.minimo);
        return value() !== null && minimo !== null && (value() ?? 0) < minimo
          ? { kind: 'rango', message: '"Hasta" no puede ser menor que "Desde".' }
          : undefined;
      });
    });
  });

  protected readonly error = mensajeDeError;

  protected async continuar(evento: Event): Promise<void> {
    evento.preventDefault();
    await submit(this.f, async () => {
      const oficios = this.modelo().items.map((item) => ({
        oficioId: item.oficioId,
        aniosExperiencia: item.aniosExperiencia,
        tarifaMinCentavos: Math.round((item.minimo ?? 0) * 100),
        tarifaMaxCentavos: Math.round((item.maximo ?? 0) * 100),
        unidadTarifa: item.unidad,
      }));
      await this.estado.guardarYAvanzar({ paso: 3, oficios });
      return undefined;
    });
  }
}
