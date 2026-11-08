/**
 * Preparación de archivos en el navegador antes de subirlos (research R4).
 * Las fotos se vuelven a codificar siempre: así se reduce su peso y se eliminan los metadatos
 * EXIF, como la ubicación GPS (principio I).
 */

export const LADO_MAXIMO_PX = 1600;
export const CALIDAD_IMAGEN = 0.8;
export const DURACION_MAXIMA_VIDEO_S = 90;
export const TAMANO_MAXIMO_VIDEO_BYTES = 60 * 1024 * 1024;
export const TAMANO_MAXIMO_DOCUMENTO_BYTES = 10 * 1024 * 1024;

export interface Dimensiones {
  readonly ancho: number;
  readonly alto: number;
}

/** Reduce proporcionalmente para que el lado mayor no pase de `ladoMaximo`; nunca amplía. */
export function calcularDimensiones(
  ancho: number,
  alto: number,
  ladoMaximo = LADO_MAXIMO_PX,
): Dimensiones {
  const mayor = Math.max(ancho, alto);
  if (mayor <= ladoMaximo) {
    return { ancho, alto };
  }
  const escala = ladoMaximo / mayor;
  return { ancho: Math.round(ancho * escala), alto: Math.round(alto * escala) };
}

/** Devuelve el motivo por el que un video no se acepta, o `null` si cumple. */
export function validarVideo(duracionSegundos: number, tamanoBytes: number): string | null {
  if (!Number.isFinite(duracionSegundos)) {
    return 'No pudimos leer la duración del video. Prueba con otro archivo.';
  }
  if (duracionSegundos > DURACION_MAXIMA_VIDEO_S) {
    return `El video dura más de ${DURACION_MAXIMA_VIDEO_S} segundos. Graba uno más corto.`;
  }
  if (tamanoBytes > TAMANO_MAXIMO_VIDEO_BYTES) {
    return 'El video pesa más de 60 MB. Graba uno más corto o con menor calidad.';
  }
  return null;
}

/** Comprime una foto: WebP si el navegador lo permite y JPEG si no (Safari no codifica WebP). */
export async function comprimirImagen(archivo: Blob): Promise<Blob> {
  const imagen = await createImageBitmap(archivo, { imageOrientation: 'from-image' });
  try {
    const { ancho, alto } = calcularDimensiones(imagen.width, imagen.height);
    const lienzo = document.createElement('canvas');
    lienzo.width = ancho;
    lienzo.height = alto;
    const contexto = lienzo.getContext('2d');
    if (!contexto) {
      throw new Error('No pudimos procesar la imagen en este dispositivo.');
    }
    contexto.drawImage(imagen, 0, 0, ancho, alto);
    const webp = await aBlob(lienzo, 'image/webp');
    if (webp?.type === 'image/webp') {
      return webp;
    }
    const jpeg = await aBlob(lienzo, 'image/jpeg');
    if (!jpeg) {
      throw new Error('No pudimos procesar la imagen en este dispositivo.');
    }
    return jpeg;
  } finally {
    imagen.close();
  }
}

/** Lee la duración de un video desde sus metadatos, sin cargarlo completo. */
export function leerDuracionVideo(archivo: Blob): Promise<number> {
  return new Promise((resolver, rechazar) => {
    const url = URL.createObjectURL(archivo);
    const video = document.createElement('video');
    video.preload = 'metadata';
    const terminar = (): void => URL.revokeObjectURL(url);
    video.onloadedmetadata = () => {
      terminar();
      resolver(video.duration);
    };
    video.onerror = () => {
      terminar();
      rechazar(new Error('No pudimos leer el video. Prueba con otro archivo.'));
    };
    video.src = url;
  });
}

function aBlob(lienzo: HTMLCanvasElement, tipo: string): Promise<Blob | null> {
  return new Promise((resolver) => lienzo.toBlob(resolver, tipo, CALIDAD_IMAGEN));
}
