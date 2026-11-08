# Modelo de datos: ServiYa MVP

**Fecha**: 2026-11-05 · **Plan**: [plan.md](plan.md)

Convenciones: base PostgreSQL 17 con PostGIS. Claves `uuid` (v7). Fechas `timestamptz` en UTC. Montos
en **centavos** (`integer`, MXN, IVA incluido). Columnas `created_at` y `updated_at` en todas las
tablas. Cada módulo tiene su esquema (`cuentas`, `prestadores`, `documentos`, `pagos`, `chat`,
`contrataciones`, `calificaciones`, `reportes`, `admin`, `catalogos`) y un `DbContext` por módulo; un
módulo no hace consultas a las tablas de otro, solo usa sus servicios o referencias por Id.

---

## Cuentas (`cuentas`)

### Usuario (extiende `IdentityUser<Guid>`)

| Campo | Tipo | Reglas |
|---|---|---|
| Email | text | Único, verificado antes de usar la cuenta (FR-001) |
| PhoneNumber | text? | Formato E.164 +52; privado |
| NombreCompleto | text | 3–120 caracteres |
| FechaNacimiento | date | 18 años o más para publicar perfil o pagar (FR-004) |
| Estado | enum | `Activo`, `Suspendido`, `Eliminado` |
| Roles | — | `Cliente` (por defecto), `Prestador`, `Admin` (FR-002, FR-003) |
| EliminadoEn | timestamptz? | Borrado lógico; los datos personales se anonimizan (FR-045) |

### CodigoVerificacion

`Id`, `UsuarioId`, `Proposito` (`VerificarCorreo`, `RecuperarContrasena`), `CodigoHash`, `ExpiraEn`
(10 min), `Intentos` (máximo 5), `UsadoEn?`.

### Consentimiento (FR-007, FR-044)

`Id`, `UsuarioId`, `Documento` (`AvisoPrivacidad`, `Terminos`, `PoliticaReembolsos`,
`DatosSensibles`), `Version`, `AceptadoEn`, `Ip`. Solo se agregan registros; nunca se modifican.

### Bloqueo (FR-040)

`BloqueadorId`, `BloqueadoId`, `CreadoEn`. Clave única (bloqueador, bloqueado).

---

## Prestadores (`prestadores`)

### PerfilPrestador

| Campo | Tipo | Reglas |
|---|---|---|
| UsuarioId | uuid | 1:1 con Usuario |
| NombrePublico | text | Calculado: nombre de pila + inicial del apellido (FR-017) |
| Descripcion | text | 0–600 caracteres; pasa por `ContactMasker` |
| Direccion | text | **Privada**; nunca sale en respuestas públicas |
| ColoniaBase | text | Pública |
| PuntoBase | geography(Point) | Aproximado (centroide de la colonia); privado |
| Disponibilidad | jsonb | Días y franjas horarias |
| Estado | enum | Ver transiciones abajo (FR-009) |
| PasoAlta | int | Último paso guardado (alta interrumpible) |
| EnviadoRevisionEn | timestamptz? | Orden de la cola de revisión |
| Insignias | text[] | `IdentidadVerificada`, `AntecedentesPresentados` |
| PromedioCalidad, PromedioPrecio, PromedioPuntualidad, PromedioTrato | numeric(3,2) | Recalculados al publicar reseñas |
| TotalResenas | int | |
| PuntajeRanking | numeric | Bayesiano (research R5) |

**Transiciones de estado**:

```
Borrador ──enviar──▶ EnRevision ──aprobar──▶ Aprobado ──suspender──▶ Suspendido ──levantar──▶ Aprobado
                       │  ▲                      │
          pedir        │  │ reenviar             └──eliminar cuenta──▶ Eliminado
       corrección      ▼  │
                 RequiereCorreccion
                       │
                    rechazar ──▶ Rechazado
```

- `enviar` exige los datos completos, la INE (frente y reverso), la selfie y el comprobante de
  domicilio cargados, el consentimiento de datos sensibles aceptado y 18 años o más.
