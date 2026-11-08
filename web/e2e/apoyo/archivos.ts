/**
 * Imagen PNG ficticia de 1×1 px para las subidas de prueba. Nunca se usan documentos reales
 * (constitución, principio VI).
 */
export const PNG_FICTICIO = {
  name: 'documento-ficticio.png',
  mimeType: 'image/png',
  buffer: Buffer.from(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
    'base64',
  ),
};

/** Fecha `AAAA-MM-DD` de hace `dias` días. */
export function haceDias(dias: number): string {
  const fecha = new Date(Date.now() - dias * 86_400_000);
  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  const dia = String(fecha.getDate()).padStart(2, '0');
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}
