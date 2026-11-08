import { Routes } from '@angular/router';
import { conSesion } from '../../core/sesion/guards';

/**
 * Contacto, pagos y pase (US3 y US4; T030–T033). Todo pide sesión: un visitante que pulsa
 * "Contactar" va a entrar o registrarse y luego regresa aquí (US3, escenario 1).
 */
export const rutasContacto: Routes = [
  {
    path: 'contactar/:prestadorId',
    canMatch: [conSesion],
    loadComponent: () => import('./opciones').then((m) => m.OpcionesContacto),
    title: 'Contactar · ServiYa',
  },
  {
    path: 'pago/simulado',
    canMatch: [conSesion],
    loadComponent: () => import('./checkout-simulado').then((m) => m.CheckoutSimulado),
    title: 'Pago · ServiYa',
  },
  {
    path: 'pago/resultado',
    canMatch: [conSesion],
    loadComponent: () => import('./resultado-pago').then((m) => m.ResultadoPago),
    title: 'Resultado del pago · ServiYa',
  },
  {
    path: 'mis-contactos',
    canMatch: [conSesion],
    loadComponent: () => import('./mis-accesos').then((m) => m.MisAccesos),
    title: 'Mis contactos · ServiYa',
  },
  {
    path: 'pase',
    canMatch: [conSesion],
    loadComponent: () => import('./pase').then((m) => m.Pase),
    title: 'Mi pase · ServiYa',
  },
];
