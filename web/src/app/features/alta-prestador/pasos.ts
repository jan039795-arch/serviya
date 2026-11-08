import { TipoDocumento } from '../../core/api/modelos';

export interface PasoAlta {
  readonly numero: number;
  readonly titulo: string;
}

/** Los 10 pasos del alta (T020). El número es también la ruta: `/alta/3`. */
export const PASOS_ALTA: readonly PasoAlta[] = [
  { numero: 1, titulo: 'Datos personales' },
  { numero: 2, titulo: 'Oficios y experiencia' },
  { numero: 3, titulo: 'Tarifas de referencia' },
  { numero: 4, titulo: 'Zona de cobertura' },
  { numero: 5, titulo: 'Disponibilidad' },
  { numero: 6, titulo: 'Descripción' },
  { numero: 7, titulo: 'Consentimiento' },
  { numero: 8, titulo: 'Documentos' },
  { numero: 9, titulo: 'Video y fotos' },
  { numero: 10, titulo: 'Revisión y envío' },
];

export const TOTAL_PASOS = PASOS_ALTA.length;

export interface DescripcionDocumento {
  readonly tipo: TipoDocumento;
  readonly titulo: string;
  readonly ayuda: string;
  readonly obligatorio: boolean;
  readonly pideFecha: boolean;
}

/** Documentos del paso 8 (FR-006): los cuatro primeros son obligatorios. */
export const DOCUMENTOS: readonly DescripcionDocumento[] = [
  {
    tipo: 'IneFrente',
    titulo: 'INE (frente)',
    ayuda: 'Foto completa y legible, sin reflejos.',
    obligatorio: true,
    pideFecha: false,
  },
  {
    tipo: 'IneReverso',
    titulo: 'INE (reverso)',
    ayuda: 'Foto completa del reverso.',
    obligatorio: true,
    pideFecha: false,
  },
  {
    tipo: 'Selfie',
    titulo: 'Selfie',
    ayuda: 'Una foto de tu cara, de frente y con buena luz. La comparamos con tu INE.',
    obligatorio: true,
    pideFecha: false,
  },
  {
    tipo: 'ComprobanteDomicilio',
    titulo: 'Comprobante de domicilio',
    ayuda: 'Recibo de luz, agua, teléfono o predial de máximo 3 meses.',
    obligatorio: true,
    pideFecha: true,
  },
  {
    tipo: 'CartaAntecedentes',
    titulo: 'Carta de no antecedentes penales',
    ayuda:
      'Opcional. Si la subes y se aprueba, tu perfil muestra la insignia "Antecedentes presentados".',
    obligatorio: false,
    pideFecha: true,
  },
];

export function tituloDocumento(tipo: TipoDocumento): string {
  return DOCUMENTOS.find((d) => d.tipo === tipo)?.titulo ?? tipo;
}

export { DIAS_SEMANA } from '../../shared/textos';
