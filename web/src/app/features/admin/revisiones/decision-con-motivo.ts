import { booleanAttribute, Component, computed, input, output, signal } from '@angular/core';
import { textoDe } from '../../../shared/formularios/eventos';
import { Boton, VarianteBoton } from '../../../shared/ui/boton';

export interface OpcionDecision<T extends string> {
  readonly decision: T;
  readonly texto: string;
  readonly variante: VarianteBoton;
  /** Si pide motivo, se abre un campo antes de confirmar (FR-010: motivo obligatorio). */
  readonly pideMotivo: boolean;
  readonly deshabilitada?: boolean;
}

export interface DecisionTomada<T extends string> {
  readonly decision: T;
  readonly motivo?: string;
}

let siguienteId = 0;

/** Botones de decisión; los que piden motivo muestran un campo obligatorio antes de confirmar. */
@Component({
  selector: 'app-decision-con-motivo',
  imports: [Boton],
  template: `
    @if (pendiente(); as opcion) {
      <div class="motivo">
        <label [for]="idMotivo">Motivo ({{ opcion.texto.toLowerCase() }})</label>
        <p class="texto-suave">El prestador verá este texto. Sé claro y amable.</p>
        <textarea
          [id]="idMotivo"
          rows="3"
          [value]="motivo()"
          (input)="motivo.set(textoDe($event))"
          [attr.aria-invalid]="faltaMotivo() ? 'true' : null"
          [attr.aria-describedby]="faltaMotivo() ? idMotivo + '-error' : null"
          data-testid="motivo"
        ></textarea>
        @if (faltaMotivo()) {
          <p class="texto-error" [id]="idMotivo + '-error'">Escribe el motivo.</p>
        }
        <div class="acciones">
          <button appBoton variante="secundario" type="button" (click)="cancelar()">
            Cancelar
          </button>
          <button
            appBoton
            [variante]="opcion.variante"
            type="button"
            [disabled]="ocupado()"
            (click)="confirmar(opcion)"
            data-testid="confirmar-decision"
          >
            Confirmar
          </button>
        </div>
      </div>
    } @else {
      <div class="botones">
        @for (opcion of opciones(); track opcion.decision) {
          <button
            appBoton
            [variante]="opcion.variante"
            type="button"
            [disabled]="ocupado() || opcion.deshabilitada"
            (click)="elegir(opcion)"
            [attr.data-testid]="'decision-' + opcion.decision"
          >
            {{ opcion.texto }}<span class="solo-lectores">: {{ contexto() }}</span>
          </button>
        }
      </div>
    }
  `,
  styles: `
    .botones {
      display: flex;
      flex-wrap: wrap;
      gap: var(--espacio-2);
    }
    .motivo {
      display: grid;
      gap: var(--espacio-1);
    }
    .motivo label {
      font-weight: 600;
    }
    .motivo p {
      margin: 0;
    }
    .acciones {
      margin-top: var(--espacio-2);
    }
  `,
})
export class DecisionConMotivo<T extends string> {
  readonly opciones = input.required<readonly OpcionDecision<T>[]>();
  /** Texto para lectores de pantalla, por ejemplo "INE (frente)". */
  readonly contexto = input('');
  readonly ocupado = input(false, { transform: booleanAttribute });
  readonly decidir = output<DecisionTomada<T>>();

  protected readonly textoDe = textoDe;
  protected readonly idMotivo = `motivo-${++siguienteId}`;
  protected readonly pendiente = signal<OpcionDecision<T> | null>(null);
  protected readonly motivo = signal('');
  private readonly intento = signal(false);
  protected readonly faltaMotivo = computed(() => this.intento() && !this.motivo().trim());

  protected elegir(opcion: OpcionDecision<T>): void {
    if (opcion.pideMotivo) {
      this.motivo.set('');
      this.intento.set(false);
      this.pendiente.set(opcion);
    } else {
      this.decidir.emit({ decision: opcion.decision });
    }
  }

  protected confirmar(opcion: OpcionDecision<T>): void {
    this.intento.set(true);
    if (!this.motivo().trim()) {
      return;
    }
    this.decidir.emit({ decision: opcion.decision, motivo: this.motivo().trim() });
    this.pendiente.set(null);
  }

  protected cancelar(): void {
    this.pendiente.set(null);
  }
}
