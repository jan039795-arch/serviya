import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { RegionAvisos } from '../../shared/ui/avisos';
import { ENTORNO, hayAreasSimuladas } from '../entorno';
import { BarraPrototipo } from '../prototipo/prototipo';
import { SesionService } from '../sesion/sesion';

interface EnlaceNavegacion {
  readonly ruta: string;
  readonly texto: string;
  readonly icono: 'buscar' | 'mensajes' | 'cuenta';
  readonly exacto: boolean;
}

/** Armazón de la app: barra superior, contenido y navegación inferior para el pulgar. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, RegionAvisos, BarraPrototipo],
  template: `
    <a class="saltar" href="#contenido">Saltar al contenido</a>
    @if (prototipo) {
      <app-selector-rol />
    }
    <header class="barra">
      <a routerLink="/" class="marca" data-testid="titulo-app">ServiYa</a>
      @if (sesion.esAdmin()) {
        <a routerLink="/admin" class="barra__enlace">Administración</a>
      }
    </header>

    <main id="contenido" tabindex="-1">
      <router-outlet />
    </main>

    <nav class="navegacion" aria-label="Principal">
      @for (enlace of enlaces; track enlace.ruta) {
        <a
          [routerLink]="enlace.ruta"
          routerLinkActive="activo"
          [routerLinkActiveOptions]="{ exact: enlace.exacto }"
          ariaCurrentWhenActive="page"
          [attr.data-testid]="'nav-' + enlace.icono"
        >
          <svg aria-hidden="true" viewBox="0 0 24 24" width="24" height="24">
            @switch (enlace.icono) {
              @case ('buscar') {
                <circle cx="11" cy="11" r="7" />
                <path d="m20 20-4-4" />
              }
              @case ('mensajes') {
                <path d="M4 5h16v11H8l-4 4z" />
              }
              @case ('cuenta') {
                <circle cx="12" cy="8" r="4" />
                <path d="M4 21c0-4 4-6 8-6s8 2 8 6" />
              }
            }
          </svg>
          <span>{{ enlace.texto }}</span>
        </a>
      }
    </nav>
    <app-region-avisos />
  `,
  styles: `
    :host {
      --alto-navegacion: 64px;
      display: flex;
      flex-direction: column;
      min-height: 100dvh;
    }
    .saltar {
      position: absolute;
      left: var(--espacio-2);
      top: -100px;
      z-index: 30;
      padding: var(--espacio-2) var(--espacio-4);
      background: var(--color-fondo);
    }
    .saltar:focus {
      top: var(--espacio-2);
    }
    .barra {
      position: sticky;
      top: 0;
      z-index: 10;
      display: flex;
      align-items: center;
      justify-content: space-between;
      min-height: 56px;
      padding: 0 var(--espacio-4);
      background: var(--color-primario);
      color: var(--color-sobre-primario);
    }
    .marca,
    .barra__enlace {
      display: inline-flex;
      align-items: center;
      min-height: var(--tactil-min);
      color: inherit;
      text-decoration: none;
    }
    .marca {
      font-size: var(--texto-grande);
      font-weight: 700;
    }
    main {
      flex: 1;
      padding-bottom: calc(var(--alto-navegacion) + env(safe-area-inset-bottom));
    }
    main:focus {
      outline: none;
    }
    .navegacion {
      position: fixed;
      inset: auto 0 0;
      z-index: 10;
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      height: calc(var(--alto-navegacion) + env(safe-area-inset-bottom));
      padding-bottom: env(safe-area-inset-bottom);
      border-top: 1px solid var(--color-borde);
      background: var(--color-fondo);
    }
    .navegacion a {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 2px;
      color: var(--color-texto-suave);
      font-size: var(--texto-chico);
      text-decoration: none;
    }
    .navegacion a.activo {
      color: var(--color-primario);
      font-weight: 600;
    }
    .navegacion svg {
      fill: none;
      stroke: currentColor;
      stroke-width: 2;
      stroke-linecap: round;
      stroke-linejoin: round;
    }
  `,
})
export class Shell implements OnInit {
  protected readonly sesion = inject(SesionService);
  protected readonly prototipo = hayAreasSimuladas(inject(ENTORNO));
  protected readonly enlaces: readonly EnlaceNavegacion[] = [
    { ruta: '/', texto: 'Buscar', icono: 'buscar', exacto: true },
    { ruta: '/mensajes', texto: 'Mensajes', icono: 'mensajes', exacto: false },
    { ruta: '/cuenta', texto: 'Mi cuenta', icono: 'cuenta', exacto: false },
  ];

  ngOnInit(): void {
    // Sin bloquear el arranque: los guards esperan esta misma carga si la necesitan.
    this.sesion.cargar().catch(() => undefined);
  }
}
