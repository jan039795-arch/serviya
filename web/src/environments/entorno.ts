/** Áreas de la API que el prototipo puede simular con `MockApiInterceptor` (research R10). */
export interface AreasSimuladas {
  readonly cuentas: boolean;
  readonly prestadores: boolean;
  readonly busqueda: boolean;
  readonly accesos: boolean;
  readonly admin: boolean;
}

export interface Entorno {
  readonly produccion: boolean;
  /** Ruta base de la API REST v1 (contracts/api.md). */
  readonly apiUrl: string;
  /** `true` en un área = sus peticiones se responden con datos ficticios. */
  readonly mocks: AreasSimuladas;
}
