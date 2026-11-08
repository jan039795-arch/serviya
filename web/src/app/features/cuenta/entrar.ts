import { Component, computed, inject, input, signal } from '@angular/core';
import { email, form, FormField, required, submit } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { ErrorApi } from '../../core/http/errores.interceptor';
import { rutaDeRegresoSegura } from '../../core/sesion/sesion';
import { mensajeDeError, mensajeGeneral } from '../../shared/formularios/errores';
import { Boton } from '../../shared/ui/boton';
import { Campo, CampoControl } from '../../shared/ui/campo';
import { CuentasApi } from './cuentas-api';

const TIPO_CORREO_SIN_VERIFICAR = '/errores/correo-sin-verificar';

@Component({
  selector: 'app-entrar',
  imports: [FormField, RouterLink, Boton, Campo, CampoControl],
  template: `
    <section class="contenedor">
      <h1>Entra a tu cuenta</h1>
      @if (mensajeEnvio(); as mensaje) {
        <p class="alerta alerta--error" role="alert" data-testid="error-entrar">{{ mensaje }}</p>
        @if (sinVerificar()) {
          <p><a routerLink="/verificar">Verificar mi correo</a></p>
        }
      }

      <form novalidate (submit)="enviar($event)" data-testid="form-entrar">
        <app-campo etiqueta="Correo electrónico" [error]="error(f.email)">
          <input
            appControl
            type="email"
            [formField]="f.email"
            autocomplete="email"
            inputmode="email"
            data-testid="email"
          />
        </app-campo>
        <app-campo etiqueta="Contraseña" [error]="error(f.password)">
          <input
            appControl
            type="password"
            [formField]="f.password"
            autocomplete="current-password"
            data-testid="password"
          />
        </app-campo>
        <button
          appBoton
          anchoCompleto
          type="submit"
          [disabled]="f().submitting()"
          data-testid="entrar"
        >
          {{ f().submitting() ? 'Entrando…' : 'Entrar' }}
        </button>
      </form>

      <p class="pie-formulario"><a routerLink="/recuperar">¿Olvidaste tu contraseña?</a></p>
      <p class="pie-formulario">
        ¿No tienes cuenta?
        <a routerLink="/registro" [queryParams]="{ volver: volver() }">Crea una gratis</a>
      </p>
    </section>
  `,
})
export class Entrar {
  private readonly cuentas = inject(CuentasApi);
  private readonly router = inject(Router);

  /** `?volver=` que dejan los guards; se valida antes de usarlo. */
  readonly volver = input<string>();

  protected readonly modelo = signal({ email: '', password: '' });
  protected readonly f = form(this.modelo, (p) => {
    required(p.email, { message: 'Escribe tu correo.' });
    email(p.email, { message: 'Escribe un correo válido.' });
    required(p.password, { message: 'Escribe tu contraseña.' });
  });
  protected readonly error = mensajeDeError;
  protected readonly errorEnvio = signal<unknown>(null);
  protected readonly mensajeEnvio = computed(() =>
    this.errorEnvio() ? mensajeGeneral(this.errorEnvio()) : null,
  );
  protected readonly sinVerificar = computed(() => {
    const error = this.errorEnvio();
    return error instanceof ErrorApi && error.tipo === TIPO_CORREO_SIN_VERIFICAR;
  });

  protected async enviar(evento: Event): Promise<void> {
    evento.preventDefault();
    this.errorEnvio.set(null);
    await submit(this.f, async () => {
      const { email: correo, password } = this.modelo();
      try {
        await this.cuentas.entrar(correo.trim(), password);
        await this.router.navigateByUrl(rutaDeRegresoSegura(this.volver()));
      } catch (error: unknown) {
        if (error instanceof ErrorApi && error.tipo === TIPO_CORREO_SIN_VERIFICAR) {
          this.cuentas.correoPorVerificar.set(correo.trim());
        }
        this.errorEnvio.set(error);
      }
      return undefined;
    });
  }
}
