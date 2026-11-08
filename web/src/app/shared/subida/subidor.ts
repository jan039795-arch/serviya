import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

/** Prefijo de las URL de subida que devuelve el prototipo en lugar de una URL firmada de R2. */
export const PREFIJO_SUBIDA_SIMULADA = 'simulado:';
const PASO_SIMULADO_MS = 120;

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
  subir(url: string, archivo: Blob, tipoContenido: string): Observable<number> {
    return url.startsWith(PREFIJO_SUBIDA_SIMULADA)
      ? this.simular()
      : this.subirReal(url, archivo, tipoContenido);
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

  private simular(): Observable<number> {
    return new Observable<number>((suscriptor) => {
      let avance = 0;
      const intervalo = setInterval(() => {
        avance = Math.min(avance + 20, 100);
        suscriptor.next(avance);
        if (avance === 100) {
          clearInterval(intervalo);
          suscriptor.complete();
        }
      }, PASO_SIMULADO_MS);
      return () => clearInterval(intervalo);
    });
  }
}
