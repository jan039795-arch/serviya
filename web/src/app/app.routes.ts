import { Routes } from '@angular/router';
import { NoEncontrada, Pendiente, SinPermiso } from './core/layout/paginas';
import { Shell } from './core/layout/shell';
import { conRol, conSesion } from './core/sesion/guards';

/**
 * Rutas de la app. Las pantallas `Pendiente` se reemplazan por las funcionalidades perezosas
 * de `features/` en las tareas T019 a T034.
 */
export const routes: Routes = [
  {
    path: '',
    component: Shell,
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./features/busqueda/busqueda.routes').then((m) => m.rutasBusqueda),
      },
      {
        path: '',
        loadChildren: () =>
          import('./features/contacto/contacto.routes').then((m) => m.rutasContacto),
      },
      {
        path: '',
        loadChildren: () => import('./features/cuenta/cuenta.routes').then((m) => m.rutasCuenta),
      },
      {
        path: 'legal/:documento',
        component: Pendiente,
        data: { titulo: 'Textos legales' },
        title: 'Legal · ServiYa',
      },
      {
        path: 'mensajes',
        canMatch: [conSesion],
        component: Pendiente,
        data: { titulo: 'Mensajes' },
        title: 'Mensajes · ServiYa',
      },
      {
        path: 'cuenta',
        canMatch: [conSesion],
        component: Pendiente,
        data: { titulo: 'Mi cuenta' },
        title: 'Mi cuenta · ServiYa',
      },
      {
        path: 'alta',
        canMatch: [conRol('Prestador')],
        loadChildren: () =>
          import('./features/alta-prestador/alta-prestador.routes').then(
            (m) => m.rutasAltaPrestador,
          ),
      },
      {
        path: 'admin',
        canMatch: [conRol('Admin')],
        loadChildren: () => import('./features/admin/admin.routes').then((m) => m.rutasAdmin),
      },
      { path: 'sin-permiso', component: SinPermiso, title: 'Sin permiso · ServiYa' },
      { path: '**', component: NoEncontrada, title: 'Página no encontrada · ServiYa' },
    ],
  },
];
