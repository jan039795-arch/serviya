import { DocumentoLegal } from './api/modelos';

/**
 * Versiones vigentes de los textos legales. Son borradores hasta que los revise un abogado
 * (T140); la API registra la versión aceptada (FR-044).
 */
export const VERSIONES_LEGALES: Readonly<Record<DocumentoLegal, string>> = {
  AvisoPrivacidad: '2026-11-borrador',
  Terminos: '2026-11-borrador',
  PoliticaReembolsos: '2026-11-borrador',
  DatosSensibles: '2026-11-borrador',
};
