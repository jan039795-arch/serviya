import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SesionService } from '../../core/sesion/sesion';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { Avisos } from '../../shared/ui/avisos';
import { Boton } from '../../shared/ui/boton';
import { CuentasApi } from './cuentas-api';

/** Mi cuenta (T034): accesos a contactos, pase, perfil de prestador, administración y salida. */
@Component({
  selector: 'app-mi-cuenta',
  imports: [RouterLink, Boton],
  template: `
    <section class="contenedor">
      @if (sesion.usuario(); as usuario) {
        <h1>Mi cuenta</h1>
        <p class="texto-suave" data-testid="datos-cuenta">
          {{ usuario.nombreCompleto }} · {{ usuario.email }}
        </p>

        @if (error(); as mensaje) {
          <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
        }

        <nav aria-label="Mi cuenta">
          <ul class="menu">
            <li><a routerLink="/mis-contactos" data-testid="ir-mis-contactos">Mis contactos</a></li>
            <li><a routerLink="/pase" data-testid="ir-pase">Mi pase mensual</a></li>
            @if (sesion.esPrestador()) {
              <li>
                <a routerLink="/alta" data-testid="ir-perfil-prestador">Mi perfil de prestador</a>
              </li>
            }
            @if (sesion.esAdmin()) {
              <li><a routerLink="/admin" data-testid="ir-admin">Administración</a></li>
            }
            <li><a routerLink="/legal/aviso-privacidad">Privacidad y mis datos</a></li>
          </ul>
        </nav>

        @if (!sesion.esPrestador()) {
          <aside class="invitacion">
            <h2>¿Ofreces un oficio?</h2>
            <p>Usa esta misma cuenta para crear tu perfil de prestador. Es gratis.</p>
            <button
              appBoton
              variante="secundario"
              type="button"
              (click)="serPrestador()"
              [disabled]="ocupado()"
              data-testid="ser-prestador"
            >
              Quiero ofrecer mis servicios
            </button>
          </aside>
        }

        <button
          appBoton
          variante="texto"
          type="button"
          (click)="salir()"
          [disabled]="ocupado()"
          data-testid="cerrar-sesion"
        >
          Cerrar sesión
        </button>
      }
    </section>
  `,
  styles: `
    .menu {
      margin: 0 0 var(--espacio-6);
      padding: 0;
      list-style: none;
      border-top: 1px solid #e3e7eb;
    }
    .menu a {
      display: flex;
      align-items: center;
      justify-content: space-between;
      min-height: 3.25rem;
      border-bottom: 1px solid #e3e7eb;
      color: var(--color-texto);
      text-decoration: none;
    }
    .menu a::after {
      content: '›';
      color: var(--color-texto-suave);
      font-size: 1.5rem;
    }
    .invitacion {
      margin-bottom: var(--espacio-6);
      padding: var(--espacio-4);
      border-radius: var(--radio);
      background: var(--color-superficie);
    }
  `,
})
export class MiCuenta {
  protected readonly sesion = inject(SesionService);
  private readonly cuentas = inject(CuentasApi);
  private readonly router = inject(Router);
  private readonly avisos = inject(Avisos);

  protected readonly ocupado = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async serPrestador(): Promise<void> {
    await this.ejecutar(async () => {
      await this.cuentas.agregarRolPrestador();
      await this.router.navigateByUrl('/alta');
    });
  }

  protected async salir(): Promise<void> {
    await this.ejecutar(async () => {
      await this.sesion.cerrar();
      this.avisos.mostrar('info', 'Cerraste tu sesión.');
      await this.router.navigateByUrl('/');
    });
  }

  private async ejecutar(accion: () => Promise<void>): Promise<void> {
    this.error.set(null);
    this.ocupado.set(true);
    try {
      await accion();
    } catch (error: unknown) {
      this.error.set(mensajeGeneral(error));
    } finally {
      this.ocupado.set(false);
    }
  }
}
