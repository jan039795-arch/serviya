const formato = new Intl.RelativeTimeFormat('es-MX', { numeric: 'auto' });

/** "hace 3 horas", "ayer", "hace 2 días"… a partir de una fecha ISO. */
export function haceCuanto(fechaIso: string, ahora: Date = new Date()): string {
  const segundos = Math.round((new Date(fechaIso).getTime() - ahora.getTime()) / 1000);
  const absolutos = Math.abs(segundos);
  if (absolutos < 60) {
    return 'hace un momento';
  }
  if (absolutos < 3600) {
    return formato.format(Math.round(segundos / 60), 'minute');
  }
  if (absolutos < 86_400) {
    return formato.format(Math.round(segundos / 3600), 'hour');
  }
  return formato.format(Math.round(segundos / 86_400), 'day');
}
