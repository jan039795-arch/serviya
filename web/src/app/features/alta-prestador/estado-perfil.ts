import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EstadoPerfil as TipoEstadoPerfil } from '../../core/api/modelos';
import { ErrorApi } from '../../core/http/errores.interceptor';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { Boton } from '../../shared/ui/boton';
import { AltaApi } from './alta-api';
import { AltaEstado } from './alta-estado';
import { DocumentoSubida } from './documento-subida';
import { DOCUMENTOS } from './pasos';

const TEXTOS: Readonly<Record<TipoEstadoPerfil, { titulo: string; explicacion: string }>> = {
  Borrador: {
    titulo: 'Tu alta está a medias',
    explicacion: 'Completa los pasos que faltan y envía tu perfil a revisión.',
  },
  EnRevision: {
    titulo: 'Estamos revisando tu perfil',
    explicacion:
      'Revisamos tus documentos, normalmente en menos de 48 horas. Te avisaremos por correo.',
  },
  RequiereCorreccion: {
    titulo: 'Necesitamos que corrijas algo',
    explicacion: 'Vuelve a subir solo lo que te pedimos y envía tu perfil de nuevo.',
  },
  Aprobado: {
    titulo: '¡Tu perfil está publicado!',
    explicacion: 'Los clientes de tu zona ya pueden encontrarte.',
  },
  Rechazado: {
    titulo: 'Tu perfil no fue aprobado',
    explicacion: 'Si crees que es un error, escríbenos desde la ayuda.',
  },
  Suspendido: {
    titulo: 'Tu perfil está suspendido',
    explicacion: 'No aparece en las búsquedas. Puedes enviar una aclaración.',
  },
  Eliminado: {
    titulo: 'Tu perfil fue eliminado',
    explicacion: 'Ya no aparece en ServiYa.',
  },
};

/** Estado del perfil y corrección de documentos señalados (FR-011; US1, escenario 4). */
@Component({
  selector: 'app-estado-perfil',
  imports: [RouterLink, Boton, DocumentoSubida],
  template: `
    @if (estado.perfil(); as perfil) {
      <h1 data-testid="titulo-estado">{{ textos[perfil.estado].titulo }}</h1>
      <p>{{ textos[perfil.estado].explicacion }}</p>
      @if (perfil.motivoEstado) {
        <p class="alerta alerta--aviso">Motivo: {{ perfil.motivoEstado }}</p>
      }

      @switch (perfil.estado) {
        @case ('Borrador') {
          <a appBoton anchoCompleto [routerLink]="['/alta', estado.pasoParaRetomar()]"
            >Continuar mi alta</a
          >
        }
        @case ('RequiereCorreccion') {
          @for (documento of porCorregir(); track documento.tipo) {
            <app-documento-subida [descripcion]="documento" />
          }
          @for (foto of fotosRechazadas(); track foto.id) {
            <p class="alerta alerta--aviso">
              Una foto no se aprobó: {{ foto.motivoRechazo }}. Puedes quitarla o cambiarla en el
              <a routerLink="/alta/9">paso 9</a>.
            </p>
          }
          @if (errorEnvio(); as mensaje) {
            <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
          }
          <button
            appBoton
            anchoCompleto
            type="button"
            (click)="reenviar()"
            [disabled]="enviando() || pendientes() > 0"
            data-testid="reenviar"
          >
            {{
              pendientes() > 0 ? 'Sube lo que falta para reenviar' : 'Enviar de nuevo a revisión'
            }}
          </button>
        }
        @case ('Aprobado') {
          <a appBoton anchoCompleto [routerLink]="['/prestador', perfil.id]"
            >Ver mi perfil público</a
          >
        }
      }
    }
  `,
})
export class EstadoPerfil {
  protected readonly estado = inject(AltaEstado);
  private readonly api = inject(AltaApi);

  protected readonly textos = TEXTOS;
  protected readonly enviando = signal(false);
  protected readonly errorEnvio = signal<string | null>(null);

  /** Documentos marcados para corrección; se siguen mostrando ya resubidos, como "Subido". */
  private readonly tiposSenalados = new Set(
    (this.estado.perfil()?.documentos ?? [])
      .filter((d) => d.estado === 'RequiereCorreccion')
      .map((d) => d.tipo),
  );
  protected readonly porCorregir = computed(() =>
    DOCUMENTOS.filter((d) => this.tiposSenalados.has(d.tipo)),
  );
  protected readonly pendientes = computed(
    () =>
      (this.estado.perfil()?.documentos ?? []).filter((d) => d.estado === 'RequiereCorreccion')
        .length,
  );
  protected readonly fotosRechazadas = computed(() =>
    (this.estado.perfil()?.portafolio ?? []).filter((e) => e.estadoModeracion === 'Rechazado'),
  );

  protected async reenviar(): Promise<void> {
    this.errorEnvio.set(null);
    this.enviando.set(true);
    try {
      await this.api.enviarRevision();
      this.estado.actualizar((perfil) => ({
        ...perfil,
        estado: 'EnRevision',
        motivoEstado: undefined,
      }));
    } catch (error: unknown) {
      const faltantes = error instanceof ErrorApi ? error.faltantes : [];
      this.errorEnvio.set([mensajeGeneral(error), ...faltantes].join(' '));
    } finally {
      this.enviando.set(false);
    }
  }
}
