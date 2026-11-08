import { Component, computed, inject, signal } from '@angular/core';
import { ElementoPortafolioPropio } from '../../../core/api/modelos';
import { mensajeGeneral } from '../../../shared/formularios/errores';
import {
  ArchivoSubido,
  SolicitarSubida,
  SubidaArchivo,
} from '../../../shared/subida/subida-archivo';
import { Boton } from '../../../shared/ui/boton';
import { AltaApi } from '../alta-api';
import { AltaEstado } from '../alta-estado';
import { PiePaso } from './pie-paso';

const MAXIMO_FOTOS = 20;
const LARGO_DESCRIPCION = 200;

const ESTADO_MODERACION: Readonly<Record<ElementoPortafolioPropio['estadoModeracion'], string>> = {
  Pendiente: 'Pendiente de revisión',
  Aprobado: 'Publicada',
  Rechazado: 'No aprobada',
};

/** Paso 9: video opcional (90 s) y hasta 20 fotos con descripción (FR-008). */
@Component({
  selector: 'app-paso-video-portafolio',
  imports: [SubidaArchivo, Boton, PiePaso],
  template: `
    <h1>Muestra tu trabajo</h1>
    <p>
      Son opcionales, pero ayudan mucho a que te elijan. Las revisamos antes de publicarlas. No
      incluyas teléfonos ni datos de contacto en las fotos.
    </p>
    @if (errorEnvio(); as mensaje) {
      <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
    }

    <h2>Video de presentación</h2>
    @if (video(); as elemento) {
      <p>Video subido · {{ estadoModeracion[elemento.estadoModeracion] }}</p>
      <button appBoton variante="secundario" type="button" (click)="eliminar(elemento)">
        Quitar video
      </button>
    } @else {
      <app-subida-archivo
        etiqueta="Elegir video"
        tipo="video"
        ayuda="Máximo 90 segundos. Preséntate y cuenta qué haces."
        [solicitarSubida]="solicitar('Video')"
        (subido)="agregar($event, 'Video', '')"
      />
    }

    <h2>Fotos de trabajos ({{ fotos().length }} de {{ maximoFotos }})</h2>
    <ul class="fotos">
      @for (foto of fotos(); track foto.id) {
        <li>
          @if (foto.url) {
            <img
              [src]="foto.url"
              [alt]="foto.descripcion || 'Foto de trabajo'"
              width="96"
              height="72"
            />
          }
          <div>
            <p>{{ foto.descripcion || 'Sin descripción' }}</p>
            <p class="texto-suave">{{ estadoModeracion[foto.estadoModeracion] }}</p>
          </div>
          <button appBoton variante="texto" type="button" (click)="eliminar(foto)">
            Quitar<span class="solo-lectores"> la foto {{ foto.descripcion }}</span>
          </button>
        </li>
      }
    </ul>

    @if (fotos().length < maximoFotos) {
      <label class="descripcion">
        <span>Descripción de la siguiente foto (opcional)</span>
        <input
          [value]="descripcion()"
          (input)="descripcion.set($any($event.target).value)"
          [attr.maxlength]="largoDescripcion"
          data-testid="descripcion-foto"
        />
      </label>
      @for (clave of [contador()]; track clave) {
        <app-subida-archivo
          etiqueta="Agregar foto"
          tipo="foto"
          [solicitarSubida]="solicitar('Foto')"
          (subido)="agregar($event, 'Foto', descripcion())"
        />
      }
    }

    <form novalidate (submit)="continuar($event)">
      <app-pie-paso [paso]="9" />
    </form>
  `,
  styles: `
    .fotos {
      list-style: none;
      margin: 0 0 var(--espacio-4);
      padding: 0;
    }
    .fotos li {
      display: flex;
      align-items: center;
      gap: var(--espacio-3);
      padding: var(--espacio-2) 0;
      border-bottom: 1px solid #e3e7eb;
    }
    .fotos img {
      border-radius: var(--radio);
      object-fit: cover;
    }
    .fotos div {
      flex: 1;
    }
    .fotos p {
      margin: 0;
    }
    .descripcion {
      display: grid;
      gap: var(--espacio-1);
      margin-bottom: var(--espacio-3);
      font-weight: 600;
    }
  `,
})
export class PasoVideoPortafolio {
  private readonly estado = inject(AltaEstado);
  private readonly api = inject(AltaApi);

  protected readonly maximoFotos = MAXIMO_FOTOS;
  protected readonly largoDescripcion = LARGO_DESCRIPCION;
  protected readonly estadoModeracion = ESTADO_MODERACION;
  protected readonly descripcion = signal('');
  /** Cambia tras cada foto para volver a montar el selector vacío. */
  protected readonly contador = signal(0);
  protected readonly errorEnvio = signal<string | null>(null);

  private readonly portafolio = computed(() => this.estado.perfil()?.portafolio ?? []);
  protected readonly video = computed(() => this.portafolio().find((e) => e.tipo === 'Video'));
  protected readonly fotos = computed(() => this.portafolio().filter((e) => e.tipo === 'Foto'));

  private readonly solicitudes: Readonly<Record<'Foto' | 'Video', SolicitarSubida>> = {
    Foto: (archivo) => this.pedirUrl('Foto', archivo.tipoContenido, archivo.tamanoBytes),
    Video: (archivo) => this.pedirUrl('Video', archivo.tipoContenido, archivo.tamanoBytes),
  };

  protected solicitar(tipo: 'Foto' | 'Video'): SolicitarSubida {
    return this.solicitudes[tipo];
  }

  protected async agregar(
    { archivoId }: ArchivoSubido,
    tipo: 'Foto' | 'Video',
    descripcion: string,
  ): Promise<void> {
    this.errorEnvio.set(null);
    try {
      const elemento = await this.api.agregarPortafolio(archivoId, tipo, descripcion.trim());
      this.estado.actualizar((perfil) => ({
        ...perfil,
        portafolio: [...perfil.portafolio, elemento],
      }));
      if (tipo === 'Foto') {
        this.descripcion.set('');
        this.contador.update((n) => n + 1);
      }
    } catch (error: unknown) {
      this.errorEnvio.set(mensajeGeneral(error));
    }
  }

  protected async eliminar(elemento: ElementoPortafolioPropio): Promise<void> {
    this.errorEnvio.set(null);
    try {
      await this.api.eliminarPortafolio(elemento.id);
      this.estado.actualizar((perfil) => ({
        ...perfil,
        portafolio: perfil.portafolio.filter((e) => e.id !== elemento.id),
      }));
    } catch (error: unknown) {
      this.errorEnvio.set(mensajeGeneral(error));
    }
  }

  protected async continuar(evento: Event): Promise<void> {
    evento.preventDefault();
    await this.estado.guardarYAvanzar({ paso: 9 });
  }

  private async pedirUrl(tipo: 'Foto' | 'Video', tipoContenido: string, tamanoBytes: number) {
    const { archivoId, urlSubida } = await this.api.urlSubida({
      destino: 'Portafolio',
      tipo,
      tipoContenido,
      tamanoBytes,
    });
    return { archivoId, urlSubida };
  }
}
