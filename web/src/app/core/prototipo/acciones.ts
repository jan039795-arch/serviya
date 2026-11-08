import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

/** Prefijo de las URL de subida que devuelve el prototipo en lugar de una URL firmada de R2. */
export const PREFIJO_SUBIDA_SIMULADA = 'simulado:';

export type PerfilPrototipo = 'visitante' | 'cliente' | 'prestador' | 'admin';
export type ResultadoSimulado = 'aprobado' | 'pendiente' | 'rechazado' | 'acreditar';

/**
 * Acciones que solo existen en el prototipo (Etapa 2): simulan lo que en producción hacen el
 * webhook de Mercado Pago, el paso del tiempo o R2. Solo se proveen en `prototipo.ts`; en
 * producción `prototipo.prod.ts` no las registra, así que `inject(..., { optional: true })`
 * devuelve `null` y su código no llega al paquete.
 */
export interface AccionesPrototipo {
  simularSesion(perfil: PerfilPrototipo): Promise<void>;
  reiniciarDatos(): void;
  simularPago(pagoId: string, resultado: ResultadoSimulado): Promise<void>;
  acreditarAcceso(accesoId: string): Promise<void>;
  vencerAcceso(accesoId: string): Promise<void>;
  simularSubida(): Observable<number>;
}

export const ACCIONES_PROTOTIPO = new InjectionToken<AccionesPrototipo>('ACCIONES_PROTOTIPO');
