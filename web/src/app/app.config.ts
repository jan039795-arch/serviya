import { registerLocaleData } from '@angular/common';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
  withNoXsrfProtection,
} from '@angular/common/http';
import localeEsMx from '@angular/common/locales/es-MX';
import {
  ApplicationConfig,
  isDevMode,
  LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideClientHydration } from '@angular/platform-browser';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import { routes } from './app.routes';
import { erroresInterceptor } from './core/http/errores.interceptor';
import { pedirTokenXsrfAlArrancar, xsrfInterceptor } from './core/http/xsrf.interceptor';
import { interceptoresPrototipo, proveedoresPrototipo } from './core/prototipo/prototipo';

registerLocaleData(localeEsMx);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled' }),
    ),
    // El de errores va primero para que también traduzca los errores simulados. El XSRF de Angular
    // se apaga: omite las URL absolutas (la API está en api.serviya.mx) y lo reemplaza xsrfInterceptor.
    provideHttpClient(
      withFetch(),
      withNoXsrfProtection(),
      withInterceptors([erroresInterceptor, xsrfInterceptor, ...interceptoresPrototipo]),
    ),
    provideAppInitializer(pedirTokenXsrfAlArrancar),
    ...proveedoresPrototipo(),
    provideClientHydration(),
    { provide: LOCALE_ID, useValue: 'es-MX' },
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
