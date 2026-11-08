import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { ENTORNO } from '../entorno';
import { AccionesPrototipo, PerfilPrototipo, ResultadoSimulado } from '../prototipo/acciones';
import { borrarTodoLocal } from './almacen-local';

const PASO_SUBIDA_MS = 120;

/** Implementación de las acciones del prototipo; las responde `MockApiInterceptor`. */
@Injectable()
export class AccionesPrototipoHttp implements AccionesPrototipo {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(ENTORNO).apiUrl;

  simularSesion(perfil: PerfilPrototipo): Promise<void> {
    return this.post('/dev/simular-sesion', { perfil });
  }

  /** Vuelve el prototipo a su estado inicial en este navegador. */
  reiniciarDatos(): void {
    borrarTodoLocal();
  }

  simularPago(pagoId: string, resultado: ResultadoSimulado): Promise<void> {
    return this.post(`/dev/pagos/${encodeURIComponent(pagoId)}/simular`, { resultado });
  }

  acreditarAcceso(accesoId: string): Promise<void> {
    return this.post(`/dev/accesos/${encodeURIComponent(accesoId)}/acreditar`, null);
  }

  vencerAcceso(accesoId: string): Promise<void> {
    return this.post(`/dev/accesos/${encodeURIComponent(accesoId)}/vencer`, null);
  }

  /** Avance ficticio de 0 a 100 en lugar del `PUT` a R2. */
  simularSubida(): Observable<number> {
    return new Observable<number>((suscriptor) => {
      let avance = 0;
      const intervalo = setInterval(() => {
        avance = Math.min(avance + 20, 100);
        suscriptor.next(avance);
        if (avance === 100) {
          clearInterval(intervalo);
          suscriptor.complete();
        }
      }, PASO_SUBIDA_MS);
      return () => clearInterval(intervalo);
    });
  }

  private post(ruta: string, cuerpo: unknown): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.apiUrl}${ruta}`, cuerpo));
  }
}
