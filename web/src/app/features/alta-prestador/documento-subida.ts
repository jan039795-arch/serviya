import { Component, computed, inject, input, signal } from '@angular/core';
import { DocumentoPropio } from '../../core/api/modelos';
import { diasDesde, hoyIso } from '../../shared/fechas/edad';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { ArchivoSubido, SolicitarSubida, SubidaArchivo } from '../../shared/subida/subida-archivo';
import { AltaApi } from './alta-api';
import { AltaEstado } from './alta-estado';
import { DescripcionDocumento } from './pasos';

const MAXIMO_DIAS_COMPROBANTE = 90;

/**
 * Tarjeta de un documento: estado, motivo de corrección, fecha de expedición si aplica y subida.
 * La usan el paso 8 y la pantalla de corrección.
 */
@Component({
  selector: 'app-documento-subida',
  imports: [SubidaArchivo],
  template: `
    <article class="documento" [attr.data-testid]="'documento-' + descripcion().tipo">
      <h2>
        {{ descripcion().titulo }}
        @if (!descripcion().obligatorio) {
          <span class="texto-suave">(opcional)</span>
        }
      </h2>
      @switch (actual()?.estado) {
        @case ('Cargado') {
          <p class="estado estado--ok">✓ Subido. Lo revisaremos pronto.</p>
        }
        @case ('Aprobado') {
          <p class="estado estado--ok">✓ Aprobado.</p>
        }
        @case ('RequiereCorreccion') {
          <p class="alerta alerta--aviso">
            Necesitamos que lo subas de nuevo: {{ actual()?.motivo }}
          </p>
        }
        @case ('Rechazado') {
          <p class="alerta alerta--error">Rechazado: {{ actual()?.motivo }}</p>
        }
      }

      @if (descripcion().pideFecha) {
        <label class="fecha">
          <span>Fecha de expedición</span>
          <input
            type="date"
            [max]="hoy"
            [value]="fecha()"
            (input)="fecha.set($any($event.target).value)"
            [attr.data-testid]="'fecha-' + descripcion().tipo"
          />
        </label>
      }

      @if (errorRegistro(); as mensaje) {
        <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
      }

      <app-subida-archivo
        [etiqueta]="actual() ? 'Reemplazar archivo' : 'Elegir foto o PDF'"
        tipo="documento"
        [ayuda]="descripcion().ayuda"
        [captura]="descripcion().tipo === 'Selfie' ? 'user' : undefined"
        [solicitarSubida]="solicitar"
        (subido)="registrar($event)"
      />
    </article>
  `,
  styles: `
    .documento {
      margin-bottom: var(--espacio-4);
      padding: var(--espacio-4);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
    }
    h2 {
      font-size: var(--texto-base);
    }
    .estado {
      margin: 0 0 var(--espacio-2);
      font-weight: 600;
    }
    .estado--ok {
      color: var(--color-exito);
    }
    .fecha {
      display: grid;
      gap: var(--espacio-1);
      margin-bottom: var(--espacio-3);
      font-weight: 600;
    }
  `,
})
export class DocumentoSubida {
  private readonly api = inject(AltaApi);
  private readonly estado = inject(AltaEstado);

  readonly descripcion = input.required<DescripcionDocumento>();

  protected readonly hoy = hoyIso();
  protected readonly actual = computed<DocumentoPropio | undefined>(() =>
    this.estado.perfil()?.documentos.find((d) => d.tipo === this.descripcion().tipo),
  );
  protected readonly fecha = signal('');
  protected readonly errorRegistro = signal<string | null>(null);

  /** Pide la URL de subida; antes valida la fecha de expedición si el documento la requiere. */
  protected readonly solicitar: SolicitarSubida = async (archivo) => {
    this.validarFecha();
    const { archivoId, urlSubida } = await this.api.urlSubida({
      destino: 'Documento',
      tipo: this.descripcion().tipo,
      tipoContenido: archivo.tipoContenido,
      tamanoBytes: archivo.tamanoBytes,
    });
    return { archivoId, urlSubida };
  };

  protected async registrar({ archivoId }: ArchivoSubido): Promise<void> {
    this.errorRegistro.set(null);
    const { tipo, pideFecha } = this.descripcion();
    try {
      const documento = await this.api.registrarDocumento(
        archivoId,
        tipo,
        pideFecha ? this.fecha() : undefined,
      );
      this.estado.actualizar((perfil) => ({
        ...perfil,
        documentos: [...perfil.documentos.filter((d) => d.tipo !== tipo), documento],
      }));
    } catch (error: unknown) {
      this.errorRegistro.set(mensajeGeneral(error));
    }
  }

  private validarFecha(): void {
    const { tipo, pideFecha } = this.descripcion();
    if (!pideFecha) {
      return;
    }
    const dias = this.fecha() ? diasDesde(this.fecha()) : null;
    if (dias === null) {
      throw new Error('Primero escribe la fecha de expedición del documento.');
    }
    if (dias < 0) {
      throw new Error('La fecha de expedición no puede ser futura.');
    }
    if (tipo === 'ComprobanteDomicilio' && dias > MAXIMO_DIAS_COMPROBANTE) {
      throw new Error('El comprobante debe tener máximo 3 meses. Busca uno más reciente.');
    }
  }
}
