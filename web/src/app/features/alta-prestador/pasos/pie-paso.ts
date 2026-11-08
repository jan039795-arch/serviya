import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Boton } from '../../../shared/ui/boton';
import { AltaEstado } from '../alta-estado';

/** Botones "Anterior" y "Guardar y continuar" de cada paso; va dentro del `<form>`. */
@Component({
  selector: 'app-pie-paso',
  imports: [RouterLink, Boton],
  template: `
    <p class="texto-suave">Tu avance se guarda al continuar; puedes salir y volver después.</p>
    <div class="acciones">
      @if (paso() > 1) {
        <a appBoton variante="secundario" [routerLink]="['/alta', paso() - 1]">Anterior</a>
      }
      <button
        appBoton
        type="submit"
        [disabled]="estado.guardando() || !estado.editable()"
        data-testid="continuar"
      >
        {{ estado.guardando() ? 'Guardando…' : textoContinuar() }}
      </button>
    </div>
  `,
})
export class PiePaso {
  protected readonly estado = inject(AltaEstado);
  readonly paso = input.required<number>();
  readonly textoBoton = input<string>();
  protected readonly textoContinuar = computed(() => this.textoBoton() ?? 'Guardar y continuar');
}
