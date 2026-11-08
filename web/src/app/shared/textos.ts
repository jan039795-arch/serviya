import { UnidadTarifa } from '../core/api/modelos';

const UNIDADES: Readonly<Record<UnidadTarifa, string>> = {
  Hora: 'hora',
  Dia: 'día',
  Servicio: 'servicio',
};

/** "hora", "día" o "servicio", para "Desde $350 por servicio". */
export function textoUnidad(unidad: UnidadTarifa): string {
  return UNIDADES[unidad];
}

/** "1 reseña" / "23 reseñas". */
export function textoResenas(total: number): string {
  return total === 1 ? '1 reseña' : `${total} reseñas`;
}

/** Días en el orden de la semana mexicana (lunes primero); `dia` sigue `Date.getDay()`. */
export const DIAS_SEMANA: readonly { readonly dia: number; readonly nombre: string }[] = [
  { dia: 1, nombre: 'Lunes' },
  { dia: 2, nombre: 'Martes' },
  { dia: 3, nombre: 'Miércoles' },
  { dia: 4, nombre: 'Jueves' },
  { dia: 5, nombre: 'Viernes' },
  { dia: 6, nombre: 'Sábado' },
  { dia: 0, nombre: 'Domingo' },
];
