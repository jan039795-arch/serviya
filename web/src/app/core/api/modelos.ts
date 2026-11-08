/**
 * Tipos de los contratos de la API v1 (specs/001-serviya-mvp/contracts/api.md).
 * Provisionales: en T068 se reemplazan por alias de los tipos generados con openapi-typescript.
 * Montos en centavos (MXN, IVA incluido) y fechas en ISO 8601 (UTC).
 */

export type Rol = 'Cliente' | 'Prestador' | 'Admin';
export type EstadoUsuario = 'Activo' | 'Suspendido' | 'Eliminado';
export type DocumentoLegal =
  'AvisoPrivacidad' | 'Terminos' | 'PoliticaReembolsos' | 'DatosSensibles';
export type Insignia = 'IdentidadVerificada' | 'AntecedentesPresentados';
export type UnidadTarifa = 'Hora' | 'Dia' | 'Servicio';
export type EstadoPerfil =
  | 'Borrador'
  | 'EnRevision'
  | 'RequiereCorreccion'
  | 'Aprobado'
  | 'Rechazado'
  | 'Suspendido'
  | 'Eliminado';
export type TipoDocumento =
  'IneFrente' | 'IneReverso' | 'Selfie' | 'ComprobanteDomicilio' | 'CartaAntecedentes';
export type EstadoDocumento =
  'Cargado' | 'Aprobado' | 'RequiereCorreccion' | 'Rechazado' | 'Vencido' | 'Reemplazado';
export type EstadoModeracion = 'Pendiente' | 'Aprobado' | 'Rechazado';
export type OrigenAcceso = 'Individual' | 'Pase' | 'Credito';
export type EstadoAcceso = 'Pendiente' | 'Activo' | 'Vencido' | 'Expirado' | 'Reembolsado';
export type EstadoPase = 'Pendiente' | 'Activo' | 'Vencido' | 'Cancelado';
export type ConceptoPago =
  'ContactoIndividual' | 'PaseMensual' | 'RenovacionPase' | 'CuotaAdministracion';
export type EstadoPago =
  'Creado' | 'Pendiente' | 'Aprobado' | 'Rechazado' | 'Expirado' | 'Reembolsado';

/** Página de resultados: `?pagina=1&tamano=20`. */
export interface Pagina<T> {
  readonly items: readonly T[];
  readonly total: number;
  readonly pagina: number;
  readonly tamano: number;
}

/** Error de la API en formato RFC 9457 (`application/problem+json`). */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  /** Errores de validación por campo. */
  readonly errors?: Readonly<Record<string, readonly string[]>>;
  /** Lista de faltantes de `POST /prestadores/yo/enviar-revision` (422). */
  readonly faltantes?: readonly string[];
}

// ---------- Cuentas ----------

export interface Sesion {
  readonly id: string;
  readonly nombreCompleto: string;
  readonly email: string;
  readonly roles: readonly Rol[];
  readonly estado: EstadoUsuario;
  readonly esMayorDeEdad: boolean;
}

export interface Aceptacion {
  readonly documento: DocumentoLegal;
  readonly version: string;
}

export interface SolicitudRegistro {
  readonly email: string;
  readonly password: string;
  readonly nombreCompleto: string;
  /** Fecha `AAAA-MM-DD`. */
  readonly fechaNacimiento: string;
  readonly rol: 'Cliente' | 'Prestador';
  readonly aceptaciones: readonly Aceptacion[];
}

// ---------- Catálogos ----------

export interface Oficio {
  readonly id: string;
  readonly nombre: string;
  readonly slug: string;
  readonly icono: string;
}

export interface Municipio {
  readonly id: string;
  readonly claveInegi: string;
  readonly nombre: string;
  readonly estado: string;
}

// ---------- Alta del prestador ----------

export interface FranjaDisponibilidad {
  /** 0 = domingo … 6 = sábado. */
  readonly dia: number;
  /** `HH:mm`. */
  readonly desde: string;
  readonly hasta: string;
}

export interface OficioDelPrestador {
  readonly oficioId: string;
  readonly aniosExperiencia: number;
  readonly tarifaMinCentavos: number;
  readonly tarifaMaxCentavos: number;
  readonly unidadTarifa: UnidadTarifa;
}

export interface DatosPersonales {
  readonly nombreCompleto: string;
  readonly fechaNacimiento: string;
  readonly telefono: string;
  readonly direccion: string;
  readonly coloniaBase: string;
}

export interface DocumentoPropio {
  readonly id: string;
  readonly tipo: TipoDocumento;
  readonly estado: EstadoDocumento;
  readonly motivo?: string;
  readonly fechaExpedicion?: string;
  readonly venceEn?: string;
}

export interface ElementoPortafolioPropio {
  readonly id: string;
  readonly tipo: 'Foto' | 'Video';
  readonly descripcion: string;
  readonly estadoModeracion: EstadoModeracion;
  readonly motivoRechazo?: string;
  readonly url?: string;
}