- `aprobar` exige que **todos** los documentos obligatorios estén en `Aprobado`.
- Solo los perfiles `Aprobado` aparecen en búsquedas o por enlace directo (FR-019, US2 escenario 3).

### OficioPrestador

`PrestadorId`, `OficioId`, `AniosExperiencia` (0–60), `TarifaMin` y `TarifaMax` (centavos),
`UnidadTarifa` (`Hora`, `Dia`, `Servicio`). Clave única (prestador, oficio).

### CoberturaPrestador

`PrestadorId`, `MunicipioId`. Clave única. Al menos una por prestador para enviar a revisión.

### ElementoPortafolio (FR-008, FR-012)

`Id`, `PrestadorId`, `Tipo` (`Foto`, `Video`), `ClaveObjetoPrivado`, `ClaveObjetoPublico?`,
`Descripcion` (≤ 200, con `ContactMasker`), `Orden`, `EstadoModeracion` (`Pendiente`, `Aprobado`,
`Rechazado`), `MotivoRechazo?`. Máximo 20 fotos y 1 video (90 s como máximo, 60 MB).

---

## Documentos (`documentos`)

### Documento

| Campo | Tipo | Reglas |
|---|---|---|
| Id | uuid | |
| PrestadorId | uuid | |
| Tipo | enum | `IneFrente`, `IneReverso`, `Selfie`, `ComprobanteDomicilio`, `CartaAntecedentes` |
| ClaveObjeto | text | Bucket privado; GUID sin datos personales |
| TipoContenido | text | `image/jpeg`, `image/png`, `image/webp`, `application/pdf` |
| TamanoBytes | int | ≤ 10 MB |
| Estado | enum | `Cargado`, `Aprobado`, `RequiereCorreccion`, `Rechazado`, `Vencido`, `Reemplazado` |
| Motivo | text? | Obligatorio en rechazo o corrección (FR-010) |
| FechaExpedicion | date? | Obligatoria para el comprobante (máximo 3 meses) y la carta |
| VenceEn | date? | Carta: expedición + 12 meses (FR-013) |
| RevisadoPor, RevisadoEn | uuid?, timestamptz? | |

Reglas: al subir una versión nueva del mismo tipo, la anterior pasa a `Reemplazado`. Solo puede haber
un documento vigente por tipo. La carta en `Aprobado` otorga la insignia, y al vencer pasa a
`Vencido` y se retira la insignia.

### RegistroBitacora (`admin`, FR-016, FR-042)

`Id`, `ActorId`, `Accion` (por ejemplo `VerDocumento`, `AprobarDocumento`, `SuspenderCuenta`,
`OtorgarRolAdmin`, `Reembolsar`), `EntidadTipo`, `EntidadId`, `Detalle` (jsonb, **sin datos
personales**), `Ip`, `CreadoEn`. Tabla de solo inserción: el usuario de base de datos de la app no
tiene permiso de `UPDATE` ni `DELETE` sobre ella.

---

## Archivos (`archivos`)

### ArchivoSubido (R3)

`Id`, `UsuarioId`, `Destino` (`Documento`, `Portafolio`, `Chat`, `Calificacion`), `Bucket`, `ClaveObjeto`
(GUID), `TipoContenido`, `TamanoBytesDeclarado`, `ExpiraEn` (10 min), `ConsumidoEn?`. Lo crea la emisión de la
URL de subida; al usar el `archivoId` la API verifica que sea del usuario, no esté vencido ni consumido, exista
en el bucket, no supere el tamaño declarado y su firma coincida con el tipo. Lo administra `ServiYa.Comun`.

---

## Catálogos (`catalogos`)

- **Oficio**: `Id`, `Nombre`, `Slug`, `Icono`, `Activo`. Semilla: albañil, fontanero, electricista,
  carpintero, pintor, jardinero, chofer, maestro particular, limpieza del hogar, cuidado de niños,
  cuidado de adultos mayores y cerrajero.
