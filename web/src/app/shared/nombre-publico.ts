/**
 * Nombre que se muestra en público (FR-017): nombre de pila + inicial del primer apellido.
 * "Juan Pérez López" → "Juan P."; "Ana Ruiz" → "Ana R.". Con 3 palabras o más se toma la
 * penúltima como primer apellido. La API calcula el definitivo; esto es la vista previa.
 */
export function nombrePublico(nombreCompleto: string): string {
  const palabras = nombreCompleto.trim().split(/\s+/).filter(Boolean);
  if (palabras.length === 0) {
    return '';
  }
  if (palabras.length === 1) {
    return palabras[0];
  }
  const apellido = palabras.length >= 3 ? palabras[palabras.length - 2] : palabras[1];
  return `${palabras[0]} ${apellido.charAt(0).toLocaleUpperCase('es-MX')}.`;
}
