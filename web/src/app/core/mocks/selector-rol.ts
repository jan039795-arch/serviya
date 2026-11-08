import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SesionService } from '../sesion/sesion';
import { PerfilPrototipo, PrototipoService } from './prototipo';

/**
 * Solo en el prototipo: permite ver la app como visitante, cliente, prestador o administrador
 * sin registrarse. Se muestra únicamente cuando hay áreas simuladas.
 */
@Component({
  selector: 'app-selector-rol',
  template: `
    <label class="selector" data-testid="selector-rol" [attr.data-perfil]="perfil()">
      <span>Prototipo · ver como</span>
      <select [value]="perfil()" (change)="cambiar($event)" [disabled]="cambiando()">
        <option value="visitante">Visitante</option>
        <option value="cliente">Cliente</option>
        <option value="prestador">Prestador</option>
        <option value="admin">Administración</option>
      </select>
    </label>
  `,
  styles: `
    .selector {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--espacio-2);
      padding: var(--espacio-1) var(--espacio-4);
      background: #fff4d6;
      color: var(--color-texto);
      font-size: var(--texto-chico);
    }
    select {
      width: auto;
      min-height: var(--tactil-min);
      padding: 0 var(--espacio-2);
    }
  `,
})
export class SelectorRol {
  private readonly prototipo = inject(PrototipoService);
  private readonly sesion = inject(SesionService);
  private readonly router = inject(Router);

  protected readonly cambiando = signal(false);
  protected readonly perfil = computed<PerfilPrototipo>(() => {
    if (this.sesion.esAdmin()) return 'admin';
    if (this.sesion.esPrestador()) return 'prestador';
    return this.sesion.usuario() ? 'cliente' : 'visitante';
  });

  protected async cambiar(evento: Event): Promise<void> {
    const perfil = (evento.target as HTMLSelectElement).value as PerfilPrototipo;
    this.cambiando.set(true);
    try {
      await this.prototipo.simularSesion(perfil);
      await this.sesion.cargar(true);
      await this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' });
    } finally {
      this.cambiando.set(false);
    }
  }
}
