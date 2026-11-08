import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  DecisionDocumento,
  DecisionModeracion,
  DecisionPerfil,
  PerfilRevision,
  RevisionPendiente,
  UrlDocumento,
} from '../../../core/api/modelos';
import { ENTORNO } from '../../../core/entorno';

/** Revisión de perfiles: `/admin/revisiones`, `/admin/prestadores`, `/admin/documentos`. */
@Injectable({ providedIn: 'root' })
export class RevisionesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${inject(ENTORNO).apiUrl}/admin`;

  pendientes(): Promise<RevisionPendiente[]> {
    return firstValueFrom(
      this.http.get<RevisionPendiente[]>(`${this.base}/revisiones`, {
        params: { estado: 'EnRevision' },
      }),
    );
  }

  perfil(prestadorId: string): Promise<PerfilRevision> {
    return firstValueFrom(
      this.http.get<PerfilRevision>(`${this.base}/prestadores/${encodeURIComponent(prestadorId)}`),
    );
  }

  /** Cada llamada queda en la bitácora como `VerDocumento` (FR-016). */
  verDocumento(documentoId: string): Promise<UrlDocumento> {
    return firstValueFrom(
      this.http.post<UrlDocumento>(
        `${this.base}/documentos/${encodeURIComponent(documentoId)}/ver`,
        null,
      ),
    );
  }

  decidirDocumento(
    documentoId: string,
    decision: DecisionDocumento,
    motivo?: string,
  ): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.base}/documentos/${encodeURIComponent(documentoId)}/decision`, {
        decision,
        motivo,
      }),
    );
  }

  decidirPerfil(prestadorId: string, decision: DecisionPerfil, motivo?: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.base}/prestadores/${encodeURIComponent(prestadorId)}/decision`, {
        decision,
        motivo,
      }),
    );
  }

  decidirPortafolio(
    elementoId: string,
    decision: DecisionModeracion,
    motivo?: string,
  ): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.base}/portafolio/${encodeURIComponent(elementoId)}/decision`, {
        decision,
        motivo,
      }),
    );
  }
}
