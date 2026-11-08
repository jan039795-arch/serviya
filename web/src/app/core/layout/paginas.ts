import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';

/** Marcador de una pantalla que todavía no se construye (solo mientras dura el prototipo). */
@Component({
  selector: 'app-pendiente',
  imports: [RouterLink],
  template: `
    <section class="contenedor">
      <h1>{{ titulo() }}</h1>
      <p>Esta pantalla se agrega en una tarea siguiente del prototipo.</p>
      <a routerLink="/">Volver al inicio</a>
    </section>
  `,
})
export class Pendiente {
  protected readonly titulo = toSignal(
    inject(ActivatedRoute).data.pipe(map((datos) => (datos['titulo'] as string) ?? 'ServiYa')),
    { initialValue: 'ServiYa' },
  );
}

@Component({
  selector: 'app-sin-permiso',
  imports: [RouterLink],
  template: `
    <section class="contenedor" data-testid="sin-permiso">
      <h1>No tienes permiso</h1>
      <p>Tu cuenta no puede ver esta sección.</p>
      <a routerLink="/">Volver al inicio</a>
    </section>
  `,
})
export class SinPermiso {}

@Component({
  selector: 'app-no-encontrada',
  imports: [RouterLink],
  template: `
    <section class="contenedor">
      <h1>No encontramos esta página</h1>
      <p>Puede que el enlace esté mal escrito o que la página ya no exista.</p>
      <a routerLink="/">Volver al inicio</a>
    </section>
  `,
})
export class NoEncontrada {}
