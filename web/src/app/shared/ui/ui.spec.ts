import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Campo, CampoControl } from './campo';
import { Estrellas } from './estrellas';

@Component({
  imports: [Campo, CampoControl],
  template: `
    <app-campo etiqueta="Teléfono" ayuda="10 dígitos" [error]="error()">
      <input appControl type="tel" />
    </app-campo>
  `,
})
class CampoDePrueba {
  readonly error = signal<string | null>(null);
}

describe('Campo', () => {
  it('liga la etiqueta, la ayuda y el error con el control', async () => {
    const fixture = TestBed.createComponent(CampoDePrueba);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;
    const control = elemento.querySelector('input') as HTMLInputElement;
    const etiqueta = elemento.querySelector('label') as HTMLLabelElement;

    expect(etiqueta.htmlFor).toBe(control.id);
    expect(control.getAttribute('aria-describedby')).toMatch(/-ayuda$/);
    expect(control.hasAttribute('aria-invalid')).toBe(false);

    fixture.componentInstance.error.set('Escribe 10 dígitos.');
    await fixture.whenStable();

    expect(control.getAttribute('aria-invalid')).toBe('true');
    const ids = control.getAttribute('aria-describedby')?.split(' ') ?? [];
    expect(ids).toHaveLength(2);
    expect(elemento.querySelector(`#${ids[1]}`)?.textContent).toContain('Escribe 10 dígitos.');
  });
});

describe('Estrellas', () => {
  it('anuncia la calificación con un decimal', async () => {
    const fixture = TestBed.createComponent(Estrellas);
    fixture.componentRef.setInput('valor', 4.46);
    await fixture.whenStable();

    const estrellas = (fixture.nativeElement as HTMLElement).querySelector('[role="img"]');
    expect(estrellas?.getAttribute('aria-label')).toBe('4.5 de 5');
  });

  it('sin calificación muestra un texto en lugar de estrellas', async () => {
    const fixture = TestBed.createComponent(Estrellas);
    fixture.componentRef.setInput('valor', 0);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Sin calificaciones');
  });
});
