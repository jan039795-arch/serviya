import { Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import {
  comprimirImagen,
  leerDuracionVideo,
  TAMANO_MAXIMO_DOCUMENTO_BYTES,
  validarVideo,
} from './comprimir-imagen';
import { Subidor } from './subidor';

export type TipoSubida = 'foto' | 'video' | 'documento';

export interface ArchivoPreparado {
  readonly blob: Blob;
  readonly tipoContenido: string;
  readonly tamanoBytes: number;
  readonly duracionSegundos?: number;
}

export interface DestinoSubida {
  readonly archivoId: string;
  readonly urlSubida: string;
}

/** Pide a la API una URL de subida (`POST /prestadores/yo/archivos/url-subida`, etc.). */
export type SolicitarSubida = (archivo: ArchivoPreparado) => Promise<DestinoSubida>;

export interface ArchivoSubido {
  readonly archivoId: string;
  readonly archivo: ArchivoPreparado;
}

type Estado =
  | { readonly fase: 'inicial' }
  | { readonly fase: 'procesando' }
  | { readonly fase: 'subiendo'; readonly avance: number }
  | { readonly fase: 'listo' }
  | { readonly fase: 'error'; readonly mensaje: string; readonly reintentable: boolean };

const ACEPTA: Readonly<Record<TipoSubida, string>> = {
  foto: 'image/*',
  video: 'video/*',
  documento: 'image/*,application/pdf',
};

let siguienteId = 0;

/**
 * Selección, preparación y subida de un archivo con avance y reintento (principio IV): si la
 * conexión falla, se reintenta con el mismo archivo ya preparado, sin volver a elegirlo.
 */
@Component({
  selector: 'app-subida-archivo',
  template: `
    @if (ayuda()) {
      <p class="subida__ayuda" [id]="idAyuda">{{ ayuda() }}</p>
    }
    <!-- El input nativo queda oculto pero enfocable; la etiqueta hace de botón visible. -->
    <label
      class="boton boton--secundario subida__boton"
      [class.subida__boton--ocupado]="ocupado()"
      [for]="idInput"
    >
      {{ etiqueta() }}
    </label>
    <input
      type="file"
      class="solo-lectores"
      [id]="idInput"
      [accept]="acepta()"
      [attr.capture]="captura() ?? null"
      [attr.aria-describedby]="ayuda() ? idAyuda : null"
      [disabled]="ocupado()"
      (change)="seleccionar($event)"
    />
    @if (vistaPrevia(); as url) {
      <img class="subida__vista" [src]="url" alt="Vista previa del archivo elegido" />
    }
    <div class="subida__estado" aria-live="polite">
      @switch (estado().fase) {
        @case ('procesando') {
          <p>Preparando el archivo…</p>
        }
        @case ('subiendo') {
          <progress max="100" [value]="avance()" aria-label="Avance de la subida"></progress>
          <p>Subiendo… {{ avance() }}%</p>
        }
        @case ('listo') {
          <p class="subida__listo">Archivo subido.</p>
        }
        @case ('error') {
          <p class="subida__error" role="alert">{{ mensajeError() }}</p>
          @if (reintentable()) {
            <button type="button" class="boton boton--secundario" (click)="reintentar()">
              Reintentar
            </button>
          }
        }
      }
    </div>
  `,
  styles: `
    :host {
      display: grid;
      gap: var(--espacio-2);
      margin-bottom: var(--espacio-4);
    }
    .subida__boton {
      justify-self: start;
    }
    .subida__boton--ocupado {
      opacity: 0.55;
      pointer-events: none;
    }
    :host:has(input:focus-visible) .subida__boton {
      outline: 3px solid var(--color-foco);
      outline-offset: 2px;
    }
    .subida__ayuda {
      margin: 0;
      color: var(--color-texto-suave);
      font-size: var(--texto-chico);
    }
    .subida__vista {
      max-height: 160px;
      width: auto;
      border-radius: var(--radio);
    }
    .subida__estado p {
      margin: 0;
    }
    progress {
      width: 100%;
      height: 0.75rem;
    }
    .subida__listo {
      color: var(--color-exito);
      font-weight: 600;
    }
    .subida__error {
      color: var(--color-error);
      font-weight: 600;
    }
  `,
})
export class SubidaArchivo {
  readonly etiqueta = input.required<string>();
  readonly tipo = input.required<TipoSubida>();
  readonly solicitarSubida = input.required<SolicitarSubida>();
  readonly ayuda = input<string>();
  /** `user` abre la cámara frontal (selfie) y `environment` la trasera. */
  readonly captura = input<'user' | 'environment'>();
  readonly subido = output<ArchivoSubido>();

  private readonly subidor = inject(Subidor);
  private readonly destroyRef = inject(DestroyRef);
  private preparado: ArchivoPreparado | null = null;
  private subida: Subscription | null = null;

  protected readonly idInput = `subida-${++siguienteId}`;
  protected readonly idAyuda = `${this.idInput}-ayuda`;
  protected readonly estado = signal<Estado>({ fase: 'inicial' });
  protected readonly vistaPrevia = signal<string | null>(null);
  protected readonly acepta = computed(() => ACEPTA[this.tipo()]);
  protected readonly ocupado = computed(() => {
    const fase = this.estado().fase;
    return fase === 'procesando' || fase === 'subiendo';
  });
  protected readonly avance = computed(() => {
    const estado = this.estado();
    return estado.fase === 'subiendo' ? estado.avance : 0;
  });
  protected readonly mensajeError = computed(() => {
    const estado = this.estado();
    return estado.fase === 'error' ? estado.mensaje : '';
  });
  protected readonly reintentable = computed(() => {
    const estado = this.estado();
    return estado.fase === 'error' && estado.reintentable;
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.subida?.unsubscribe();
      this.cambiarVistaPrevia(null);
    });
  }

  protected async seleccionar(evento: Event): Promise<void> {
    const entrada = evento.target as HTMLInputElement;
    const archivo = entrada.files?.[0];
    // Se limpia para que volver a elegir el mismo archivo (tras un error) dispare `change` otra vez.
    entrada.value = '';
    if (!archivo) {
      return;
    }
    this.estado.set({ fase: 'procesando' });
    try {
      this.preparado = await this.preparar(archivo);
    } catch (error: unknown) {
      this.preparado = null;
      this.estado.set({ fase: 'error', mensaje: mensajeDe(error), reintentable: false });
      return;
    }
    const esImagen = this.preparado.tipoContenido.startsWith('image/');
    this.cambiarVistaPrevia(esImagen ? URL.createObjectURL(this.preparado.blob) : null);
    await this.subir();
  }

  protected reintentar(): Promise<void> {
    return this.subir();
  }

  private async subir(): Promise<void> {
    const archivo = this.preparado;
    if (!archivo) {
      return;
    }
    this.estado.set({ fase: 'subiendo', avance: 0 });
    let destino: DestinoSubida;
    try {
      // Se pide una URL nueva en cada intento: las firmadas vencen a los 10 minutos.
      destino = await this.solicitarSubida()(archivo);
    } catch (error: unknown) {
      this.estado.set({ fase: 'error', mensaje: mensajeDe(error), reintentable: true });
      return;
    }
    this.subida?.unsubscribe();
    this.subida = this.subidor
      .subir(destino.urlSubida, archivo.blob, archivo.tipoContenido)
      .subscribe({
        next: (avance) => this.estado.set({ fase: 'subiendo', avance }),
        error: (error: unknown) =>
          this.estado.set({ fase: 'error', mensaje: mensajeDe(error), reintentable: true }),
        complete: () => {
          this.estado.set({ fase: 'listo' });
          this.subido.emit({ archivoId: destino.archivoId, archivo });
        },
      });
  }

  private async preparar(archivo: File): Promise<ArchivoPreparado> {
    switch (this.tipo()) {
      case 'video':
        return this.prepararVideo(archivo);
      case 'documento':
        return archivo.type === 'application/pdf'
          ? prepararPdf(archivo)
          : prepararFoto(archivo, true);
      case 'foto':
        return prepararFoto(archivo, false);
    }
  }

  private async prepararVideo(archivo: File): Promise<ArchivoPreparado> {
    if (!archivo.type.startsWith('video/')) {
      throw new Error('Elige un archivo de video.');
    }
    const duracion = await leerDuracionVideo(archivo);
    const motivo = validarVideo(duracion, archivo.size);
    if (motivo) {
      throw new Error(motivo);
    }
    return {
      blob: archivo,
      tipoContenido: archivo.type,
      tamanoBytes: archivo.size,
      duracionSegundos: duracion,
    };
  }

  private cambiarVistaPrevia(url: string | null): void {
    const anterior = this.vistaPrevia();
    if (anterior) {
      URL.revokeObjectURL(anterior);
    }
    this.vistaPrevia.set(url);
  }
}

async function prepararFoto(archivo: File, esDocumento: boolean): Promise<ArchivoPreparado> {
  if (!archivo.type.startsWith('image/')) {
    throw new Error(esDocumento ? 'Sube una foto o un PDF.' : 'Elige una foto.');
  }
  const blob = await comprimirImagen(archivo);
  if (esDocumento && blob.size > TAMANO_MAXIMO_DOCUMENTO_BYTES) {
    throw new Error('El archivo pesa más de 10 MB.');
  }
  return { blob, tipoContenido: blob.type, tamanoBytes: blob.size };
}

function prepararPdf(archivo: File): ArchivoPreparado {
  if (archivo.size > TAMANO_MAXIMO_DOCUMENTO_BYTES) {
    throw new Error('El PDF pesa más de 10 MB.');
  }
  return { blob: archivo, tipoContenido: 'application/pdf', tamanoBytes: archivo.size };
}

function mensajeDe(error: unknown): string {
  return error instanceof Error && error.message
    ? error.message
    : 'Algo salió mal con el archivo. Inténtalo de nuevo.';
}
