export const EDAD_MINIMA = 18;

/** Edad cumplida en años a partir de una fecha `AAAA-MM-DD`; `null` si la fecha no es válida. */
export function calcularEdad(fechaNacimiento: string, hoy: Date = new Date()): number | null {
  const coincidencia = /^(\d{4})-(\d{2})-(\d{2})$/.exec(fechaNacimiento);
  if (!coincidencia) {
    return null;
  }
  const [anio, mes, dia] = coincidencia.slice(1).map(Number);
  const fecha = new Date(anio, mes - 1, dia);
  if (fecha.getFullYear() !== anio || fecha.getMonth() !== mes - 1 || fecha.getDate() !== dia) {
    return null;
  }
  let edad = hoy.getFullYear() - anio;
  const yaCumplio =
    hoy.getMonth() > mes - 1 || (hoy.getMonth() === mes - 1 && hoy.getDate() >= dia);
  if (!yaCumplio) {
    edad -= 1;
  }
  return edad;
}

/** La validación definitiva está en el servidor (FR-004); esto solo orienta al usuario. */
export function esMayorDeEdad(fechaNacimiento: string, hoy: Date = new Date()): boolean {
  const edad = calcularEdad(fechaNacimiento, hoy);
  return edad !== null && edad >= EDAD_MINIMA;
}

/** Fecha local de hoy en formato `AAAA-MM-DD`, para el atributo `max` de los campos de fecha. */
export function hoyIso(hoy: Date = new Date()): string {
  const mes = String(hoy.getMonth() + 1).padStart(2, '0');
  const dia = String(hoy.getDate()).padStart(2, '0');
  return `${hoy.getFullYear()}-${mes}-${dia}`;
}

/** Días completos entre una fecha `AAAA-MM-DD` y hoy (negativo si es futura). */
export function diasDesde(fechaIso: string, hoy: Date = new Date()): number | null {
  const coincidencia = /^(\d{4})-(\d{2})-(\d{2})$/.exec(fechaIso);
  if (!coincidencia) {
    return null;
  }
  const [anio, mes, dia] = coincidencia.slice(1).map(Number);
  const inicio = Date.UTC(anio, mes - 1, dia);
  const fin = Date.UTC(hoy.getFullYear(), hoy.getMonth(), hoy.getDate());
  return Math.floor((fin - inicio) / 86_400_000);
}
