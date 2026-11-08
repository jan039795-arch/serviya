import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SesionService } from '../sesion/sesion';
import { ACCIONES_PROTOTIPO, PerfilPrototipo } from '../prototipo/acciones';
import { Avisos } from '../../shared/ui/avisos';
import { mensajeGeneral } from '../../shared/formularios/errores';

/**
 * Solo en el prototipo: permite ver la app como visitante, cliente, prestador o administrador
 * sin registrarse. Se muestra únicamente cuando hay áreas simuladas.
 */
@Component({
  selector: 'app-selector-rol',
  template: `
    <div class="barra-prototipo">
      <label class="selector" data-testid="selector-rol" [attr.data-perfil]="perfil()">
        <span>Prototipo (usa solo datos ficticios) · ver como</span>
        <select [value]="perfil()" (change)="cambiar($event)" [disabled]="cambiando()">
          <option value="visitante">Visitante</option>
          <option value="cliente">Cliente</option>
          <option value="prestador">Prestador</option>
          <option value="admin">Administración</option>
        </select>
      </label>
      <button
        type="button"
        class="reiniciar"
        (click)="reiniciar()"
        data-testid="reiniciar-prototipo"
      >
        Reiniciar datos
      </button>
    </div>
  `,
  styles: `
    .barra-prototipo {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: center;
      gap: var(--espacio-1) var(--espacio-3);
      padding: var(--espacio-1) var(--espacio-4);
      background: #fff4d6;
      color: var(--color-texto);
      font-size: var(--texto-chico);
    }
    .selector {
      display: flex;
      align-items: center;
      gap: var(--espacio-2);
    }
    .reiniciar {
      min-height: var(--tactil-min);
      border: 0;
      background: transparent;
      color: var(--color-primario-oscuro);
      text-decoration: underline;
      cursor: pointer;
    }
    select {
      width: auto;
      min-height: var(--tactil-min);
      padding: 0 var(--espacio-2);
    }
  `,
})
export class SelectorRol {
  private readonly prototipo = inject(ACCIONES_PROTOTIPO);
  private readonly avisos = inject(Avisos);
  private readonly sesion = inject(SesionService);
  private readonly router = inject(Router);

  protected readonly cambiando = signal(false);
  protected readonly perfil = computed<PerfilPrototipo>(() => {
    if (this.sesion.esAdmin()) return 'admin';
    if (this.sesion.esPrestador()) return 'prestador';
    return this.sesion.usuario() ? 'cliente' : 'visitante';
  });

  /** Borra perfiles, pagos y sesión simulados de este navegador y recarga desde el inicio. */
  protected reiniciar(): void {
    if (!globalThis.confirm('¿Borrar todos los datos del prototipo en este navegador?')) {
      return;
    }
    this.prototipo.reiniciarDatos();
    globalThis.location.assign('/');
  }

  protected async cambiar(evento: Event): Promise<void> {
    const perfil = (evento.target as HTMLSelectElement).value as PerfilPrototipo;
    this.cambiando.set(true);
    try {
      await this.prototipo.simularSesion(perfil);
      await this.sesion.cargar(true);
      await this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' });
    } catch (error: unknown) {
      this.avisos.mostrar('error', mensajeGeneral(error));
    } finally {
      this.cambiando.set(false);
    }
  }
}
