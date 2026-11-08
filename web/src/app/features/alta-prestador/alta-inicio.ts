import { Component, effect, inject } from '@angular/core';
import { Cargando } from '../../shared/ui/cargando';
import { AltaEstado } from './alta-estado';

/**
 * `/alta` sin paso: lleva al prestador a donde se quedó (US1, escenario 2) o al estado de su
 * perfil si ya lo envió.
 */
@Component({
  selector: 'app-alta-inicio',
  imports: [Cargando],
  template: `<app-cargando texto="Abriendo tu alta…" />`,
})
export class AltaInicio {
  private readonly estado = inject(AltaEstado);

  constructor() {
    effect(() => {
      const perfil = this.estado.perfil();
      if (!perfil) {
        return;
      }
      if (perfil.estado === 'Borrador') {
        void this.estado.irAPaso(this.estado.pasoParaRetomar());
      } else {
        void this.estado.irAEstado();
      }
    });
  }
}
