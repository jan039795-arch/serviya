import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { ACCIONES_PROTOTIPO, PREFIJO_SUBIDA_SIMULADA } from '../../core/prototipo/acciones';

/**
 * Sube un archivo con `PUT` directo a la URL firmada de R2 (research R3) y emite el avance de
 * 0 a 100.
 *
 * Usa `XMLHttpRequest` y no `HttpClient` a propósito: con `withFetch()` no hay eventos de avance
 * de subida, y la URL firmada es de otro dominio, así que no debe recibir la cookie de sesión ni el
 * encabezado XSRF que agregan los interceptores.
 */
@Injectable({ providedIn: 'root' })
export class Subidor {
  /** Solo en el prototipo; en producción es `null` y una URL simulada se rechaza. */
  private readonly prototipo = inject(ACCIONES_PROTOTIPO, { optional: true });

  subir(url: string, archivo: Blob, tipoContenido: string): Observable<number> {
    if (url.startsWith(PREFIJO_SUBIDA_SIMULADA) && this.prototipo) {
      return this.prototipo.simularSubida();
    }
    // Los documentos (INE, selfie, comprobante) solo viajan cifrados. El dominio exacto de R2 se
    // valida al conectar la API real (T085), cuando exista en la configuración.
    if (!esUrlSegura(url)) {
      return throwError(() => new Error('No pudimos preparar la subida. Inténtalo de nuevo.'));
    }
    return this.subirReal(url, archivo, tipoContenido);
  }

  private subirReal(url: string, archivo: Blob, tipoContenido: string): Observable<number> {
    return new Observable<number>((suscriptor) => {
      const xhr = new XMLHttpRequest();
      xhr.open('PUT', url);
      xhr.setRequestHeader('Content-Type', tipoContenido);
      xhr.upload.onprogress = (evento) => {
        if (evento.lengthComputable) {
          suscriptor.next(Math.round((evento.loaded / evento.total) * 100));
        }
      };
      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          suscriptor.next(100);
          suscriptor.complete();
        } else {
          suscriptor.error(new Error('No se pudo subir el archivo. Inténtalo de nuevo.'));
        }
      };
      xhr.onerror = () =>
        suscriptor.error(new Error('La subida se interrumpió. Revisa tu conexión y reintenta.'));
      xhr.send(archivo);
      return () => {
        if (xhr.readyState !== XMLHttpRequest.DONE) {
          xhr.abort();
        }
      };
    });
  }
}

export function esUrlSegura(valor: string): boolean {
  try {
    return new URL(valor).protocol === 'https:';
  } catch {
    return false;
  }
}
