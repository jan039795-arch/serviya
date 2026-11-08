import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * Prototipo (Etapa 2): todo se renderiza en el navegador.
 * T094 cambia `/prestador/:id` y `/buscar` a `RenderMode.Server`.
 */
export const serverRoutes: ServerRoute[] = [
  {
    path: '**',
    renderMode: RenderMode.Client,
  },
];
