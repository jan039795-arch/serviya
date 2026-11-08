import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('muestra el nombre de la plataforma y la navegación principal', async () => {
    const harness = await RouterTestingHarness.create('/');
    const pagina = harness.fixture.nativeElement as HTMLElement;

    expect(pagina.querySelector('[data-testid="titulo-app"]')?.textContent).toBe('ServiYa');
    expect(pagina.querySelectorAll('nav[aria-label="Principal"] a')).toHaveLength(3);
  });
});
