import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  DocumentoPropio,
  ElementoPortafolioPropio,
  GuardadoPaso,
  PerfilPropio,
  SolicitudUrlSubida,
  TipoDocumento,
  UrlSubida,
} from '../../core/api/modelos';
import { ENTORNO } from '../../core/entorno';

/** Acceso a `/prestadores/yo/*` (contracts/api.md, sección Alta del prestador). */
@Injectable({ providedIn: 'root' })
export class AltaApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${inject(ENTORNO).apiUrl}/prestadores/yo`;

  perfil(): Promise<PerfilPropio> {
    return firstValueFrom(this.http.get<PerfilPropio>(this.base));
  }

  guardar(cambios: GuardadoPaso): Promise<PerfilPropio> {
    return firstValueFrom(this.http.patch<PerfilPropio>(this.base, cambios));
  }

  aceptarConsentimiento(version: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.base}/consentimiento-sensible`, { version }),
    );
  }

  urlSubida(solicitud: SolicitudUrlSubida): Promise<UrlSubida> {
    return firstValueFrom(this.http.post<UrlSubida>(`${this.base}/archivos/url-subida`, solicitud));
  }

  registrarDocumento(
    archivoId: string,
    tipo: TipoDocumento,
    fechaExpedicion?: string,
  ): Promise<DocumentoPropio> {
    return firstValueFrom(
      this.http.post<DocumentoPropio>(`${this.base}/documentos`, {
        archivoId,
        tipo,
        ...(fechaExpedicion ? { fechaExpedicion } : {}),
      }),
    );
  }

  agregarPortafolio(
    archivoId: string,
    tipo: 'Foto' | 'Video',
    descripcion: string,
  ): Promise<ElementoPortafolioPropio> {
    return firstValueFrom(
      this.http.post<ElementoPortafolioPropio>(`${this.base}/portafolio`, {
        archivoId,
        tipo,
        descripcion,
      }),
    );
  }

  eliminarPortafolio(id: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(`${this.base}/portafolio/${encodeURIComponent(id)}`),
    );
  }

  enviarRevision(): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/enviar-revision`, null));
  }
}