- **Municipio**: `Id`, `ClaveInegi`, `Nombre`, `Estado`, `Poligono` (geography(MultiPolygon)),
  `Centroide`. Solo los de la zona del piloto.

---

## Pagos (`pagos`)

### AccesoContacto (FR-020, FR-024)

| Campo | Tipo | Reglas |
|---|---|---|
| Id | uuid | |
| ClienteId, PrestadorId | uuid | |
| Origen | enum | `Individual`, `Pase`, `Credito` |
| PagoId? / PaseId? / CreditoId? | uuid? | Según el origen |
| Estado | enum | `Pendiente`, `Activo`, `Vencido`, `Expirado`, `Reembolsado` |
| ActivadoEn, VenceEn | timestamptz? | VenceEn = ActivadoEn + 30 días |
| PrimerMensajeClienteEn | timestamptz? | Arranca el plazo de 48 h (FR-026) |
| RespuestaPrestadorEn | timestamptz? | |

**Transiciones**: `Pendiente → Activo` (pago confirmado), `Pendiente → Expirado` (referencia vencida
o rechazo), `Activo → Vencido` (30 días), `Activo → Reembolsado` (devolución).

Restricción: **un solo acceso `Pendiente` o `Activo` por (cliente, prestador)**, con índice único
parcial (FR-025).

### PaseMensual (FR-021, FR-028)

`Id`, `ClienteId`, `InicioEn`, `FinEn` (inicio + 30 días), `ContactosIncluidos` (30),
`ContactosUsados`, `RenovacionAutomatica` (bool), `SuscripcionMpId?`, `RenovacionCanceladaEn?`,
`AvisoRenovacionEnviadoEn?`, `Estado` (`Pendiente`, `Activo`, `Vencido`, `Cancelado`).
Regla: `ContactosUsados ≤ ContactosIncluidos`, con incremento atómico (`UPDATE ... WHERE
contactos_usados < contactos_incluidos`).

### Pago (FR-022, FR-023, FR-031)

| Campo | Tipo | Reglas |
|---|---|---|
| Id | uuid | Se usa como `external_reference` en Mercado Pago |
| ClienteId | uuid | |
| Concepto | enum | `ContactoIndividual`, `PaseMensual`, `RenovacionPase`, `CuotaAdministracion` |
| MontoCentavos | int | 3900, 12900 o 12900 menos el descuento (FR-027) |
| DescuentoCentavos | int | ≥ 0 |
| Metodo | text? | `credit_card`, `debit_card`, `ticket` (OXXO o Paycash), `bank_transfer` (SPEI), `account_money` |
| Estado | enum | `Creado`, `Pendiente`, `Aprobado`, `Rechazado`, `Expirado`, `Reembolsado` |
| PreferenciaMpId | text? | |
| PagoMpId | text? | **Único** cuando no es nulo (idempotencia del webhook) |
| AvisoMostradoVersion | text | Versión del aviso de no garantía mostrado (FR-022) |
| ConfirmadoEn | timestamptz? | |

### NotificacionMp

`Id`, `RequestId` (único), `Tipo`, `DataId`, `RecibidaEn`, `ProcesadaEn?`, `Error?`. Se guarda antes
de procesar, para poder reintentar y auditar.

### Credito (FR-026, FR-043)

`Id`, `ClienteId`, `Motivo` (`SinRespuesta48h`, `PrestadorSuspendido`, `PrestadorEliminado`,
`PagoDuplicado`), `AccesoOrigenId?`, `Estado` (`Disponible`, `Usado`, `CambiadoPorReembolso`),
`UsadoEnAccesoId?`. Un crédito equivale a un contacto nuevo. El cliente puede cambiarlo por la
devolución en dinero del pago de origen.

### SolicitudFactura

`Id`, `PagoId`, `Rfc`, `RazonSocial`, `RegimenFiscal`, `CodigoPostal`, `UsoCfdi`, `Estado`
(`Solicitada`, `Emitida`), `EmitidaEn?`.