/** Perfil del propio prestador: incluye datos privados (solo lo ve él). */
export interface PerfilPropio {
  readonly id: string;
  readonly estado: EstadoPerfil;
  /** Motivo del último cambio de estado (rechazo, corrección o suspensión), si lo hay. */
  readonly motivoEstado?: string;
  readonly pasoAlta: number;
  readonly datosPersonales?: DatosPersonales;
  readonly oficios: readonly OficioDelPrestador[];
  readonly cobertura: readonly string[];
  readonly disponibilidad: readonly FranjaDisponibilidad[];
  readonly descripcion: string;
  readonly consentimientoSensibleVersion?: string;
  readonly documentos: readonly DocumentoPropio[];
  readonly portafolio: readonly ElementoPortafolioPropio[];
  readonly nombrePublico?: string;
  readonly insignias: readonly Insignia[];
}

/** Cuerpo de `PATCH /prestadores/yo` (guardado parcial por paso). */
export interface GuardadoPaso {
  readonly paso: number;
  readonly datosPersonales?: DatosPersonales;
  readonly oficios?: readonly OficioDelPrestador[];
  readonly cobertura?: readonly string[];
  readonly disponibilidad?: readonly FranjaDisponibilidad[];
  readonly descripcion?: string;
}

export interface SolicitudUrlSubida {
  readonly destino: 'Documento' | 'Portafolio';
  readonly tipo: string;
  readonly tipoContenido: string;
  readonly tamanoBytes: number;
}

export interface UrlSubida {
  readonly archivoId: string;
  readonly urlSubida: string;
  readonly expiraEn: string;
}

// ---------- Búsqueda y perfil público (nunca datos privados) ----------

export interface PromediosPorEje {
  readonly calidad: number;
  readonly precioJusto: number;
  readonly puntualidad: number;
  readonly trato: number;
}

export interface OficioPublico {
  readonly oficioId: string;
  readonly nombre: string;
  readonly aniosExperiencia: number;
  readonly tarifaMinCentavos: number;
  readonly tarifaMaxCentavos: number;
  readonly unidadTarifa: UnidadTarifa;
}

export interface TarjetaPrestador {
  readonly id: string;
  readonly nombrePublico: string;
  readonly colonia: string;
  readonly municipio: string;
  readonly oficios: readonly string[];
  readonly tarifaDesde: number;
  readonly promedios: PromediosPorEje;
  readonly totalResenas: number;
  readonly insignias: readonly Insignia[];
  readonly fotoPrincipalUrl?: string;
}

export interface FotoPublica {
  readonly url: string;
  readonly descripcion: string;
}

export interface PerfilPublico {
  readonly id: string;
  readonly nombrePublico: string;
  readonly colonia: string;
  readonly municipio: string;
  readonly descripcion: string;
  readonly oficios: readonly OficioPublico[];
  readonly disponibilidad: readonly FranjaDisponibilidad[];
  readonly videoUrl?: string;
  readonly fotos: readonly FotoPublica[];
  readonly insignias: readonly Insignia[];
  readonly promedios: PromediosPorEje;
  readonly totalResenas: number;
}

export interface ResenaPublica {
  readonly id: string;
  readonly autor: string;
  readonly calificaciones: PromediosPorEje;
  readonly comentario?: string;
  readonly fotosUrls: readonly string[];
  readonly publicadaEn: string;
  readonly respuesta?: string;
}

// ---------- Acceso al contacto y pagos ----------

export interface AvisoPago {
  readonly version: string;
  readonly texto: string;
}

export interface AccesoContacto {
  readonly id: string;
  readonly prestadorId: string;
  readonly nombrePrestador: string;
  readonly origen: OrigenAcceso;
  readonly estado: EstadoAcceso;
  readonly activadoEn?: string;
  readonly venceEn?: string;
}

export interface OpcionesAcceso {
  readonly accesoVigente?: AccesoContacto;
  readonly pase?: { readonly contactosDisponibles: number; readonly finEn: string };
  readonly creditosDisponibles: number;
  readonly ofertaPase?: { readonly montoCentavos: number; readonly descuentoCentavos: number };
  readonly aviso: AvisoPago;
  readonly precios: { readonly contactoCentavos: number; readonly paseCentavos: number };
}

export interface SolicitudAcceso {
  readonly prestadorId: string;
  readonly via: OrigenAcceso;
  readonly avisoVersion: string;
}

export interface RespuestaAcceso {
  readonly acceso: AccesoContacto;
  /** Solo cuando hay que pagar en Checkout Pro. */
  readonly checkoutUrl?: string;
}

export interface PaseMensual {
  readonly id: string;
  readonly estado: EstadoPase;
  readonly inicioEn?: string;
  readonly finEn?: string;
  readonly contactosIncluidos: number;
  readonly contactosUsados: number;
  readonly renovacionAutomatica: boolean;
  readonly renovacionCanceladaEn?: string;
}

export interface Pago {
  readonly id: string;
  readonly concepto: ConceptoPago;
  readonly montoCentavos: number;
  readonly descuentoCentavos: number;
  readonly metodo?: string;
  readonly estado: EstadoPago;
  readonly creadoEn: string;
  readonly confirmadoEn?: string;
}

export interface ContactoDesbloqueado {
  readonly telefono: string;
  readonly whatsapp?: string;
}
