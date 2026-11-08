/** Texto de un `<input>` o `<textarea>` a partir de su evento, sin `$any` en la plantilla. */
export function textoDe(evento: Event): string {
  const destino = evento.target;
  return destino instanceof HTMLInputElement || destino instanceof HTMLTextAreaElement
    ? destino.value
    : '';
}

/** Estado de una casilla a partir de su evento `change`. */
export function marcadoDe(evento: Event): boolean {
  const destino = evento.target;
  return destino instanceof HTMLInputElement && destino.checked;
}
