import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ENTORNO } from '../entorno';

export type PerfilPrototipo = 'visitante' | 'cliente' | 'prestador' | 'admin';

/** Acciones que existen solo en el prototipo (las responde `MockApiInterceptor`). */
@Injectable({ providedIn: 'root' })
export class PrototipoService {
  private readonly http = inject(HttpClient);
  private readonly entorno = inject(ENTORNO);

  simularSesion(perfil: PerfilPrototipo): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.entorno.apiUrl}/dev/simular-sesion`, { perfil }),
    );
  }
}