---

## Chat (`chat`)

- **Conversacion**: `Id`, `ClienteId`, `PrestadorId`, `UltimoMensajeEn`. Clave única (cliente,
  prestador). Se puede escribir solo con un `AccesoContacto` `Activo`; si no hay, queda en solo
  lectura (FR-034).
- **Mensaje**: `Id`, `ConversacionId`, `AutorId`, `Texto` (≤ 2000), `TextoMostrado` (con
  `ContactMasker` aplicado si no había acceso activo), `ClaveFoto?`, `EnviadoEn`, `LeidoEn?`,
  `OcultoPorModeracion` (bool).
- **SuscripcionPush**: `Id`, `UsuarioId`, `Endpoint`, `P256dh`, `Auth`, `CreadaEn`.

---

## Contrataciones (`contrataciones`)

### Contratacion (FR-035)

`Id`, `ConversacionId`, `ClienteId`, `PrestadorId`, `Descripcion` (≤ 500), `FechaServicio`,
`PrecioAcordadoCentavos` (solo informativo; la plataforma no lo cobra), `Estado`, `TerminadaEn?`.

```
Propuesta ──confirmar (prestador)──▶ Confirmada ──terminar (cualquiera)──▶ Terminada
    │                                    │
    └─rechazar──▶ Rechazada              └─cancelar──▶ Cancelada
```

---

## Calificaciones (`calificaciones`)

### Calificacion (FR-036 a FR-039)

| Campo | Tipo | Reglas |
|---|---|---|
| Id | uuid | |
| ContratacionId | uuid | Debe estar `Terminada` |
| AutorId, DestinatarioId | uuid | Una calificación por autor y contratación |
| Direccion | enum | `ClienteAPrestador`, `PrestadorACliente` |
| Calidad, PrecioJusto, Puntualidad, Trato | smallint 1–5 | Al prestador (todos obligatorios) |
| PuntualidadPago, TratoCliente, Claridad | smallint 1–5 | Al cliente |
| Comentario | text? | ≤ 1000, con `ContactMasker` |
| ClavesFotos | text[] | Hasta 5 |
| Estado | enum | `Oculta` (doble ciega) → `Publicada`; `RetiradaPorModeracion` |
| PublicadaEn | timestamptz? | Cuando califican ambos o a los 14 días |
| Respuesta, RespondidaEn | text?, timestamptz? | Una sola respuesta del destinatario prestador |

---

## Reportes (`reportes`)

- **Reporte** (FR-040, FR-041): `Id`, `ReportanteId`, `ObjetivoTipo` (`Perfil`, `Mensaje`,
  `Resena`), `ObjetivoId`, `Motivo` (catálogo), `Detalle`, `Estado` (`Abierto`, `Resuelto`,
  `Descartado`), `Resolucion?`, `ResueltoPor?`.
- **Suspension**: `Id`, `UsuarioId`, `Motivo`, `AplicadaPor`, `AplicadaEn`, `LevantadaEn?`,
  `Aclaracion?`, `AclaracionEn?`, `Decision?`. Al aplicarse, se generan créditos a los clientes con
  acceso activo al prestador (FR-043).

---

## Índices clave

- `prestadores.perfil (estado, puntaje_ranking DESC)` para la búsqueda.
- `prestadores.cobertura (municipio_id)`, `oficio_prestador (oficio_id)`.
- GIST sobre `municipio.poligono` y `perfil.punto_base`.
- Único parcial `acceso_contacto (cliente_id, prestador_id) WHERE estado IN ('Pendiente','Activo')`.
- Único `pago (pago_mp_id) WHERE pago_mp_id IS NOT NULL`; único `notificacion_mp (request_id)`.
- `mensaje (conversacion_id, enviado_en)`.
- Para las tareas programadas: `acceso_contacto (estado, vence_en)`,
  `acceso_contacto (primer_mensaje_cliente_en) WHERE respuesta_prestador_en IS NULL`,
  `calificacion (estado, created_at)`.
