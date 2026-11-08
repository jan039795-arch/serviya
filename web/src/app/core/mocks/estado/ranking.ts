import { PromediosPorEje } from '../../api/modelos';
export { promedioGeneral } from '../../../shared/calificaciones';

export type Promedios = PromediosPorEje;

/**
 * Orden de resultados con promedio bayesiano (research R5): `(C·m + Σ calificaciones) / (C + n)`.
 * Con pocas reseñas el puntaje se acerca al promedio global `m`, así una sola reseña de 5 no
 * supera a muchas reseñas altas (US6, escenario 5). La API real lo calcula en T091.
 */
export const PESO_PREVIO = 5;

export function puntajeBayesiano(
  promedio: number,
  totalResenas: number,
  promedioGlobal: number,
  pesoPrevio = PESO_PREVIO,
): number {
  return (pesoPrevio * promedioGlobal + promedio * totalResenas) / (pesoPrevio + totalResenas);
}
