import { Routes } from '@angular/router';

/** Búsqueda y perfiles públicos (US2; T026–T028). Rutas públicas, sin sesión. */
export const rutasBusqueda: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./inicio').then((m) => m.Inicio),
    title: 'ServiYa · Prestadores de oficios verificados',
  },
  {
    path: 'buscar',
    loadComponent: () => import('./resultados').then((m) => m.Resultados),
    title: 'Resultados · ServiYa',
  },
  {
    path: 'prestador/:id',
    loadComponent: () => import('./perfil-publico').then((m) => m.PerfilPublico),
    title: 'Perfil · ServiYa',
  },
];
