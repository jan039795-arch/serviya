import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Sesion, SolicitudRegistro } from '../../core/api/modelos';
import { ENTORNO } from '../../core/entorno';
import { SesionService } from '../../core/sesion/sesion';

/** Acceso a `/cuentas/*` (contracts/api.md, sección Cuentas). */
@Injectable({ providedIn: 'root' })
export class CuentasApi {
  private readonly http = inject(HttpClient);
  private readonly entorno = inject(ENTORNO);
  private readonly sesion = inject(SesionService);

  /**
   * Correo que se está verificando. Vive solo en memoria: nunca viaja en la URL (principio I).
   */
  readonly correoPorVerificar = signal('');
  /** Rol elegido al registrarse, para saber a dónde llevar al usuario tras verificar. */
  readonly rolRegistrado = signal<'Cliente' | 'Prestador'>('Cliente');
  /** Ruta a la que se regresa tras verificar el correo (por ejemplo, la página de contactar). */
  readonly volverTrasVerificar = signal<string | null>(null);

  async registrar(solicitud: SolicitudRegistro): Promise<void> {
    await firstValueFrom(this.http.post<void>(this.url('/cuentas/registro'), solicitud));
    this.correoPorVerificar.set(solicitud.email);
    this.rolRegistrado.set(solicitud.rol);
  }

  /** Verifica el correo; la API inicia la sesión al terminar. */
  async verificarCorreo(email: string, codigo: string): Promise<Sesion | null> {
    await firstValueFrom(
      this.http.post<void>(this.url('/cuentas/verificar-correo'), { email, codigo }),
    );
    this.correoPorVerificar.set('');
    return this.sesion.cargar(true);
  }

  reenviarCodigo(email: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(this.url('/cuentas/reenviar-codigo'), { email }));
  }

  async entrar(email: string, password: string): Promise<Sesion> {
    const sesion = await firstValueFrom(
      this.http.post<Sesion>(this.url('/cuentas/login'), { email, password }),
    );
    this.sesion.establecer(sesion);
    return sesion;
  }

  recuperar(email: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(this.url('/cuentas/recuperar'), { email }));
  }

  restablecer(email: string, codigo: string, nuevaPassword: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(this.url('/cuentas/restablecer'), { email, codigo, nuevaPassword }),
    );
  }

  async agregarRolPrestador(): Promise<void> {
    await firstValueFrom(this.http.post<void>(this.url('/cuentas/yo/roles/prestador'), null));
    await this.sesion.cargar(true);
  }

  private url(ruta: string): string {
    return `${this.entorno.apiUrl}${ruta}`;
  }
}
