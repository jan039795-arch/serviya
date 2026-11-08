import { InjectionToken } from '@angular/core';
import { environment } from '../../environments/environment';
import { Entorno } from '../../environments/entorno';

/** Configuración del entorno, inyectable para poder sustituirla en las pruebas. */
export const ENTORNO = new InjectionToken<Entorno>('ENTORNO', {
  providedIn: 'root',
  factory: () => environment,
});

/** `true` si al menos un área de la API se responde con datos ficticios. */
export function hayAreasSimuladas(entorno: Entorno): boolean {
  return Object.values(entorno.mocks).some(Boolean);
}
