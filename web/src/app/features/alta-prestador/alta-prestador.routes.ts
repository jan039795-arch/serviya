import { Routes } from '@angular/router';
import { Alta } from './alta';
import { AltaEstado } from './alta-estado';

const titulo = (texto: string): string => `${texto} · Alta · ServiYa`;

/** Alta del prestador en 10 pasos con guardado (T020–T024). */
export const rutasAltaPrestador: Routes = [
  {
    path: '',
    component: Alta,
    providers: [AltaEstado],
    children: [
      { path: '', loadComponent: () => import('./alta-inicio').then((m) => m.AltaInicio) },
      {
        path: 'estado',
        loadComponent: () => import('./estado-perfil').then((m) => m.EstadoPerfil),
        title: titulo('Estado de tu perfil'),
      },
      {
        path: '1',
        loadComponent: () => import('./pasos/datos-personales').then((m) => m.PasoDatosPersonales),
        title: titulo('Datos personales'),
      },
      {
        path: '2',
        loadComponent: () => import('./pasos/oficios').then((m) => m.PasoOficios),
        title: titulo('Oficios'),
      },
      {
        path: '3',
        loadComponent: () => import('./pasos/tarifas').then((m) => m.PasoTarifas),
        title: titulo('Tarifas'),
      },
      {
        path: '4',
        loadComponent: () => import('./pasos/cobertura').then((m) => m.PasoCobertura),
        title: titulo('Cobertura'),
      },
      {
        path: '5',
        loadComponent: () => import('./pasos/disponibilidad').then((m) => m.PasoDisponibilidad),
        title: titulo('Disponibilidad'),
      },
      {
        path: '6',
        loadComponent: () => import('./pasos/descripcion').then((m) => m.PasoDescripcion),
        title: titulo('Descripción'),
      },
      {
        path: '7',
        loadComponent: () => import('./pasos/consentimiento').then((m) => m.PasoConsentimiento),
        title: titulo('Consentimiento'),
      },
      {
        path: '8',
        loadComponent: () => import('./pasos/documentos').then((m) => m.PasoDocumentos),
        title: titulo('Documentos'),
      },
      {
        path: '9',
        loadComponent: () => import('./pasos/video-portafolio').then((m) => m.PasoVideoPortafolio),
        title: titulo('Video y fotos'),
      },
      {
        path: '10',
        loadComponent: () => import('./pasos/revision-envio').then((m) => m.PasoRevisionEnvio),
        title: titulo('Revisión y envío'),
      },
    ],
  },
];
