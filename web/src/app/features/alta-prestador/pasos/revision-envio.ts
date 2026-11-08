import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PerfilPropio } from '../../../core/api/modelos';
import { ErrorApi } from '../../../core/http/errores.interceptor';
import { esMayorDeEdad } from '../../../shared/fechas/edad';
import { mensajeGeneral } from '../../../shared/formularios/errores';
import { Boton } from '../../../shared/ui/boton';
import { AltaApi } from '../alta-api';
import { AltaEstado } from '../alta-estado';
import { DOCUMENTOS } from '../pasos';

interface PuntoRevision {
  readonly texto: string;
  readonly listo: boolean;
  readonly paso: number;
  readonly opcional: boolean;
}

/** Revisión local antes de enviar; la decisión final de qué falta la da la API (422). */
export function puntosDeRevision(perfil: PerfilPropio): PuntoRevision[] {
  const datos = perfil.datosPersonales;
  const documento = (tipo: string): boolean =>
    perfil.documentos.some(
      (d) => d.tipo === tipo && d.estado !== 'RequiereCorreccion' && d.estado !== 'Rechazado',
    );
  return [
    {
      texto: 'Datos personales',
      listo: !!(datos?.nombreCompleto && datos.telefono && datos.direccion && datos.coloniaBase),
      paso: 1,
      opcional: false,
    },
    { texto: 'Oficios y experiencia', listo: perfil.oficios.length > 0, paso: 2, opcional: false },
    {
      texto: 'Tarifas de referencia',
      listo: perfil.oficios.length > 0 && perfil.oficios.every((o) => o.tarifaMinCentavos > 0),
      paso: 3,
      opcional: false,
    },
    { texto: 'Zona de cobertura', listo: perfil.cobertura.length > 0, paso: 4, opcional: false },
    { texto: 'Disponibilidad', listo: perfil.disponibilidad.length > 0, paso: 5, opcional: true },
    { texto: 'Descripción', listo: perfil.descripcion.length > 0, paso: 6, opcional: true },
    {
      texto: 'Consentimiento de datos sensibles',
      listo: !!perfil.consentimientoSensibleVersion,
      paso: 7,
      opcional: false,
    },
    ...DOCUMENTOS.map((d) => ({
      texto: d.titulo,
      listo: documento(d.tipo),
      paso: 8,
      opcional: !d.obligatorio,
    })),
    { texto: 'Video y fotos', listo: perfil.portafolio.length > 0, paso: 9, opcional: true },
  ];
}

/** Paso 10: resumen y envío a revisión (FR-009; US1, escenarios 3 y 7). */
@Component({
  selector: 'app-paso-revision-envio',
  imports: [RouterLink, Boton],
  template: `
    <h1>Revisa y envía tu perfil</h1>
    <p>Un administrador revisará tus documentos, normalmente en menos de 48 horas.</p>

    @if (menorDeEdad()) {
      <p class="alerta alerta--aviso">Necesitas 18 años o más para publicar un perfil.</p>
    }

    <ul class="revision">
      @for (punto of puntos(); track punto.texto) {
        <li [class.revision--falta]="!punto.listo && !punto.opcional">
          <span aria-hidden="true">{{ punto.listo ? '✓' : punto.opcional ? '–' : '✗' }}</span>
          <span class="revision__texto">
            {{ punto.texto }}
            <span class="solo-lectores"
              >:
              {{
                punto.listo ? 'listo' : punto.opcional ? 'opcional, sin completar' : 'falta'
              }}</span
            >
            @if (punto.opcional) {
              <span class="texto-suave">(opcional)</span>
            }
          </span>
          @if (!punto.listo) {
            <a [routerLink]="['/alta', punto.paso]"
              >Completar<span class="solo-lectores"> {{ punto.texto }}</span></a
            >
          }
        </li>
      }
    </ul>

    @if (errorEnvio(); as mensaje) {
      <div class="alerta alerta--error" role="alert" data-testid="error-envio">
        <p>{{ mensaje }}</p>
        @if (faltantes().length > 0) {
          <ul>
            @for (faltante of faltantes(); track faltante) {
              <li>{{ faltante }}</li>
            }
          </ul>
        }
      </div>
    }

    <div class="acciones">
      <a appBoton variante="secundario" routerLink="/alta/9">Anterior</a>
      <button
        appBoton
        type="button"
        (click)="enviar()"
        [disabled]="enviando() || !estado.editable()"
        data-testid="enviar-revision"
      >
        {{ enviando() ? 'Enviando…' : 'Enviar a revisión' }}
      </button>
    </div>
  `,
  styles: `
    .revision {
      list-style: none;
      margin: 0 0 var(--espacio-4);
      padding: 0;
    }
    .revision li {
      display: flex;
      align-items: center;
      gap: var(--espacio-3);
      min-height: var(--tactil-min);
      border-bottom: 1px solid #e3e7eb;
    }
    .revision__texto {
      flex: 1;
    }
    .revision--falta {
      color: var(--color-error);
      font-weight: 600;
    }
    .alerta ul {
      margin: var(--espacio-2) 0 0;
      padding-left: var(--espacio-6);
    }
    .alerta p {
      margin: 0;
    }
  `,
})
export class PasoRevisionEnvio {
  protected readonly estado = inject(AltaEstado);
  private readonly api = inject(AltaApi);

  protected readonly puntos = computed(() => {
    const perfil = this.estado.perfil();
    return perfil ? puntosDeRevision(perfil) : [];
  });
  protected readonly menorDeEdad = computed(() => {
    const fecha = this.estado.perfil()?.datosPersonales?.fechaNacimiento;
    return !!fecha && !esMayorDeEdad(fecha);
  });
  protected readonly enviando = signal(false);
  protected readonly errorEnvio = signal<string | null>(null);
  protected readonly faltantes = signal<readonly string[]>([]);

  protected async enviar(): Promise<void> {
    this.errorEnvio.set(null);
    this.faltantes.set([]);
    this.enviando.set(true);
    try {
      await this.api.enviarRevision();
      this.estado.actualizar((perfil) => ({
        ...perfil,
        estado: 'EnRevision',
        motivoEstado: undefined,
      }));
      await this.estado.irAEstado();
    } catch (error: unknown) {
      this.errorEnvio.set(mensajeGeneral(error));
      this.faltantes.set(error instanceof ErrorApi ? error.faltantes : []);
    } finally {
      this.enviando.set(false);
    }
  }
}
