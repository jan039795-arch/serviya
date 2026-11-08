import { Component, computed, inject, signal } from '@angular/core';
import {
  form,
  FormField,
  maxLength,
  minLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { SesionService } from '../../../core/sesion/sesion';
import { calcularEdad, hoyIso } from '../../../shared/fechas/edad';
import { mensajeDeError } from '../../../shared/formularios/errores';
import { nombrePublico } from '../../../shared/nombre-publico';
import { Campo, CampoControl } from '../../../shared/ui/campo';
import { AltaEstado } from '../alta-estado';
import { PiePaso } from './pie-paso';

const PREFIJO_MX = '+52';

/** Quita espacios y guiones, y el `+52` si lo trae: deja los 10 dígitos. */
export function digitosTelefono(valor: string): string {
  const digitos = valor.replace(/\D/g, '');
  return digitos.length === 12 && digitos.startsWith('52') ? digitos.slice(2) : digitos;
}

/** Paso 1 (FR-005). Teléfono y dirección son privados; la colonia se muestra en público. */
@Component({
  selector: 'app-paso-datos-personales',
  imports: [FormField, Campo, CampoControl, PiePaso],
  template: `
    <h1>Tus datos personales</h1>
    <p>Tu teléfono y tu dirección son privados: los clientes solo ven tu teléfono si pagan.</p>

    <form novalidate (submit)="continuar($event)">
      <app-campo etiqueta="Nombre completo" [error]="error(f.nombreCompleto)">
        <input appControl [formField]="f.nombreCompleto" autocomplete="name" data-testid="nombre" />
      </app-campo>
      @if (vistaNombre()) {
        <p class="alerta alerta--info">
          Así te verán los clientes: <strong>{{ vistaNombre() }}</strong>
        </p>
      }

      <app-campo etiqueta="Fecha de nacimiento" [error]="error(f.fechaNacimiento)">
        <input
          appControl
          type="date"
          [max]="hoy"
          [formField]="f.fechaNacimiento"
          autocomplete="bday"
          data-testid="fecha-nacimiento"
        />
      </app-campo>
      @if (menorDeEdad()) {
        <p class="alerta alerta--aviso" data-testid="aviso-menor">
          Necesitas 18 años o más para publicar tu perfil.
        </p>
      }

      <app-campo
        etiqueta="Teléfono celular"
        ayuda="10 dígitos. Lo verán solo los clientes que paguen tu contacto."
        [error]="error(f.telefono)"
      >
        <input
          appControl
          type="tel"
          inputmode="tel"
          [formField]="f.telefono"
          autocomplete="tel-national"
          data-testid="telefono"
        />
      </app-campo>

      <app-campo
        etiqueta="Dirección"
        ayuda="Calle, número y colonia. Es privada: nunca se muestra."
        [error]="error(f.direccion)"
      >
        <input
          appControl
          [formField]="f.direccion"
          autocomplete="street-address"
          data-testid="direccion"
        />
      </app-campo>

      <app-campo
        etiqueta="Colonia donde trabajas"
        ayuda="Esta sí aparece en tu perfil."
        [error]="error(f.coloniaBase)"
      >
        <input appControl [formField]="f.coloniaBase" data-testid="colonia" />
      </app-campo>

      <app-pie-paso [paso]="1" />
    </form>
  `,
})
export class PasoDatosPersonales {
  private readonly estado = inject(AltaEstado);
  private readonly sesion = inject(SesionService);

  private readonly guardados = this.estado.perfil()?.datosPersonales;
  protected readonly modelo = signal({
    nombreCompleto: this.guardados?.nombreCompleto ?? this.sesion.usuario()?.nombreCompleto ?? '',
    fechaNacimiento: this.guardados?.fechaNacimiento ?? '',
    telefono: digitosTelefono(this.guardados?.telefono ?? ''),
    direccion: this.guardados?.direccion ?? '',
    coloniaBase: this.guardados?.coloniaBase ?? '',
  });

  protected readonly f = form(this.modelo, (p) => {
    required(p.nombreCompleto, { message: 'Escribe tu nombre completo.' });
    minLength(p.nombreCompleto, 3, { message: 'Escribe tu nombre completo.' });
    maxLength(p.nombreCompleto, 120, { message: 'Máximo 120 caracteres.' });
    required(p.fechaNacimiento, { message: 'Escribe tu fecha de nacimiento.' });
    validate(p.fechaNacimiento, ({ value }) =>
      value() && calcularEdad(value()) === null
        ? { kind: 'fecha', message: 'Escribe una fecha válida.' }
        : undefined,
    );
    required(p.telefono, { message: 'Escribe tu teléfono.' });
    validate(p.telefono, ({ value }) =>
      value() && digitosTelefono(value()).length !== 10
        ? { kind: 'telefono', message: 'El teléfono debe tener 10 dígitos.' }
        : undefined,
    );
    required(p.direccion, { message: 'Escribe tu dirección.' });
    maxLength(p.direccion, 200, { message: 'Máximo 200 caracteres.' });
    required(p.coloniaBase, { message: 'Escribe tu colonia.' });
    maxLength(p.coloniaBase, 80, { message: 'Máximo 80 caracteres.' });
  });

  protected readonly error = mensajeDeError;
  protected readonly hoy = hoyIso();
  protected readonly vistaNombre = computed(() => nombrePublico(this.modelo().nombreCompleto));
  protected readonly menorDeEdad = computed(() => {
    const edad = calcularEdad(this.modelo().fechaNacimiento);
    return edad !== null && edad < 18;
  });

  protected async continuar(evento: Event): Promise<void> {
    evento.preventDefault();
    await submit(this.f, async () => {
      const datos = this.modelo();
      await this.estado.guardarYAvanzar({
        paso: 1,
        datosPersonales: {
          nombreCompleto: datos.nombreCompleto.trim(),
          fechaNacimiento: datos.fechaNacimiento,
          telefono: `${PREFIJO_MX}${digitosTelefono(datos.telefono)}`,
          direccion: datos.direccion.trim(),
          coloniaBase: datos.coloniaBase.trim(),
        },
      });
      return undefined;
    });
  }
}
