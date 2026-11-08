import { PromediosPorEje } from '../core/api/modelos';

/** Promedio general de los cuatro ejes (calidad, precio justo, puntualidad y trato); 0 si no hay. */
export function promedioGeneral(promedios: PromediosPorEje): number {
  const { calidad, precioJusto, puntualidad, trato } = promedios;
  return (calidad + precioJusto + puntualidad + trato) / 4;
}
