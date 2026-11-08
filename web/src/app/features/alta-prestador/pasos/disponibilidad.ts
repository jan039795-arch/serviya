import { Component, computed, inject, signal } from '@angular/core';
import { applyEach, form, FormField, required, submit, validate } from '@angular/forms/signals';
import { FranjaDisponibilidad } from '../../../core/api/modelos';
import { mensajeDeError } from '../../../shared/formularios/errores';
import { AltaEstado } from '../alta-estado';
import { DIAS_SEMANA } from '../pasos';
import { PiePaso } from './pie-paso';

interface DiaEditable {
  dia: number;
  nombre: string;
  activo: boolean;
  desde: string;
  hasta: string;
}

/** Paso 5: días y horario en que suele trabajar. */
@Component({
  selector: 'app-paso-disponibilidad',
  imports: [FormField, PiePaso],
  template: `
    <h1>¿Cuándo trabajas?</h1>
    <p>Marca los días y el horario en que sueles atender. Puedes cambiarlo después.</p>

    <form novalidate (submit)="continuar($event)">
      @for (item of modelo().dias; track item.dia; let i = $index) {
        <div class="dia">
          <label class="opcion">
            <input
              type="checkbox"
              [formField]="f.dias[i].activo"
              [attr.data-testid]="'dia-' + item.dia"
            />
            {{ item.nombre }}
          </label>
          @if (item.activo) {
            <div class="horario">
              <label>
                <span class="solo-lectores">{{ item.nombre }}, desde</span>
                <input type="time" [formField]="f.dias[i].desde" />
              </label>
              <span aria-hidden="true">a</span>
              <label>
                <span class="solo-lectores">{{ item.nombre }}, hasta</span>
                <input type="time" [formField]="f.dias[i].hasta" />
              </label>
            </div>
            @if (error(f.dias[i].hasta); as mensaje) {
              <p class="texto-error">{{ mensaje }}</p>
            }
          }
        </div>
      }
      @if (enviado() && sinDias()) {
        <p class="alerta alerta--error" role="alert">Marca al menos un día.</p>
      }
      <app-pie-paso [paso]="5" />
    </form>
  `,
  styles: `
    .dia {
      border-bottom: 1px solid #e3e7eb;
      padding: var(--espacio-1) 0;
    }
    .horario {
      display: flex;
      align-items: center;
      gap: var(--espacio-2);
      margin: 0 0 var(--espacio-2) 2.125rem;
    }
    .horario input {
      width: 8rem;
    }
  `,
})
export class PasoDisponibilidad {
  private readonly estado = inject(AltaEstado);
  private readonly guardadas = this.estado.perfil()?.disponibilidad ?? [];

  protected readonly modelo = signal<{ dias: DiaEditable[] }>({
    dias: DIAS_SEMANA.map(({ dia, nombre }) => {
      const franja = this.guardadas.find((f) => f.dia === dia);
      return {
        dia,
        nombre,
        activo: !!franja,
        desde: franja?.desde ?? '09:00',
        hasta: franja?.hasta ?? '18:00',
      };
    }),
  });

  protected readonly f = form(this.modelo, (p) => {
    applyEach(p.dias, (dia) => {
      required(dia.desde, {
        message: 'Escribe la hora de inicio.',
        when: ({ valueOf }) => valueOf(dia.activo),
      });
      required(dia.hasta, {
        message: 'Escribe la hora de fin.',
        when: ({ valueOf }) => valueOf(dia.activo),
      });
      validate(dia.hasta, ({ value, valueOf }) =>
        valueOf(dia.activo) && value() <= valueOf(dia.desde)
          ? { kind: 'horario', message: 'La hora de fin debe ser después de la de inicio.' }
          : undefined,
      );
    });
    validate(p, ({ value }) =>
      value().dias.some((d) => d.activo)
        ? undefined
        : { kind: 'dias', message: 'Marca al menos un día.' },
    );
  });

  protected readonly error = mensajeDeError;
  protected readonly enviado = signal(false);
  protected readonly sinDias = computed(() => !this.modelo().dias.some((d) => d.activo));

  protected async continuar(evento: Event): Promise<void> {
    evento.preventDefault();
    this.enviado.set(true);
    await submit(this.f, async () => {
      const disponibilidad: FranjaDisponibilidad[] = this.modelo()
        .dias.filter((d) => d.activo)
        .map(({ dia, desde, hasta }) => ({ dia, desde, hasta }));
      await this.estado.guardarYAvanzar({ paso: 5, disponibilidad });
      return undefined;
    });
  }
}
