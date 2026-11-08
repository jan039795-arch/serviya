import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  Insignia,
  Municipio,
  Pagina,
  PerfilPublico,
  ResenaPublica,
  TarjetaPrestador,
} from '../../core/api/modelos';
import { ENTORNO } from '../../core/entorno';

export interface FiltrosBusqueda {
  readonly oficio?: string;
  readonly municipio?: string;
  readonly calificacionMin?: number;
  readonly insignia?: Insignia;
  /** Centavos. */
  readonly tarifaMax?: number;
  readonly tamano?: number;
}

/** Búsqueda y perfiles públicos (contracts/api.md). Nunca reciben datos de contacto. */
@Injectable({ providedIn: 'root' })
export class BusquedaApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(ENTORNO).apiUrl;

  buscar(filtros: FiltrosBusqueda): Promise<Pagina<TarjetaPrestador>> {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtros)) {
      if (valor !== undefined && valor !== null && valor !== '') {
        params = params.set(clave, String(valor));
      }
    }
    return firstValueFrom(
      this.http.get<Pagina<TarjetaPrestador>>(`${this.apiUrl}/prestadores`, { params }),
    );
  }

  perfil(id: string): Promise<PerfilPublico> {
    return firstValueFrom(
      this.http.get<PerfilPublico>(`${this.apiUrl}/prestadores/${encodeURIComponent(id)}`),
    );
  }

  resenas(id: string, tamano: number): Promise<Pagina<ResenaPublica>> {
    return firstValueFrom(
      this.http.get<Pagina<ResenaPublica>>(
        `${this.apiUrl}/prestadores/${encodeURIComponent(id)}/resenas`,
        { params: { pagina: 1, tamano } },
      ),
    );
  }

  /** La ubicación va en el cuerpo y no en la URL: es un dato personal (principio I). */
  ubicar(lat: number, lng: number): Promise<Municipio> {
    return firstValueFrom(
      this.http.post<Municipio>(`${this.apiUrl}/catalogos/municipios/ubicar`, { lat, lng }),
    );
  }
}
