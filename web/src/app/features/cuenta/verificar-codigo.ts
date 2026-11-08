import { Component, computed, inject, signal } from '@angular/core';
import {
  email,
  form,
  FormField,
  maxLength,
  pattern,
  required,
  submit,
} from '@angular/forms/signals';
import { Router } from '@angular/router';
import { ENTORNO, hayAreasSimuladas } from '../../core/entorno';
import { rutaDeRegresoSegura } from '../../core/sesion/sesion';
import { mensajeDeError, mensajeGeneral } from '../../shared/formularios/errores';
import { Avisos } from '../../shared/ui/avisos';
import { Boton } from '../../shared/ui/boton';
import { Campo, CampoControl } from '../../shared/ui/campo';
import { CuentasApi } from './cuentas-api';

@Component({
  selector: 'app-verificar-codigo',
  imports: [FormField, Boton, Campo, CampoControl],
  template: `
    <section class="contenedor">
      <h1>Verifica tu correo</h1>
      <p>Te enviamos un código de 6 dígitos. Vence en 10 minutos.</p>
      @if (prototipo) {
        <p class="alerta alerta--info">Prototipo: usa el código <strong>123456</strong>.</p>
      }
      @if (mensajeEnvio(); as mensaje) {
        <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
      }

      <form novalidate (submit)="enviar($event)">
        <app-campo etiqueta="Correo electrónico" [error]="error(f.email)">
          <input appControl type="email" [formField]="f.email" autocomplete="email" />
        </app-campo>
        <app-campo etiqueta="Código" [error]="error(f.codigo)">
          <input
            appControl
            [formField]="f.codigo"
            inputmode="numeric"
            autocomplete="one-time-code"
            data-testid="codigo"
          />
        </app-campo>
        <button
          appBoton
          anchoCompleto
          type="submit"
          [disabled]="f().submitting()"
          data-testid="verificar"
        >
          {{ f().submitting() ? 'Verificando…' : 'Verificar' }}
        </button>
      </form>

      <div class="acciones">
        <button
          appBoton
          variante="texto"
          type="button"
          (click)="reenviar()"
          [disabled]="reenviando()"
        >
          Enviar un código nuevo
        </button>
      </div>
    </section>
  `,
})
export class VerificarCodigo {
  private readonly cuentas = inject(CuentasApi);
  private readonly router = inject(Router);
  private readonly avisos = inject(Avisos);

  protected readonly prototipo = hayAreasSimuladas(inject(ENTORNO));
  protected readonly modelo = signal({ email: this.cuentas.correoPorVerificar(), codigo: '' });
  protected readonly f = form(this.modelo, (p) => {
    required(p.email, { message: 'Escribe el correo con el que te registraste.' });
    email(p.email, { message: 'Escribe un correo válido.' });
    required(p.codigo, { message: 'Escribe el código.' });
    pattern(p.codigo, /^\d{6}$/, { message: 'El código tiene 6 dígitos.' });
    maxLength(p.codigo, 6);
  });
  protected readonly error = mensajeDeError;
  protected readonly errorEnvio = signal<unknown>(null);
  protected readonly mensajeEnvio = computed(() =>
    this.errorEnvio() ? mensajeGeneral(this.errorEnvio()) : null,
  );
  protected readonly reenviando = signal(false);

  protected async enviar(evento: Event): Promise<void> {
    evento.preventDefault();
    this.errorEnvio.set(null);
    await submit(this.f, async () => {
      try {
        const { email: correo, codigo } = this.modelo();
        const sesion = await this.cuentas.verificarCorreo(correo.trim(), codigo);
        const esPrestador = sesion?.roles.includes('Prestador') ?? false;
        this.avisos.mostrar('exito', '¡Listo! Tu correo quedó verificado.');
        const volver = rutaDeRegresoSegura(this.cuentas.volverTrasVerificar());
        await this.router.navigateByUrl(esPrestador ? '/alta' : volver);
      } catch (error: unknown) {
        this.errorEnvio.set(error);
      }
      return undefined;
    });
  }

  protected async reenviar(): Promise<void> {
    const correo = this.modelo().email.trim();
    if (!correo) {
      this.f.email().markAsTouched();
      return;
    }
    this.reenviando.set(true);
    try {
      await this.cuentas.reenviarCodigo(correo);
      this.avisos.mostrar('info', 'Te enviamos un código nuevo.');
    } catch (error: unknown) {
      this.errorEnvio.set(error);
    } finally {
      this.reenviando.set(false);
    }
  }
}
