import { Routes } from '@angular/router';

/** Pantallas públicas de cuenta (T019). */
export const rutasCuenta: Routes = [
  {
    path: 'registro',
    loadComponent: () => import('./registro').then((m) => m.Registro),
    title: 'Crear cuenta · ServiYa',
  },
  {
    path: 'verificar',
    loadComponent: () => import('./verificar-codigo').then((m) => m.VerificarCodigo),
    title: 'Verificar correo · ServiYa',
  },
  {
    path: 'entrar',
    loadComponent: () => import('./entrar').then((m) => m.Entrar),
    title: 'Entrar · ServiYa',
  },
  {
    path: 'recuperar',
    loadComponent: () => import('./recuperar').then((m) => m.Recuperar),
    title: 'Recuperar contraseña · ServiYa',
  },
];
