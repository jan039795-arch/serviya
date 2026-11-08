import { Routes } from '@angular/router';

/** Administración (rol `Admin`; la ruta padre tiene el guard `canMatch`). */
export const rutasAdmin: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'revisiones' },
  {
    path: 'revisiones',
    loadComponent: () => import('./revisiones/cola').then((m) => m.ColaRevisiones),
    title: 'Perfiles por revisar · ServiYa',
  },
  {
    path: 'revisiones/:prestadorId',
    loadComponent: () => import('./revisiones/detalle-revision').then((m) => m.DetalleRevision),
    title: 'Revisión de perfil · ServiYa',
  },
];
