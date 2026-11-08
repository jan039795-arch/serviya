import {
  booleanAttribute,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import {
  DecisionDocumento,
  DocumentoRevision as TipoDocumentoRevision,
  EstadoDocumento,
  UrlDocumento,
} from '../../../core/api/modelos';
import { mensajeGeneral } from '../../../shared/formularios/errores';
import { Boton } from '../../../shared/ui/boton';
import { tituloDocumento } from '../../alta-prestador/pasos';
import { DecisionConMotivo, DecisionTomada, OpcionDecision } from './decision-con-motivo';
import { RevisionesApi } from './revisiones-api';

const TEXTO_ESTADO: Readonly<Record<EstadoDocumento, string>> = {
  Cargado: 'Por revisar',
  Aprobado: 'Aprobado',
  RequiereCorreccion: 'Requiere corrección',
  Rechazado: 'Rechazado',
  Vencido: 'Vencido',
  Reemplazado: 'Reemplazado',
};

const OPCIONES: readonly OpcionDecision<DecisionDocumento>[] = [
  { decision: 'Aprobar', texto: 'Aprobar', variante: 'primario', pideMotivo: false },
  {
    decision: 'RequiereCorreccion',
    texto: 'Pedir corrección',
    variante: 'secundario',
    pideMotivo: true,
  },
  { decision: 'Rechazar', texto: 'Rechazar', variante: 'peligro', pideMotivo: true },
];

/**
 * Un documento en revisión. La imagen solo se pide al pulsar "Ver documento": cada consulta queda
 * en la bitácora y la URL firmada vence a los 5 minutos (FR-016, research R3).
 */
@Component({
  selector: 'app-documento-revision',
  imports: [Boton, DecisionConMotivo],
  template: `
    <article class="documento" [attr.data-testid]="'revision-' + documento().tipo">
      <header>
        <h3>{{ titulo() }}</h3>
        <span class="estado" [class]="'estado estado--' + documento().estado">
          {{ textoEstado[documento().estado] }}
        </span>
      </header>
      @if (documento().fechaExpedicion) {
        <p class="texto-suave">Expedido el {{ documento().fechaExpedicion }}</p>
      }
      @if (documento().motivo) {
        <p class="texto-suave">Motivo: {{ documento().motivo }}</p>
      }

      @if (vista(); as archivo) {
        <figure class="visor">
          @if (documento().tipoContenido === 'application/pdf') {
            <a [href]="archivo.url" target="_blank" rel="noopener noreferrer">Abrir el PDF</a>
          } @else {
            <img
              [src]="archivo.url"
              [alt]="titulo()"
              referrerpolicy="no-referrer"
              (error)="cerrarVista('El enlace del documento venció. Vuelve a abrirlo.')"
              data-testid="visor-documento"
            />
          }
          <figcaption class="texto-suave">
            Esta consulta quedó registrada. El enlace vence en 5 minutos.
          </figcaption>
        </figure>
      } @else {
        <button
          appBoton
          variante="secundario"
          type="button"
          (click)="ver()"
          [disabled]="cargando()"
          data-testid="ver-documento"
        >
          {{ cargando() ? 'Abriendo…' : 'Ver documento' }}
        </button>
      }
      @if (error(); as mensaje) {
        <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
      }

      @if (editable()) {
        <app-decision-con-motivo
          [opciones]="opciones"
          [contexto]="titulo()"
          [ocupado]="decidiendo()"
          (decidir)="decidirDocumento($event)"
        />
      }
    </article>
  `,
  styles: `
    .documento {
      display: grid;
      gap: var(--espacio-2);
      padding: var(--espacio-4);
      margin-bottom: var(--espacio-3);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
    }
    header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--espacio-2);
    }
    h3 {
      margin: 0;
      font-size: var(--texto-base);
    }
    p {
      margin: 0;
    }
    .estado {
      padding: 0 var(--espacio-2);
      border-radius: 999px;
      background: #e8f0f8;
      color: var(--color-primario-oscuro);
      font-size: var(--texto-chico);
      font-weight: 600;
    }
    .estado--Aprobado {
      background: #e3f1e6;
      color: var(--color-exito);
    }
    .estado--RequiereCorreccion {
      background: #fdf3e2;
      color: var(--color-aviso);
    }
    .estado--Rechazado {
      background: #fbeceb;
      color: var(--color-error);
    }
    .visor {
      margin: 0;
    }
    .visor img {
      width: 100%;
      border-radius: var(--radio);
      border: 1px solid #d5dbe1;
    }
  `,
})
export class DocumentoRevision {
  private readonly api = inject(RevisionesApi);

  readonly documento = input.required<TipoDocumentoRevision>();
  readonly editable = input(false, { transform: booleanAttribute });
  readonly cambio = output<void>();

  protected readonly opciones = OPCIONES;
  protected readonly textoEstado = TEXTO_ESTADO;
  protected readonly titulo = computed(() => tituloDocumento(this.documento().tipo));
  protected readonly vista = signal<UrlDocumento | null>(null);
  protected readonly cargando = signal(false);
  protected readonly decidiendo = signal(false);
  protected readonly error = signal<string | null>(null);
  private vencimientoVista: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.cancelarVencimiento());
  }

  protected async ver(): Promise<void> {
    this.error.set(null);
    this.cargando.set(true);
    try {
      const vista = await this.api.verDocumento(this.documento().id);
      this.vista.set(vista);
      this.programarVencimiento(vista.expiraEn);
    } catch (error: unknown) {
      this.error.set(mensajeGeneral(error));
    } finally {
      this.cargando.set(false);
    }
  }

  protected async decidirDocumento({
    decision,
    motivo,
  }: DecisionTomada<DecisionDocumento>): Promise<void> {
    this.error.set(null);
    this.decidiendo.set(true);
    try {
      await this.api.decidirDocumento(this.documento().id, decision, motivo);
      this.cambio.emit();
    } catch (error: unknown) {
      this.error.set(mensajeGeneral(error));
    } finally {
      this.decidiendo.set(false);
    }
  }

  /** Cierra el visor cuando vence la URL firmada (5 min) para que se pida una nueva. */
  protected cerrarVista(mensaje?: string): void {
    this.cancelarVencimiento();
    this.vista.set(null);
    if (mensaje) {
      this.error.set(mensaje);
    }
  }

  private programarVencimiento(expiraEn: string): void {
    this.cancelarVencimiento();
    const restante = Math.max(new Date(expiraEn).getTime() - Date.now(), 0);
    this.vencimientoVista = setTimeout(() => this.cerrarVista(), restante);
  }

  private cancelarVencimiento(): void {
    if (this.vencimientoVista) {
      clearTimeout(this.vencimientoVista);
      this.vencimientoVista = null;
    }
  }
}
