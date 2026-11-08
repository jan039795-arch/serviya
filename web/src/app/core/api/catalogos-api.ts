import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ENTORNO } from '../entorno';
import { Municipio, Oficio } from './modelos';

/**
 * Catálogos de oficios y municipios. Casi no cambian, así que se piden una vez por visita;
 * si la petición falla, se vuelve a intentar en la siguiente llamada.
 */
@Injectable({ providedIn: 'root' })
export class CatalogosApi {
  private readonly http = inject(HttpClient);
  private readonly entorno = inject(ENTORNO);
  private oficiosEnCache: Promise<readonly Oficio[]> | null = null;
  private municipiosEnCache: Promise<readonly Municipio[]> | null = null;

  oficios(): Promise<readonly Oficio[]> {
    this.oficiosEnCache ??= this.pedir<Oficio[]>('/catalogos/oficios').catch((error: unknown) => {
      this.oficiosEnCache = null;
      throw error;
    });
    return this.oficiosEnCache;
  }

  municipios(): Promise<readonly Municipio[]> {
    this.municipiosEnCache ??= this.pedir<Municipio[]>('/catalogos/municipios').catch(
      (error: unknown) => {
        this.municipiosEnCache = null;
        throw error;
      },
    );
    return this.municipiosEnCache;
  }

  private pedir<T>(ruta: string): Promise<T> {
    return firstValueFrom(this.http.get<T>(`${this.entorno.apiUrl}${ruta}`));
  }
}
