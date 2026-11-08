import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AltaEstado } from '../alta-estado';
import { DocumentoSubida } from '../documento-subida';
import { DOCUMENTOS } from '../pasos';
import { PiePaso } from './pie-paso';

/** Paso 8: INE por ambos lados, selfie y comprobante obligatorios; carta opcional (FR-006). */
@Component({
  selector: 'app-paso-documentos',
  imports: [RouterLink, DocumentoSubida, PiePaso],
  template: `
    <h1>Sube tus documentos</h1>
    @if (!conConsentimiento()) {
      <p class="alerta alerta--aviso">
        Antes de subir documentos necesitas dar tu consentimiento.
        <a routerLink="/alta/7">Ir al paso 7</a>
      </p>
    } @else {
      <p>Toma las fotos con buena luz y sin reflejos. Puedes subir foto o PDF de hasta 10 MB.</p>
      <p class="texto-suave">{{ obligatoriosListos() }} de 4 documentos obligatorios subidos.</p>
      @for (documento of documentos; track documento.tipo) {
        <app-documento-subida [descripcion]="documento" />
      }
      <form novalidate (submit)="continuar($event)">
        <app-pie-paso [paso]="8" />
      </form>
    }
  `,
})
export class PasoDocumentos {
  private readonly estado = inject(AltaEstado);

  protected readonly documentos = DOCUMENTOS;
  protected readonly conConsentimiento = computed(
    () => !!this.estado.perfil()?.consentimientoSensibleVersion,
  );
  protected readonly obligatoriosListos = computed(() => {
    const subidos = this.estado.perfil()?.documentos ?? [];
    return DOCUMENTOS.filter(
      (d) =>
        d.obligatorio &&
        subidos.some((s) => s.tipo === d.tipo && s.estado !== 'RequiereCorreccion'),
    ).length;
  });

  protected async continuar(evento: Event): Promise<void> {
    evento.preventDefault();
    await this.estado.guardarYAvanzar({ paso: 8 });
  }
}
