import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, submit, validate } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { VERSIONES_LEGALES } from '../../../core/legal';
import { mensajeDeError, mensajeGeneral } from '../../../shared/formularios/errores';
import { AltaApi } from '../alta-api';
import { AltaEstado } from '../alta-estado';
import { PiePaso } from './pie-paso';

/**
 * Paso 7: consentimiento expreso para datos sensibles y biométricos antes de subir documentos
 * (FR-007, LFPDPPP).
 */
@Component({
  selector: 'app-paso-consentimiento',
  imports: [FormField, RouterLink, PiePaso],
  template: `
    <h1>Tus documentos y tu privacidad</h1>
    <p>
      En el siguiente paso subirás tu INE, una selfie y tu comprobante de domicilio. Antes, debes
      saber:
    </p>
    <ul class="puntos">
      <li>Solo los usa el equipo de ServiYa para verificar tu identidad.</li>
      <li>Se guardan en un almacenamiento privado. Nunca los ven clientes ni otros prestadores.</li>
      <li>Cada vez que alguien del equipo abre uno, queda registrado.</li>
      <li>Si eliminas tu cuenta, se borran.</li>
    </ul>
    <p>
      La selfie y tu INE son datos sensibles y biométricos. Más detalles en el
      <a routerLink="/legal/aviso-privacidad">aviso de privacidad</a>.
    </p>

    @if (yaAceptado()) {
      <p class="alerta alerta--exito" data-testid="consentimiento-aceptado">
        Ya diste tu consentimiento.
      </p>
      <form novalidate (submit)="continuarSinCambios($event)">
        <app-pie-paso [paso]="7" />
      </form>
    } @else {
      @if (errorEnvio(); as mensaje) {
        <p class="alerta alerta--error" role="alert">{{ mensaje }}</p>
      }
      <form novalidate (submit)="aceptar($event)">
        <label class="opcion">
          <input type="checkbox" [formField]="f.acepta" data-testid="acepta-consentimiento" />
          Doy mi consentimiento expreso para que ServiYa trate mis datos sensibles y biométricos con
          el fin de verificar mi identidad.
        </label>
        @if (error(f.acepta); as mensaje) {
          <p class="texto-error">{{ mensaje }}</p>
        }
        <app-pie-paso [paso]="7" textoBoton="Acepto y continúo" />
      </form>
    }
  `,
  styles: `
    .puntos {
      padding-left: var(--espacio-6);
    }
    .puntos li {
      margin-bottom: var(--espacio-2);
    }
  `,
})
export class PasoConsentimiento {
  private readonly estado = inject(AltaEstado);
  private readonly api = inject(AltaApi);

  protected readonly yaAceptado = computed(
    () => !!this.estado.perfil()?.consentimientoSensibleVersion,
  );
  protected readonly modelo = signal({ acepta: false });
  protected readonly f = form(this.modelo, (p) => {
    validate(p.acepta, ({ value }) =>
      value() ? undefined : { kind: 'acepta', message: 'Para continuar, da tu consentimiento.' },
    );
  });
  protected readonly error = mensajeDeError;
  protected readonly errorEnvio = signal<string | null>(null);

  protected async aceptar(evento: Event): Promise<void> {
    evento.preventDefault();
    this.errorEnvio.set(null);
    await submit(this.f, async () => {
      const version = VERSIONES_LEGALES.DatosSensibles;
      try {
        await this.api.aceptarConsentimiento(version);
      } catch (error: unknown) {
        this.errorEnvio.set(mensajeGeneral(error));
        return undefined;
      }
      this.estado.actualizar((perfil) => ({ ...perfil, consentimientoSensibleVersion: version }));
      await this.estado.guardarYAvanzar({ paso: 7 });
      return undefined;
    });
  }

  protected async continuarSinCambios(evento: Event): Promise<void> {
    evento.preventDefault();
    await this.estado.guardarYAvanzar({ paso: 7 });
  }
}
