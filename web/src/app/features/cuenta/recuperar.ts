import { Component, computed, inject, signal } from '@angular/core';
import {
  email,
  form,
  FormField,
  maxLength,
  minLength,
  pattern,
  required,
  submit,
} from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { ENTORNO, hayAreasSimuladas } from '../../core/entorno';
import { mensajeDeError, mensajeGeneral } from '../../shared/formularios/errores';
import { Boton } from '../../shared/ui/boton';
import { Campo, CampoControl } from '../../shared/ui/campo';
import { CuentasApi } from './cuentas-api';

type Etapa = 'correo' | 'codigo' | 'listo';

@Component({
  selector: 'app-recuperar',
  imports: [FormField, RouterLink, Boton, Campo, CampoControl],
  template: `
    <section class="contenedor">
      <h1>Recupera tu contraseña</h1>
      @if (mensajeEnvio(); as mensaje) {
        <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
      }

      @switch (etapa()) {
        @case ('correo') {
          <p>Escribe tu correo. Si tiene una cuenta, te enviaremos un código.</p>
          <form novalidate (submit)="pedirCodigo($event)">
            <app-campo etiqueta="Correo electrónico" [error]="error(f.email)">
              <input appControl type="email" [formField]="f.email" autocomplete="email" />
            </app-campo>
            <button appBoton anchoCompleto type="submit" [disabled]="enviando()">
              Enviar código
            </button>
          </form>
        }
        @case ('codigo') {
          <p>Si el correo tiene una cuenta, te llegó un código de 6 dígitos.</p>
          @if (prototipo) {
            <p class="alerta alerta--info">Prototipo: usa el código <strong>123456</strong>.</p>
          }
          <form novalidate (submit)="restablecer($event)">
            <app-campo etiqueta="Código" [error]="error(f.codigo)">
              <input
                appControl
                [formField]="f.codigo"
                inputmode="numeric"
                autocomplete="one-time-code"
              />
            </app-campo>
            <app-campo
              etiqueta="Nueva contraseña"
              ayuda="Al menos 8 caracteres."
              [error]="error(f.nuevaPassword)"
            >
              <input
                appControl
                type="password"
                [formField]="f.nuevaPassword"
                autocomplete="new-password"
              />
            </app-campo>
            <button appBoton anchoCompleto type="submit" [disabled]="enviando()">
              Cambiar contraseña
            </button>
          </form>
        }
        @case ('listo') {
          <p class="alerta alerta--exito" role="status">Tu contraseña cambió. Ya puedes entrar.</p>
          <a appBoton anchoCompleto routerLink="/entrar">Entrar</a>
        }
      }
    </section>
  `,
})
export class Recuperar {
  private readonly cuentas = inject(CuentasApi);

  protected readonly prototipo = hayAreasSimuladas(inject(ENTORNO));
  protected readonly etapa = signal<Etapa>('correo');
  protected readonly enviando = signal(false);
  protected readonly modelo = signal({ email: '', codigo: '', nuevaPassword: '' });
  protected readonly f = form(this.modelo, (p) => {
    required(p.email, { message: 'Escribe tu correo.' });
    email(p.email, { message: 'Escribe un correo válido.' });
    required(p.codigo, {
      message: 'Escribe el código.',
      when: () => this.etapa() === 'codigo',
    });
    pattern(p.codigo, /^\d{6}$/, {
      message: 'El código tiene 6 dígitos.',
      when: () => this.etapa() === 'codigo',
    });
    maxLength(p.codigo, 6);
    minLength(p.nuevaPassword, 8, {
      message: 'Usa al menos 8 caracteres.',
      when: () => this.etapa() === 'codigo',
    });
  });
  protected readonly error = mensajeDeError;
  protected readonly errorEnvio = signal<unknown>(null);
  protected readonly mensajeEnvio = computed(() =>
    this.errorEnvio() ? mensajeGeneral(this.errorEnvio()) : null,
  );

  protected pedirCodigo(evento: Event): Promise<void> {
    evento.preventDefault();
    return this.ejecutar(async () => {
      await this.cuentas.recuperar(this.modelo().email.trim());
      this.etapa.set('codigo');
    });
  }

  protected restablecer(evento: Event): Promise<void> {
    evento.preventDefault();
    return this.ejecutar(async () => {
      const { email: correo, codigo, nuevaPassword } = this.modelo();
      await this.cuentas.restablecer(correo.trim(), codigo, nuevaPassword);
      this.etapa.set('listo');
    });
  }

  private async ejecutar(accion: () => Promise<void>): Promise<void> {
    this.errorEnvio.set(null);
    await submit(this.f, async () => {
      this.enviando.set(true);
      try {
        await accion();
      } catch (error: unknown) {
        this.errorEnvio.set(error);
      } finally {
        this.enviando.set(false);
      }
      return undefined;
    });
  }
}
