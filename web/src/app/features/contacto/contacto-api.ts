import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AccesoContacto,
  ContactoDesbloqueado,
  OpcionesAcceso,
  OrigenAcceso,
  Pago,
  PaseMensual,
  RespuestaAcceso,
  RespuestaPase,
} from '../../core/api/modelos';
import { ENTORNO } from '../../core/entorno';

export type ResultadoSimulado = 'aprobado' | 'pendiente' | 'rechazado' | 'acreditar';

/** Acceso al contacto, pagos y pase (contracts/api.md, sección Acceso al contacto y pagos). */
@Injectable({ providedIn: 'root' })
export class ContactoApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(ENTORNO).apiUrl;

  /** Sin `prestadorId` devuelve solo lo general: pase, oferta, créditos, aviso y precios. */
  opciones(prestadorId?: string): Promise<OpcionesAcceso> {
    const params: Record<string, string> = prestadorId ? { prestadorId } : {};
    return firstValueFrom(
      this.http.get<OpcionesAcceso>(`${this.apiUrl}/accesos/opciones`, { params }),
    );
  }

  solicitarAcceso(
    prestadorId: string,
    via: OrigenAcceso,
    avisoVersion: string,
  ): Promise<RespuestaAcceso> {
    return firstValueFrom(
      this.http.post<RespuestaAcceso>(`${this.apiUrl}/accesos`, { prestadorId, via, avisoVersion }),
    );
  }

  misAccesos(): Promise<AccesoContacto[]> {
    return firstValueFrom(this.http.get<AccesoContacto[]>(`${this.apiUrl}/accesos/yo`));
  }

  contacto(accesoId: string): Promise<ContactoDesbloqueado> {
    return firstValueFrom(
      this.http.get<ContactoDesbloqueado>(
        `${this.apiUrl}/accesos/${encodeURIComponent(accesoId)}/contacto`,
      ),
    );
  }

  pago(pagoId: string): Promise<Pago> {
    return firstValueFrom(
      this.http.get<Pago>(`${this.apiUrl}/pagos/${encodeURIComponent(pagoId)}`),
    );
  }

  comprarPase(renovacionAutomatica: boolean, avisoVersion: string): Promise<RespuestaPase> {
    return firstValueFrom(
      this.http.post<RespuestaPase>(`${this.apiUrl}/pases`, { renovacionAutomatica, avisoVersion }),
    );
  }

  miPase(): Promise<PaseMensual> {
    return firstValueFrom(this.http.get<PaseMensual>(`${this.apiUrl}/pases/yo`));
  }

  cancelarRenovacion(paseId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(
        `${this.apiUrl}/pases/${encodeURIComponent(paseId)}/cancelar-renovacion`,
        null,
      ),
    );
  }

  /** Solo prototipo: hace lo que en producción hace el webhook de Mercado Pago. */
  simularPago(pagoId: string, resultado: ResultadoSimulado): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.apiUrl}/dev/pagos/${encodeURIComponent(pagoId)}/simular`, {
        resultado,
      }),
    );
  }

  /** Solo prototipo: simula que se acreditó el pago en OXXO o SPEI de un acceso pendiente. */
  acreditarAcceso(accesoId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(
        `${this.apiUrl}/dev/accesos/${encodeURIComponent(accesoId)}/acreditar`,
        null,
      ),
    );
  }

  /** Solo prototipo: adelanta el vencimiento de un acceso para ver la solo lectura. */
  vencerAcceso(accesoId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(
        `${this.apiUrl}/dev/accesos/${encodeURIComponent(accesoId)}/vencer`,
        null,
      ),
    );
  }
}
