import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';

/** Dominios a los que la app puede mandar a pagar (Checkout Pro de Mercado Pago). */
const DOMINIOS_PERMITIDOS = ['mercadopago.com.mx', 'mercadopago.com'];

/**
 * Lleva al usuario a pagar. Solo acepta la ruta interna del checkout simulado o una URL `https`
 * de Mercado Pago: así una respuesta alterada no puede mandarlo a otro sitio.
 */
@Injectable({ providedIn: 'root' })
export class Checkout {
  private readonly router = inject(Router);

  async ir(checkoutUrl: string): Promise<void> {
    if (checkoutUrl.startsWith('/pago/')) {
      await this.router.navigateByUrl(checkoutUrl);
      return;
    }
    if (!esUrlDePago(checkoutUrl)) {
      throw new Error('No pudimos abrir la página de pago. Inténtalo de nuevo.');
    }
    globalThis.location.assign(checkoutUrl);
  }
}

export function esUrlDePago(valor: string): boolean {
  try {
    const url = new URL(valor);
    return (
      url.protocol === 'https:' &&
      DOMINIOS_PERMITIDOS.some((d) => url.hostname === d || url.hostname.endsWith(`.${d}`))
    );
  } catch {
    return false;
  }
}
