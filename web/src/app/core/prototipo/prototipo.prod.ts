import { HttpInterceptorFn } from '@angular/common/http';
import { Component, EnvironmentProviders, Provider } from '@angular/core';
import { Routes } from '@angular/router';

/**
 * Reemplazo de `prototipo.ts` en el build de producción (`fileReplacements` en `angular.json`):
 * sin simulación de la API, sin acciones de prueba, sin checkout simulado y sin barra de prototipo.
 */
export const interceptoresPrototipo: readonly HttpInterceptorFn[] = [];

export function proveedoresPrototipo(): (Provider | EnvironmentProviders)[] {
  return [];
}

export const rutasPrototipo: Routes = [];

@Component({ selector: 'app-selector-rol', template: '' })
export class BarraPrototipo {}
