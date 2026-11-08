import { Pipe, PipeTransform } from '@angular/core';

const formato = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/** Convierte centavos a pesos: `3900` → "$39.00 MXN". Los montos de la API vienen en centavos. */
export function formatearMxn(centavos: number | null | undefined): string {
  if (centavos === null || centavos === undefined || !Number.isFinite(centavos)) {
    return '';
  }
  return `${formato.format(centavos / 100)} MXN`;
}

@Pipe({ name: 'mxn' })
export class MxnPipe implements PipeTransform {
  transform(centavos: number | null | undefined): string {
    return formatearMxn(centavos);
  }
}
