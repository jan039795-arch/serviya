import { HttpInterceptorFn } from '@angular/common/http';
import { EnvironmentProviders, Provider } from '@angular/core';
import { Routes } from '@angular/router';
import { mockApiInterceptor } from '../mocks/mock-api.interceptor';
import { AccionesPrototipoHttp } from '../mocks/prototipo';
import { conSesion } from '../sesion/guards';
import { ACCIONES_PROTOTIPO } from './acciones';

/**
 * Piezas del prototipo (Etapa 2): simulación de la API, acciones de prueba, checkout simulado y la
 * barra "Prototipo · ver como". En producción `angular.json` reemplaza este archivo por
 * `prototipo.prod.ts`, así que nada de esto (ni los datos ficticios) llega al paquete.
 */
export const interceptoresPrototipo: readonly HttpInterceptorFn[] = [mockApiInterceptor];

export function proveedoresPrototipo(): (Provider | EnvironmentProviders)[] {
  return [{ provide: ACCIONES_PROTOTIPO, useClass: AccionesPrototipoHttp }];
}

export const rutasPrototipo: Routes = [
  {
    path: 'pago/simulado',
    canMatch: [conSesion],
    loadComponent: () =>
      import('../../features/contacto/checkout-simulado').then((m) => m.CheckoutSimulado),
    title: 'Pago · ServiYa',
  },
];

export { SelectorRol as BarraPrototipo } from '../mocks/selector-rol';
