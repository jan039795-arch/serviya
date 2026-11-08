import { Component, computed, inject, resource, signal } from '@angular/core';
import { form, FormField, required, submit } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { CatalogosApi } from '../../core/api/catalogos-api';
import { mensajeDeError, mensajeGeneral } from '../../shared/formularios/errores';
import { Boton } from '../../shared/ui/boton';
import { valorDe } from '../../shared/recursos';
import { BusquedaApi } from './busqueda-api';

const OFICIOS_FRECUENTES = [
  'fontanero',
  'electricista',
  'albanil',
  'limpieza-del-hogar',
  'pintor',
  'cerrajero',
];

/** Inicio: buscar un oficio en un municipio (US2; T026). */
@Component({
  selector: 'app-inicio',
  imports: [FormField, RouterLink, Boton],
  template: `
    <section class="portada">
      <div class="contenedor">
        <h1>Encuentra a alguien de confianza para tu trabajo</h1>
        <p>Prestadores de oficios con identidad verificada. Tú eliges y acuerdas el precio.</p>
      </div>
    </section>

    <section class="contenedor">
      <form novalidate (submit)="buscar($event)" class="buscador" data-testid="form-busqueda">
        <label for="oficio">¿Qué necesitas?</label>
        <select id="oficio" [formField]="f.oficio" data-testid="oficio">
          <option value="">Elige un oficio</option>
          @for (oficio of oficios.hasValue() ? oficios.value() : []; track oficio.id) {
            <option [value]="oficio.slug">{{ oficio.nombre }}</option>
          }
        </select>
        @if (error(f.oficio); as mensaje) {
          <p class="texto-error">{{ mensaje }}</p>
        }

        <label for="municipio">¿Dónde?</label>
        <select id="municipio" [formField]="f.municipio" data-testid="municipio">
          <option value="">Cualquier municipio</option>
          @for (municipio of municipios.hasValue() ? municipios.value() : []; track municipio.id) {
            <option [value]="municipio.id">{{ municipio.nombre }}</option>
          }
        </select>
        <button
          appBoton
          variante="texto"
          type="button"
          (click)="usarUbicacion()"
          [disabled]="ubicando()"
          data-testid="usar-ubicacion"
        >
          {{ ubicando() ? 'Buscando tu municipio…' : 'Usar mi ubicación' }}
        </button>
        @if (mensajeUbicacion(); as mensaje) {
          <p class="texto-suave" role="status">{{ mensaje }}</p>
        }

        <button appBoton anchoCompleto type="submit" data-testid="buscar">Buscar</button>
      </form>

      <h2>Oficios más buscados</h2>
      <ul class="frecuentes">
        @for (oficio of frecuentes(); track oficio.slug) {
          <li>
            <a [routerLink]="['/buscar']" [queryParams]="{ oficio: oficio.slug }">{{
              oficio.nombre
            }}</a>
          </li>
        }
      </ul>

      <aside class="invitacion">
        <h2>¿Ofreces un oficio?</h2>
        <p>Crea tu perfil gratis. Los clientes te contactan y el pago del trabajo es todo tuyo.</p>
        <a
          appBoton
          variante="secundario"
          routerLink="/registro"
          [queryParams]="{ rol: 'prestador' }"
        >
          Quiero ofrecer mis servicios
        </a>
      </aside>
    </section>
  `,
  styles: `
    .portada {
      background: var(--color-primario);
      color: var(--color-sobre-primario);
    }
    .portada h1 {
      margin-bottom: var(--espacio-2);
    }
    .portada p {
      margin: 0;
    }
    .buscador {
      display: grid;
      gap: var(--espacio-2);
      margin: var(--espacio-4) 0 var(--espacio-6);
    }
    .buscador label {
      font-weight: 600;
    }
    .buscador button[type='button'] {
      justify-self: start;
    }
    .frecuentes {
      display: flex;
      flex-wrap: wrap;
      gap: var(--espacio-2);
      margin: 0 0 var(--espacio-6);
      padding: 0;
      list-style: none;
    }
    .frecuentes a {
      display: inline-flex;
      align-items: center;
      min-height: var(--tactil-min);
      padding: 0 var(--espacio-4);
      border: 1px solid var(--color-borde);
      border-radius: 999px;
      color: var(--color-texto);
      text-decoration: none;
    }
    .invitacion {
      padding: var(--espacio-4);
      border-radius: var(--radio);
      background: var(--color-superficie);
    }
  `,
})
export class Inicio {
  private readonly catalogos = inject(CatalogosApi);
  private readonly api = inject(BusquedaApi);
  private readonly router = inject(Router);

  protected readonly oficios = resource({ loader: () => this.catalogos.oficios() });
  protected readonly municipios = resource({ loader: () => this.catalogos.municipios() });
  protected readonly frecuentes = computed(() =>
    (valorDe(this.oficios) ?? []).filter((o) => OFICIOS_FRECUENTES.includes(o.slug)),
  );

  protected readonly modelo = signal({ oficio: '', municipio: '' });
  protected readonly f = form(this.modelo, (p) => {
    required(p.oficio, { message: 'Elige qué oficio necesitas.' });
  });
  protected readonly error = mensajeDeError;
  protected readonly ubicando = signal(false);
  protected readonly mensajeUbicacion = signal<string | null>(null);

  protected async buscar(evento: Event): Promise<void> {
    evento.preventDefault();
    await submit(this.f, async () => {
      const { oficio, municipio } = this.modelo();
      await this.router.navigate(['/buscar'], {
        queryParams: { oficio, municipio: municipio || null },
      });
      return undefined;
    });
  }

  protected usarUbicacion(): void {
    this.mensajeUbicacion.set(null);
    if (!('geolocation' in navigator)) {
      this.mensajeUbicacion.set('Tu navegador no comparte la ubicación. Elige tu municipio.');
      return;
    }
    this.ubicando.set(true);
    navigator.geolocation.getCurrentPosition(
      async ({ coords }) => {
        try {
          const municipio = await this.api.ubicar(coords.latitude, coords.longitude);
          this.modelo.update((m) => ({ ...m, municipio: municipio.id }));
          this.mensajeUbicacion.set(`Te ubicamos en ${municipio.nombre}.`);
        } catch (error: unknown) {
          this.mensajeUbicacion.set(mensajeGeneral(error));
        } finally {
          this.ubicando.set(false);
        }
      },
      () => {
        this.ubicando.set(false);
        this.mensajeUbicacion.set('No pudimos usar tu ubicación. Elige tu municipio de la lista.');
      },
      { maximumAge: 600_000, timeout: 10_000 },
    );
  }
}
