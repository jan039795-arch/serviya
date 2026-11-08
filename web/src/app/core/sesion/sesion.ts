import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Rol, Sesion } from '../api/modelos';
import { ENTORNO } from '../entorno';
import { ErrorApi } from '../http/errores.interceptor';

interface EstadoSesion {
  readonly cargada: boolean;
  readonly usuario: Sesion | null;
}

/**
 * Sesión del usuario. La cookie HttpOnly la maneja el navegador (research R2); aquí solo se
 * guarda lo que devuelve `GET /cuentas/yo`.
 */
@Injectable({ providedIn: 'root' })
export class SesionService {
  private readonly http = inject(HttpClient);
  private readonly entorno = inject(ENTORNO);
  private readonly estado = signal<EstadoSesion>({ cargada: false, usuario: null });
  private carga: Promise<Sesion | null> | null = null;

  readonly cargada = computed(() => this.estado().cargada);
  readonly usuario = computed(() => this.estado().usuario);
  readonly roles = computed<readonly Rol[]>(() => this.usuario()?.roles ?? []);
  readonly esCliente = computed(() => this.roles().includes('Cliente'));
  readonly esPrestador = computed(() => this.roles().includes('Prestador'));
  readonly esAdmin = computed(() => this.roles().includes('Admin'));

  /** Carga la sesión una sola vez; con `forzar` vuelve a pedirla (por ejemplo, tras entrar). */
  cargar(forzar = false): Promise<Sesion | null> {
    if (this.carga && !forzar) {
      return this.carga;
    }
    this.carga = firstValueFrom(this.http.get<Sesion>(`${this.entorno.apiUrl}/cuentas/yo`)).then(
      (usuario) => this.establecer(usuario),
      (error: unknown) => {
        if (error instanceof ErrorApi && error.status === 401) {
          return this.establecer(null);
        }
        this.carga = null;
        throw error;
      },
    );
    return this.carga;
  }

  async cerrar(): Promise<void> {
    await firstValueFrom(this.http.post<void>(`${this.entorno.apiUrl}/cuentas/logout`, null));
    this.establecer(null);
  }

  establecer(usuario: Sesion | null): Sesion | null {
    this.estado.set({ cargada: true, usuario });
    this.carga = Promise.resolve(usuario);
    return usuario;
  }

  tieneRol(rol: Rol): boolean {
    return this.roles().includes(rol);
  }
}

/**
 * Valida la ruta de regreso que llega en `?volver=` para evitar redirecciones abiertas:
 * solo se aceptan rutas internas que empiezan con una sola `/`.
 */
export function rutaDeRegresoSegura(valor: string | null | undefined, porDefecto = '/'): string {
  if (!valor || !valor.startsWith('/') || valor.startsWith('//') || valor.includes('\\')) {
    return porDefecto;
  }
  return valor;
}
