import { Component, computed, inject, input, OnInit, signal } from '@angular/core';
import {
  email,
  form,
  FormField,
  minLength,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { VERSIONES_LEGALES } from '../../core/legal';
import { calcularEdad, hoyIso } from '../../shared/fechas/edad';
import { errorDelServidor, mensajeDeError, mensajeGeneral } from '../../shared/formularios/errores';
import { Boton } from '../../shared/ui/boton';
import { Campo, CampoControl } from '../../shared/ui/campo';
import { CuentasApi } from './cuentas-api';

interface DatosRegistro {
  email: string;
  password: string;
  nombreCompleto: string;
  fechaNacimiento: string;
  rol: 'Cliente' | 'Prestador';
  acepta: boolean;
}

@Component({
  selector: 'app-registro',
  imports: [FormField, RouterLink, Boton, Campo, CampoControl],
  templateUrl: './registro.html',
})
export class Registro implements OnInit {
  private readonly cuentas = inject(CuentasApi);
  private readonly router = inject(Router);

  /** `?rol=prestador` llega desde "Quiero ofrecer mis servicios". */
  readonly rol = input<string>();

  protected readonly modelo = signal<DatosRegistro>({
    email: '',
    password: '',
    nombreCompleto: '',
    fechaNacimiento: '',
    rol: 'Cliente',
    acepta: false,
  });

  protected readonly f = form(this.modelo, (p) => {
    required(p.email, { message: 'Escribe tu correo.' });
    email(p.email, { message: 'Escribe un correo válido, como nombre@ejemplo.com.' });
    required(p.password, { message: 'Crea una contraseña.' });
    minLength(p.password, 8, { message: 'Usa al menos 8 caracteres.' });
    required(p.nombreCompleto, { message: 'Escribe tu nombre completo.' });
    minLength(p.nombreCompleto, 3, { message: 'Escribe tu nombre completo.' });
    maxLength(p.nombreCompleto, 120, { message: 'Máximo 120 caracteres.' });
    required(p.fechaNacimiento, { message: 'Escribe tu fecha de nacimiento.' });
    validate(p.fechaNacimiento, ({ value }) =>
      value() && calcularEdad(value()) === null
        ? { kind: 'fecha', message: 'Escribe una fecha válida.' }
        : undefined,
    );
    validate(p.acepta, ({ value }) =>
      value()
        ? undefined
        : { kind: 'acepta', message: 'Para crear tu cuenta, acepta el aviso y los términos.' },
    );
  });

  protected readonly errorEnvio = signal<unknown>(null);
  protected readonly hoy = hoyIso();
  protected readonly error = mensajeDeError;
  protected readonly menorDeEdad = computed(() => {
    const edad = calcularEdad(this.modelo().fechaNacimiento);
    return edad !== null && edad < 18;
  });
  protected readonly errorCorreo = computed(
    () => this.error(this.f.email) ?? errorDelServidor(this.errorEnvio(), 'email'),
  );
  protected readonly mensajeEnvio = computed(() =>
    this.errorEnvio() ? mensajeGeneral(this.errorEnvio()) : null,
  );

  ngOnInit(): void {
    if (this.rol() === 'prestador') {
      this.modelo.update((m) => ({ ...m, rol: 'Prestador' }));
    }
  }

  protected async enviar(evento: Event): Promise<void> {
    evento.preventDefault();
    this.errorEnvio.set(null);
    await submit(this.f, async () => {
      const datos = this.modelo();
      try {
        await this.cuentas.registrar({
          email: datos.email.trim(),
          password: datos.password,
          nombreCompleto: datos.nombreCompleto.trim(),
          fechaNacimiento: datos.fechaNacimiento,
          rol: datos.rol,
          aceptaciones: [
            { documento: 'AvisoPrivacidad', version: VERSIONES_LEGALES.AvisoPrivacidad },
            { documento: 'Terminos', version: VERSIONES_LEGALES.Terminos },
          ],
        });
        await this.router.navigateByUrl('/verificar');
      } catch (error: unknown) {
        this.errorEnvio.set(error);
      }
      return undefined;
    });
  }
}
