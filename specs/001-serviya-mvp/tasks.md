---

description: "Lista de tareas del MVP de ServiYa"
---

# Tasks: ServiYa MVP — Plataforma de contacto con prestadores de oficios verificados

**Input**: documentos de diseño en `specs/001-serviya-mvp/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/api.md](contracts/api.md),
[contracts/webhooks.md](contracts/webhooks.md), [quickstart.md](quickstart.md)

**Tests**: la constitución (principio VI) y `.claude/rules/common/testing.md` las exigen. Los módulos
críticos (Pagos y webhook, permisos por rol, desbloqueo de contacto, Documentos y `ContactMasker`) se
hacen con **TDD**: la prueba se escribe primero, debe fallar y luego se implementa. Cobertura mínima
del 80% en esos módulos. El resto lleva pruebas de comportamiento y de casos de error.

**Organization**: las fases siguen las etapas de `plan.md`. La Fase 2 es el prototipo con datos
ficticios de US1–US4 (Etapa 2). Desde la Fase 5, cada historia tiene su fase con backend, conexión
del frontend y pruebas, y se puede probar de forma independiente.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin depender de tareas incompletas).
- **[Story]**: historia de usuario de `spec.md` (US1–US8).
- Rutas relativas a la raíz del repositorio (`api/`, `web/`, `docker-compose.yml`).

## Convenciones para todas las tareas

- Backend: un proyecto por módulo en `api/src/Modulos/ServiYa.<Modulo>/` con carpetas `Dominio/`,
  `Datos/`, `Endpoints/`, `Servicios/` y `Tareas/`; cada módulo implementa `IModulo` y tiene su
  `DbContext` y su esquema. Un módulo **no consulta tablas de otro**: usa sus interfaces públicas
  (`ICuentasService`, `IAccesosService`, `IArchivosService`, `IBitacora`, etc.).
- DTOs, comandos y opciones como `record`; errores como `ProblemDetails`; sin `IRepository<T>`.
- Claves `uuid` v7 (`Guid.CreateVersion7()`), fechas `timestamptz` en UTC con `TimeProvider`
  (`FakeTimeProvider` en pruebas), montos en centavos (`int`, MXN, IVA incluido).
- Frontend: componentes standalone sin sufijo `.component`, `inject()`, signals, `@if`/`@for`,
  Signal Forms, `data-testid` en elementos que usan las pruebas E2E, textos en español de México.
- Nunca se usan documentos reales: solo imágenes generadas con la leyenda "DOCUMENTO FICTICIO".

---

## Fase 1: Preparación del frontend (Etapa 2)

**Purpose**: proyecto Angular listo para construir el prototipo. Solo necesita Node 24.

- [x] T001 Crear el proyecto en `web/` con `npx @angular/cli@22 new web --routing --ssr --style=css --skip-git` (componentes standalone, archivos sin sufijo `.component`) y confirmar que `npm start` abre `http://localhost:4200`
- [x] T002 Agregar la PWA con `ng add @angular/pwa` y ajustar `web/public/manifest.webmanifest` (`name` "ServiYa", `lang` "es-MX", `display` "standalone", `theme_color`, íconos 192 y 512 px `maskable`) y `web/ngsw-config.json` (sin caché de `/api/**`)
- [x] T003 [P] Configurar ESLint (`ng add angular-eslint`) y Prettier (`web/.prettierrc`), con los scripts `lint` y `format` en `web/package.json`
- [x] T004 [P] Verificar el ejecutor de pruebas de Angular CLI 22 (Vitest) con `npx ng test --no-watch` sobre `web/src/app/app.spec.ts`
- [x] T005 [P] Instalar Playwright 1.65 como dependencia de desarrollo y crear `web/playwright.config.ts` con los proyectos `Pixel 7` e `iPhone 14`, `baseURL` `http://localhost:4200`, `webServer` con `npm start` y carpeta `web/e2e/`
- [x] T006 [P] Crear `web/src/environments/environment.ts` (`apiUrl: '/api/v1'`, `mocks: { cuentas: true, prestadores: true, busqueda: true, accesos: true, admin: true }`), `environment.prototipo.ts` (todo simulado con optimización) y `environment.prod.ts` (todo en `false`), con sus `fileReplacements` y la configuración `prototipo` en `web/angular.json`
- [x] T007 [P] Configurar el idioma: `registerLocaleData(localeEsMx)`, `LOCALE_ID` `'es-MX'` en `web/src/app/app.config.ts` y `<html lang="es-MX">` en `web/src/index.html`
- [x] T008 [P] Crear los estilos base mobile-first en `web/src/styles.css`: variables de color con contraste AA, texto base de 16 px, áreas táctiles de al menos 44 px, diseño desde 360 px de ancho y foco visible
- [x] T009 Agregar a la sección "Comandos" de `CLAUDE.md` los comandos del frontend (`npm start`, `npx ng test --no-watch`, `npx playwright test`, `npm run lint`)

---

## Fase 2: Prototipo con datos ficticios (Etapa 2)

**Purpose**: validar en el celular las pantallas de US1–US4 antes de escribir el backend (research
R10). Todo responde con `MockApiInterceptor` según `contracts/api.md`.

**Independent Test**: los cinco puntos de la sección "Etapa 2" de [quickstart.md](quickstart.md) en
los perfiles Pixel 7 e iPhone 14.

### Base del prototipo

- [x] T010 Crear las interfaces de los contratos en `web/src/app/core/api/modelos.ts` (`Sesion`, `PerfilPropio`, `DocumentoPropio`, `TarjetaPrestador`, `PerfilPublico`, `ResenaPublica`, `Oficio`, `Municipio`, `OpcionesAcceso`, `AccesoContacto`, `PaseMensual`, `Pago`, `Pagina<T>` con `{ items, total, pagina, tamano }` y `ProblemDetails`); se reemplazan por tipos generados en T068
- [x] T011 [P] Crear los datos ficticios en `web/src/mocks/`: `oficios.json` (los 12 oficios semilla de data-model.md), `municipios.json` (6 municipios ficticios), `prestadores.json` (14 prestadores inventados: 12 `Aprobado`, 1 `EnRevision`, 1 `Suspendido`), `resenas.json`, `sesiones.json` (cliente, prestador y administrador) y `revisiones.json` (cola con documentos de imagen `web/src/mocks/archivos/documento-ficticio-*.svg` que dicen "DOCUMENTO FICTICIO")
- [x] T012 Implementar el motor de simulación en `web/src/app/core/mocks/mock-api.interceptor.ts`: tabla de rutas (método + patrón) a manejadores por área, solo para las áreas en `true` de `environment.mocks`, latencia de 300 ms, estado en memoria con signals y guardado en `localStorage` del progreso del alta; registrarlo en `web/src/app/app.config.ts`; prueba en `mock-api.interceptor.spec.ts`
- [x] T013 [P] Crear el interceptor de errores en `web/src/app/core/http/errores.interceptor.ts` que convierte `ProblemDetails` (incluido `errors` por campo y 429 con `Retry-After`) en mensajes claros en español, y el servicio de avisos en `web/src/app/shared/ui/avisos.ts`
- [x] T014 Crear `SesionService` en `web/src/app/core/sesion/sesion.ts` (signals `usuario`, `roles`, `esCliente`, `esPrestador`, `esAdmin`; carga con `GET /cuentas/yo`) y los guards `conSesion` y `conRol(rol)` en `web/src/app/core/sesion/guards.ts`, con pruebas en `guards.spec.ts` (sin sesión redirige a `/entrar?volver=…`; rol incorrecto muestra "No tienes permiso")
- [x] T015 Crear el armazón en `web/src/app/core/layout/shell.ts` (barra superior y navegación inferior: Buscar, Mensajes, Mi cuenta), las rutas perezosas por funcionalidad en `web/src/app/app.routes.ts` y el selector de rol simulado `web/src/app/core/mocks/selector-rol.ts`, visible solo con mocks activos
- [x] T016 [P] Crear los componentes de UI en `web/src/app/shared/ui/`: `boton.ts`, `campo.ts` (etiqueta, ayuda y error ligados con `aria-describedby`), `tarjeta.ts`, `estrellas.ts` (solo lectura, con `aria-label` "4.5 de 5"), `insignia.ts`, `estado-vacio.ts` y `cargando.ts`
- [x] T017 [P] Crear el pipe `mxn` en `web/src/app/shared/pipes/mxn.ts` (de centavos a pesos: `3900` → "$39.00 MXN") con prueba en `mxn.spec.ts`
- [x] T018 [P] Crear `web/src/app/shared/subida/comprimir-imagen.ts` (canvas, máximo 1600 px, WebP o JPEG calidad 0.8; research R4) y el componente `web/src/app/shared/subida/subida-archivo.ts` (valida video de máximo 90 s leyendo sus metadatos y máximo 60 MB, muestra progreso y permite reintentar sin perder el formulario), con pruebas en `comprimir-imagen.spec.ts`

### Pantallas de US1 (alta y verificación)

- [x] T019 [P] [US1] Crear las pantallas de cuenta en `web/src/app/features/cuenta/`: `registro.ts` (correo, contraseña, nombre completo, fecha de nacimiento, rol Cliente o Prestador y aceptación del aviso de privacidad y los términos con su versión), `verificar-codigo.ts` (6 dígitos), `entrar.ts` y `recuperar.ts` (pedir código y nueva contraseña), con su manejador `web/src/app/core/mocks/handlers/cuentas.ts`
- [x] T020 [US1] Crear el contenedor del alta en `web/src/app/features/alta-prestador/alta.ts` y `alta-prestador.routes.ts`: 10 pasos (1 Datos personales, 2 Oficios y experiencia, 3 Tarifas de referencia, 4 Zona de cobertura, 5 Disponibilidad, 6 Descripción, 7 Consentimiento de datos sensibles, 8 Documentos, 9 Video y portafolio, 10 Revisión y envío), barra de avance, guardado con `PATCH /prestadores/yo` al salir de cada paso y regreso al `pasoAlta` guardado
- [x] T021 [P] [US1] Crear los pasos 1 a 6 en `web/src/app/features/alta-prestador/pasos/` (`datos-personales.ts`, `oficios.ts`, `tarifas.ts`, `cobertura.ts`, `disponibilidad.ts`, `descripcion.ts`) con estas validaciones de data-model.md: NombreCompleto "3–120 caracteres"; AniosExperiencia "0–60"; TarifaMin ≤ TarifaMax en centavos con UnidadTarifa `Hora`, `Dia` o `Servicio`; al menos un municipio de cobertura; Descripcion "0–600 caracteres"; y vista previa del nombre público ("Así te verán: Juan P.")
- [x] T022 [P] [US1] Crear los pasos 7 a 10 en `web/src/app/features/alta-prestador/pasos/`: `consentimiento.ts` (aceptación expresa antes de habilitar documentos, FR-007), `documentos.ts` (obligatorios `IneFrente`, `IneReverso`, `Selfie` y `ComprobanteDomicilio`; opcional `CartaAntecedentes`; fecha de expedición obligatoria para el comprobante, de máximo 3 meses, y para la carta), `video-portafolio.ts` (1 video de máximo 90 s y hasta 20 fotos con descripción de máximo 200 caracteres) y `revision-envio.ts` (muestra la lista de faltantes del 422 y el motivo si es menor de 18 años)
- [x] T023 [US1] Crear el manejador `web/src/app/core/mocks/handlers/prestadores.ts`: `GET`/`PATCH /prestadores/yo`, `url-subida` simulada, `documentos`, `portafolio` y `enviar-revision` (422 con faltantes y con el caso de menor de edad)
- [x] T024 [P] [US1] Crear `web/src/app/features/alta-prestador/estado-perfil.ts`: estado del perfil (Borrador, En revisión, Requiere corrección, Aprobado, Rechazado) con el motivo por documento y un botón para volver a subir solo el documento señalado
- [x] T025 [P] [US1] Crear la revisión del administrador en `web/src/app/features/admin/revisiones/cola.ts` y `detalle-revision.ts` (visor de documentos ficticios; decisión Aprobar, Requiere corrección o Rechazar con motivo obligatorio salvo al aprobar; "Aprobar perfil" deshabilitado mientras falten documentos aprobados; moderación de fotos y video), con su manejador `web/src/app/core/mocks/handlers/admin.ts`

### Pantallas de US2 (búsqueda y perfil público)

- [x] T026 [P] [US2] Crear `web/src/app/features/busqueda/inicio.ts`: buscador por oficio y municipio, botón "Usar mi ubicación" (simulado) y oficios frecuentes
- [x] T027 [P] [US2] Crear `web/src/app/features/busqueda/resultados.ts`: filtros de calificación mínima, insignia y tarifa máxima guardados en la URL, lista de `TarjetaPrestador`, botón "Ver más" y estado vacío
- [x] T028 [P] [US2] Crear `web/src/app/features/busqueda/perfil-publico.ts`: nombre público, colonia o municipio, oficios, experiencia, tarifas, disponibilidad, video, fotos, insignias, promedios por eje, reseñas paginadas y botón "Contactar"; sin teléfono, correo, dirección ni documentos; página "Este perfil no está disponible" para perfiles no aprobados
- [x] T029 [US2] Crear el manejador `web/src/app/core/mocks/handlers/busqueda.ts`: catálogos, `GET /prestadores` (solo `Aprobado`, con filtros y paginación) y `GET /prestadores/{id}` (404 si no está aprobado)

### Pantallas de US3 y US4 (pago de acceso y pase)

- [x] T030 [P] [US3] Crear `web/src/app/features/contacto/opciones.ts`: sin sesión manda a registro con regreso; muestra $39 MXN (chat y teléfono por 30 días, crédito si no responde en 48 h), $129 MXN del pase, créditos disponibles, el aviso "Pagas por acceso y contacto, no por el trabajo. No garantiza la contratación." con su versión, la advertencia de que OXXO y SPEI pueden tardar en acreditarse y el caso de acceso vigente ("Ya tienes acceso; no se cobra de nuevo")
- [x] T031 [P] [US3] Crear `web/src/app/features/contacto/checkout-simulado.ts` (elige tarjeta aprobada, OXXO pendiente o rechazo), `resultado-pago.ts` (aprobado, pendiente con instrucciones, expirado) y `mis-accesos.ts` (estado y vencimiento; teléfono visible solo si el acceso está `Activo`)
- [x] T032 [US3] Crear el manejador `web/src/app/core/mocks/handlers/accesos.ts`: `GET /accesos/opciones`, `POST /accesos`, `GET /accesos/yo`, `GET /accesos/{id}/contacto` (403 si no está activo) y `GET /pagos/{id}`
- [x] T033 [P] [US4] Crear `web/src/app/features/contacto/pase.ts`: compra como pago único o con renovación automática (solo tarjeta), contactos disponibles, oferta con descuento (por ejemplo, $90 MXN tras un contacto de $39) y "Cancelar renovación" en un clic con el aviso de que conserva el acceso hasta el fin del periodo; ampliar `handlers/accesos.ts` con `POST /pases` y `POST /pases/{id}/cancelar-renovacion`

### Cierre del prototipo

- [x] T034 Crear `web/src/app/features/cuenta/mi-cuenta.ts`: mis contactos, mi pase, "Quiero ofrecer mis servicios" (agrega el rol de prestador), privacidad (pendiente) y cerrar sesión
- [x] T035 [P] Escribir `web/e2e/prototipo.spec.ts` en Pixel 7 e iPhone 14 (los puntos 1 a 4 quedaron en `web/e2e/*.prototipo.spec.ts` y este archivo los enlaza y cubre Mi cuenta) con los puntos 1 a 4 de la Etapa 2 de quickstart.md: alta interrumpida y retomada, búsqueda sin teléfono ni correo en el DOM, "Contactar" → registro → opciones con el aviso, y aprobación con motivo en la cola del administrador
- [x] T036 Documentar en `specs/001-serviya-mvp/quickstart.md` cómo probar en un celular Android: `npm run serve:prototipo` (compila y sirve en el puerto 4000) con reenvío de puerto desde `chrome://inspect` (así el sitio es `localhost` y se puede instalar como PWA sin HTTPS), e instalar la app en la pantalla de inicio
- [ ] T037 Ejecutar `typescript-reviewer` sobre `web/`, revisar contraste, etiquetas y áreas táctiles de 44 px, y registrar en `docs/PROGRESO.md` los ajustes que pida el usuario tras probar el prototipo

**Checkpoint**: el usuario aprueba el prototipo en su celular. Sin esta aprobación no se empieza el backend.

---

## Fase 3: Preparación del backend (Etapa 3)

**Purpose**: solución de .NET, base de datos local y proyectos de pruebas.

**⚠️ Requisito previo**: Docker Desktop con WSL 2 instalado por el usuario (PostgreSQL local, RustFS y
Testcontainers).

- [x] T038 Crear `api/ServiYa.sln` con `api/src/ServiYa.Api` (web), `api/src/ServiYa.Comun` (biblioteca) y los 10 módulos en `api/src/Modulos/` (Cuentas, Prestadores, Documentos, Busqueda, Pagos, Chat, Contrataciones, Calificaciones, Reportes, Administracion); `api/Directory.Build.props` (`net10.0`, `Nullable` y `ImplicitUsings` activados, `TreatWarningsAsErrors`, `AnalysisLevel` `latest-recommended`) y `api/Directory.Packages.props` con las versiones de research.md
- [x] T039 [P] Crear `docker-compose.yml` en la raíz con `postgis/postgis:17` (base `serviya`, volumen y script `docker/postgres/init.sql` que crea el rol propietario `serviya_owner` y el rol de la app `serviya_app`) y RustFS 1.0.4 (S3; MinIO dejó de publicar imágenes) con un contenedor `amazon/aws-cli` que crea los buckets `serviya-privado` y `serviya-publico`. Probado el 2026-11-08: roles y permisos, PostGIS 3.7, buckets con lectura anónima solo en el público
- [x] T040 [P] Crear `api/tests/ServiYa.UnitTests`, `api/tests/ServiYa.IntegrationTests` y `api/tests/ServiYa.ContractTests` con xUnit v3 sobre Microsoft.Testing.Platform, AwesomeAssertions 9.6.0, NSubstitute, `Microsoft.Extensions.TimeProvider.Testing`, `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers.PostgreSql 4.16.0 y `Microsoft.Testing.Extensions.CodeCoverage` (`coverlet.collector` no funciona con Microsoft.Testing.Platform; Testcontainers.Minio se quitó: la imagen ya no existe)
- [x] T041 [P] Crear las opciones con validación al inicio (`ValidateOnStart`) en `api/src/ServiYa.Comun/Configuracion/` (`StorageOptions`, `MercadoPagoOptions`, `EmailOptions`, `WebPushOptions`), `api/src/ServiYa.Api/appsettings.json` sin secretos y `dotnet user-secrets init` con las claves de quickstart.md; `scripts/configurar-desarrollo.ps1` genera `.env` y los user-secrets
- [x] T042 [P] Crear `.github/workflows/ci.yml`: compilar y probar `api/` (Testcontainers en `ubuntu-latest`) y ejecutar `lint`, pruebas y `build` de `web/` (pasos probados en local; corre en GitHub al subir)
- [x] T043 Agregar a la sección "Comandos" de `CLAUDE.md` los comandos del backend (`docker compose up -d`, `dotnet ef database update`, `dotnet run --project src/ServiYa.Api`, `dotnet test`)

---

## Fase 4: Fundamentos (bloquean todas las historias)

**Purpose**: host de la API, piezas comunes, cuentas, catálogos e infraestructura de pruebas.

**⚠️ CRITICAL**: ninguna historia empieza antes de terminar esta fase.

### Host y piezas comunes

- [x] T044 Configurar `api/src/ServiYa.Api/Program.cs`: `AddProblemDetails` con manejador de excepciones y páginas de estado, `AddOpenApi` (documento `/openapi/v1.json` en desarrollo), grupo de rutas `/api/v1`, `TimeProvider.System` como singleton y la interfaz `api/src/ServiYa.Comun/Modulos/IModulo.cs` (`AgregarServicios`, `MapearEndpoints`) que registra cada módulo
- [x] T045 Crear la base de persistencia en `api/src/ServiYa.Comun/Datos/`: `ConvencionNombres.cs` (tablas y columnas en snake_case sin paquetes extra), `MarcasTiempoInterceptor.cs` (`created_at` y `updated_at` con `TimeProvider`), claves `Guid.CreateVersion7()` y `UseNetTopologySuite()` en el registro de cada `DbContext` (marcas como propiedades sombra `CreadoEn`/`ActualizadoEn`; pruebas del modelo sin conexión)
- [x] T046 Crear la infraestructura de pruebas en `api/tests/ServiYa.IntegrationTests/Infraestructura/`: `ServiYaFactory.cs` (`WebApplicationFactory` con un contenedor `postgis/postgis:17` compartido por colección, migraciones aplicadas, `FakeTimeProvider`, correo falso que guarda los códigos y almacenamiento en un contenedor RustFS de Testcontainers (`ContainerBuilder` genérico)) y `ClientesDePrueba.cs` (crea usuarios por rol y devuelve un `HttpClient` con cookie y XSRF). *Hecho: `ContenedoresServiYa` (PostGIS con el `init.sql` del proyecto y RustFS, compartidos por ensamblado) y `ServiYaFactory` (base propia por clase, migraciones aplicadas, `FakeTimeProvider`, correo en memoria, planificador manual). `ClientesDePrueba.cs` se escribe con T059 y T061, cuando existan la cookie y los endpoints*
- [x] T047 [P] Escribir primero las pruebas de `ContactMasker` (TDD) en `api/tests/ServiYa.UnitTests/Privacidad/ContactMaskerTests.cs` con el catálogo `CasosEvasion.cs`: teléfonos con espacios, guiones, puntos, paréntesis y `+52`; dígitos escritos con palabras ("cinco cinco uno dos…") y mezclados; correos con "arroba" y "punto com"; `wa.me`, `t.me`, Instagram, Facebook y Telegram; "WhatsApp" seguido de números; y falsos positivos que NO se ocultan (`$1,500`, `12/11/2026`, "3 horas", "20 años de experiencia")
- [x] T048 Implementar `api/src/ServiYa.Comun/Privacidad/ContactMasker.cs` (servicio puro, research R8) hasta que pasen las pruebas de T047
- [ ] T049 [P] Crear `IBitacora` en `api/src/ServiYa.Comun/Bitacora/IBitacora.cs` y su implementación en `api/src/Modulos/ServiYa.Administracion/`: entidad `RegistroBitacora` en el esquema `admin` (`Id`, `ActorId`, `Accion`, `EntidadTipo`, `EntidadId`, `Detalle` jsonb "**sin datos personales**", `Ip`, `CreadoEn`), migración con `REVOKE UPDATE, DELETE ON admin.registro_bitacora FROM serviya_app` y prueba de integración que confirma que un `UPDATE` con `serviya_app` falla
- [x] T050 [P] Crear `IAlmacenArchivos` en `api/src/ServiYa.Comun/Almacenamiento/` con implementación para R2 sobre `AWSSDK.S3` (`ServiceURL`, `ForcePathStyle`): URL de subida PUT de 10 min, URL de lectura de 5 min, copiar, eliminar y leer los primeros bytes; `ValidadorFirmaArchivo.cs` (JPEG, PNG, WebP, PDF, MP4 y MOV por sus primeros bytes) con pruebas unitarias, y prueba de integración contra RustFS (integración en `Almacenamiento/AlmacenRustFsTests.cs`)
- [x] T051 [P] Crear `IEmailSender` en `api/src/ServiYa.Comun/Correo/` con `ConsolaEmailSender` (desarrollo; guarda los últimos correos y los expone en `GET /dev/correos` solo en el entorno `Development`) y `ResendEmailSender` (`HttpClient`, sin SDK), con plantillas de texto en español en `Correo/Plantillas/`
- [x] T052 [P] Crear las tareas programadas en `api/src/ServiYa.Comun/Tareas/`: `ITareaProgramada.cs` (`Nombre`, `EjecutarAsync`) y `PlanificadorTareas.cs` (`BackgroundService` con `PeriodicTimer` de 5 min, `pg_try_advisory_lock` por tarea, error de una tarea registrado sin detener las demás), con prueba de que dos instancias no ejecutan la misma tarea a la vez (integración en `Tareas/CandadoPostgresTests.cs`)
- [x] T053 [P] Crear los límites de uso en `api/src/ServiYa.Api/Seguridad/LimitesUso.cs`: política `login-codigos` (5 por minuto por IP) y `mensajes` (30 por minuto por usuario), con respuesta 429 en `ProblemDetails` y `Retry-After`
- [x] T054 [P] Crear `api/src/ServiYa.Api/Seguridad/Encabezados.cs`: HTTPS obligatorio, HSTS, CORS solo para `https://serviya.mx` y `http://localhost:4200` con credenciales, `X-Content-Type-Options` y `Referrer-Policy`
- [x] T055 [P] Configurar el registro en JSON (`AddJsonConsole`) sin cuerpos de petición ni cadenas de consulta, con prueba de que un registro de petición no contiene datos personales (principio I), en `api/src/ServiYa.Api/Program.cs`

### Cuentas (FR-001 a FR-004)

- [x] T056 Crear el dominio de Cuentas en `api/src/Modulos/ServiYa.Cuentas/Dominio/`: `Usuario : IdentityUser<Guid>` (Email "Único, verificado antes de usar la cuenta"; PhoneNumber "Formato E.164 +52; privado"; NombreCompleto "3–120 caracteres"; FechaNacimiento `date`; Estado `Activo`, `Suspendido`, `Eliminado`; EliminadoEn), `CodigoVerificacion` (Proposito `VerificarCorreo` o `RecuperarContrasena`, `CodigoHash`, ExpiraEn de 10 min, Intentos "máximo 5", `UsadoEn?`), `Consentimiento` (Documento `AvisoPrivacidad`, `Terminos`, `PoliticaReembolsos` o `DatosSensibles`; `Version`, `AceptadoEn`, `Ip`; "Solo se agregan registros; nunca se modifican") y `Bloqueo` ("Clave única (bloqueador, bloqueado)")
- [x] T057 Crear `CuentasDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>` en `api/src/Modulos/ServiYa.Cuentas/Datos/` con el esquema `cuentas` y la migración inicial (migración `Inicial` aplicada con `serviya_owner`)
- [ ] T058 Escribir primero las pruebas de cuentas y permisos (TDD) en `api/tests/ServiYa.IntegrationTests/Cuentas/RegistroYSesionTests.cs` (registro → código → verificación → cookie; login sin verificar rechazado; código vencido a los 10 min; bloqueo tras 5 intentos; máximo 3 reenvíos por hora; petición sin `X-XSRF-TOKEN` rechazada; `esMayorDeEdad` falso con menos de 18 años) y `api/tests/ServiYa.IntegrationTests/Seguridad/PermisosPorRolTests.cs` (matriz endpoint × rol: anónimo, Cliente, Prestador, Admin), que se amplía en cada historia
- [ ] T059 Configurar Identity y la sesión en `api/src/Modulos/ServiYa.Cuentas/CuentasModulo.cs`: `AddIdentityCore<Usuario>().AddRoles`, cookie `serviya.sesion` HttpOnly, `Secure`, `SameSite=Lax` y `Domain` del dominio padre, 14 días con renovación, respuestas 401/403 sin redirecciones, contraseña de mínimo 8 caracteres, roles semilla `Cliente`, `Prestador` y `Admin`; antiforgery con cookie `XSRF-TOKEN` legible por JavaScript y encabezado `X-XSRF-TOKEN`, validado en todo `POST`, `PUT`, `PATCH` y `DELETE` salvo el webhook
- [x] T060 [P] Crear `api/src/Modulos/ServiYa.Cuentas/Servicios/ServicioCodigos.cs` (6 dígitos con `RandomNumberGenerator`, guardado como hash, 10 min, 5 intentos, máximo 3 envíos por hora) con pruebas en `api/tests/ServiYa.UnitTests/Cuentas/ServicioCodigosTests.cs`
- [ ] T061 Crear `api/src/Modulos/ServiYa.Cuentas/Endpoints/CuentasEndpoints.cs`: `registro` (registra los consentimientos con versión e IP), `verificar-correo`, `reenviar-codigo`, `login`, `logout`, `recuperar` (siempre 204 para no revelar si el correo existe), `restablecer`, `yo`, `yo/roles/prestador` y `xsrf`, con validación en `ValidationProblem` y la política `login-codigos` **Revisión de seguridad (Etapa 3)**: al guardar la verificación de un código, tratar `DbUpdateConcurrencyException` (token `xmin` de `CodigoVerificacion`) como intento rechazado, para que peticiones en paralelo no se salten los 5 intentos.
- [ ] T062 Crear `ICuentasService` en `api/src/Modulos/ServiYa.Cuentas/Servicios/ICuentasService.cs` para los demás módulos (`EsMayorDeEdad`, `TelefonoDe`, `EstaBloqueado`, `RegistrarConsentimiento`, `NombrePublicoDe`)
- [ ] T063 Crear el filtro de cuenta suspendida en `api/src/ServiYa.Api/Seguridad/CuentaSuspendidaFiltro.cs`: 403 con `type: "/errores/cuenta-suspendida"` y el motivo, salvo en `GET /cuentas/yo` y en la aclaración
- [ ] T064 Crear el comando `dotnet run --project src/ServiYa.Api -- crear-admin --email <correo>` (asigna `Admin` a un usuario verificado ya existente) y los endpoints `POST`/`DELETE /admin/usuarios/{id}/roles/admin` en `api/src/Modulos/ServiYa.Administracion/Endpoints/RolesEndpoints.cs`, con bitácora `OtorgarRolAdmin`/`RetirarRolAdmin` y sin permitir retirar el rol al último administrador (FR-003)

### Catálogos (insumo de US1 y US2)

- [ ] T065 Crear en `api/src/Modulos/ServiYa.Busqueda/` las entidades `Oficio` (`Id`, `Nombre`, `Slug`, `Icono`, `Activo`) y `Municipio` (`Id`, `ClaveInegi`, `Nombre`, `Estado`, `Poligono` geography(MultiPolygon), `Centroide`) en el esquema `catalogos`, índice GIST sobre `poligono`, semilla de los 12 oficios y migración
- [ ] T066 Crear el importador `api/src/Modulos/ServiYa.Busqueda/Datos/Semillas/importar-municipios.sql` que carga un GeoJSON del Marco Geoestadístico de INEGI con `ST_GeomFromGeoJSON` (sin paquetes extra); en desarrollo usar los municipios de una zona de prueba hasta que se defina la ciudad del piloto (pendiente abierto)
- [ ] T067 Crear `api/src/Modulos/ServiYa.Busqueda/Endpoints/CatalogosEndpoints.cs`: `GET /catalogos/oficios`, `GET /catalogos/municipios` (sin polígono) y `POST /catalogos/municipios/ubicar` con `{ lat, lng }` en el cuerpo (punto dentro de polígono; 404 si no cae en la zona). El contrato ya se cambió de `GET ?lat&lng` a `POST` en la Etapa 2, porque la ubicación es un dato personal y no debe viajar en la URL (principio I). Pruebas en `api/tests/ServiYa.IntegrationTests/Busqueda/CatalogosTests.cs`; crear `ICatalogosService` para Prestadores

### Conexión base del frontend

- [ ] T068 Agregar `openapi-typescript` como dependencia de desarrollo con el script `generar:api` que escribe `web/src/app/core/api/esquema.d.ts` desde `/openapi/v1.json`, y convertir `web/src/app/core/api/modelos.ts` en alias de esos tipos; agregar en cada `*-api.ts` guardas de formato (`esSesion`, `esPago`, `esOpcionesAcceso`, …) que lancen `ErrorApi` si la respuesta no coincide (hallazgo H2 de la revisión de la Etapa 2). *Las guardas de formato se escriben al conectar cada área (T070, T085, T095, T110…), junto con los tipos generados: escribirlas antes sobre los modelos a mano obligaría a rehacerlas*
- [x] T069 Configurar `provideHttpClient(withFetch(), withInterceptors([...]))` en `web/src/app/app.config.ts` con un interceptor propio `web/src/app/core/http/xsrf.interceptor.ts` que agrega `X-XSRF-TOKEN` y `withCredentials` también a URLs absolutas de la API (el soporte XSRF de Angular omite URLs absolutas y en producción la API está en `api.serviya.mx`), la llamada a `GET /cuentas/xsrf` con `provideAppInitializer` y el proxy de desarrollo `web/proxy.conf.json` para `/api` y `/hubs` (el XSRF de Angular se apaga con `withNoXsrfProtection()`; el token se pide al arrancar sin bloquear y, si falta, antes de la primera petición que modifica datos)
- [ ] T070 Conectar las pantallas de `web/src/app/features/cuenta/` a la API real (`mocks.cuentas: false`) y escribir `web/e2e/cuentas.spec.ts` (registro, código leído de `GET /dev/correos`, verificación, entrar y salir)

**Checkpoint**: la base está lista; las historias pueden empezar.

---

## Fase 5: User Story 1 — Alta y verificación del prestador (Priority: P1) 🎯 MVP

**Goal**: el prestador completa el alta por pasos con documentos ficticios, el administrador los
revisa y el perfil se publica solo tras la aprobación (Etapas 3 y 4).

**Independent Test**: registrar un prestador con documentos ficticios, pedir corrección de la INE,
reenviarla y aprobar; el perfil queda `Aprobado` y la bitácora tiene `VerDocumento` y `AprobarPerfil`
(quickstart, escenarios 1 y 2).

### Tests for User Story 1 ⚠️ (escribir primero; deben fallar)

- [ ] T071 [P] [US1] Pruebas de la máquina de estados en `api/tests/ServiYa.UnitTests/Prestadores/PerfilPrestadorEstadoTests.cs`: las transiciones de data-model.md; `enviar` exige datos completos, INE frente y reverso, selfie, comprobante de domicilio, consentimiento de datos sensibles, 18 años o más y al menos una cobertura; `aprobar` exige que **todos** los documentos obligatorios estén en `Aprobado`
- [ ] T072 [P] [US1] Pruebas del nombre público en `api/tests/ServiYa.UnitTests/Prestadores/NombrePublicoTests.cs`: nombre de pila + inicial del primer apellido ("Juan Pérez López" → "Juan P."; "Ana Ruiz" → "Ana R."; con 3 palabras o más se toma la penúltima)
- [ ] T073 [P] [US1] Pruebas de Documentos (módulo crítico) en `api/tests/ServiYa.IntegrationTests/Documentos/DocumentosTests.cs`: sin consentimiento no hay URL de subida; TipoContenido solo `image/jpeg`, `image/png`, `image/webp`, `application/pdf` y TamanoBytes "≤ 10 MB"; archivo con firma falsa rechazado; una versión nueva deja la anterior en `Reemplazado` y hay un solo documento vigente por tipo; cliente, visitante u otro prestador no pueden leer un documento (403/404); `POST /admin/documentos/{id}/ver` devuelve una URL de 5 min y registra `VerDocumento`; rechazar o pedir corrección sin motivo devuelve 400
- [ ] T074 [P] [US1] Pruebas del alta en `api/tests/ServiYa.IntegrationTests/Prestadores/AltaPrestadorTests.cs`: guardado parcial por paso y regreso al `pasoAlta`; `enviar-revision` con faltantes → 422 con la lista; menor de edad → 422 con el motivo; envío completo → `EnRevision` y aparece en `GET /admin/revisiones`; corrección de la INE → solo ese documento pendiente; aprobar con documentos pendientes → 422; aprobación → `Aprobado`, bitácora `AprobarPerfil` con el actor e insignia `AntecedentesPresentados` solo si la carta está aprobada; ampliar `PermisosPorRolTests.cs` con estos endpoints

### Implementation for User Story 1

- [ ] T075 [P] [US1] Crear el dominio en `api/src/Modulos/ServiYa.Prestadores/Dominio/`: `PerfilPrestador` (UsuarioId 1:1; NombrePublico calculado; Descripcion "0–600 caracteres; pasa por `ContactMasker`"; Direccion "**Privada**"; ColoniaBase pública; PuntoBase geography(Point) privado; Disponibilidad jsonb; Estado; PasoAlta; Insignias `IdentidadVerificada`, `AntecedentesPresentados`; promedios numeric(3,2); TotalResenas; PuntajeRanking) con los métodos `Enviar`, `Aprobar`, `PedirCorreccion`, `Reenviar`, `Rechazar`, `Suspender`, `Levantar` y `Eliminar`; `OficioPrestador` (AniosExperiencia "0–60", TarifaMin y TarifaMax en centavos, UnidadTarifa `Hora`, `Dia`, `Servicio`, "Clave única (prestador, oficio)"); `CoberturaPrestador` ("Clave única"); `ElementoPortafolio` (Tipo `Foto` o `Video`, claves privada y pública, Descripcion "≤ 200, con `ContactMasker`", Orden, EstadoModeracion `Pendiente`, `Aprobado`, `Rechazado`, MotivoRechazo)
- [ ] T076 [US1] Crear `PrestadoresDbContext` en `api/src/Modulos/ServiYa.Prestadores/Datos/` (esquema `prestadores`) con los índices `perfil (estado, puntaje_ranking DESC)`, `cobertura (municipio_id)`, `oficio_prestador (oficio_id)` y GIST sobre `punto_base`, y su migración
- [ ] T077 [P] [US1] Crear el dominio y los datos de Documentos en `api/src/Modulos/ServiYa.Documentos/`: `Documento` (Tipo `IneFrente`, `IneReverso`, `Selfie`, `ComprobanteDomicilio`, `CartaAntecedentes`; ClaveObjeto "GUID sin datos personales"; TipoContenido; TamanoBytes; Estado `Cargado`, `Aprobado`, `RequiereCorreccion`, `Rechazado`, `Vencido`, `Reemplazado`; Motivo "Obligatorio en rechazo o corrección"; FechaExpedicion; VenceEn "Carta: expedición + 12 meses"; RevisadoPor y RevisadoEn) y `ArchivoSubido` (archivo pendiente de registrar: destino, tipo de contenido, tamaño declarado y vencimiento de 10 min), `DocumentosDbContext` (esquema `documentos`) y migración
- [ ] T078 [US1] Crear `IArchivosService` en `api/src/Modulos/ServiYa.Documentos/Servicios/ServicioArchivos.cs` (lo usan Prestadores, Chat y Calificaciones): emite la URL de subida, y al registrar comprueba que el objeto existe, su tamaño real y su firma de archivo
- [ ] T079 [US1] Crear `api/src/Modulos/ServiYa.Prestadores/Endpoints/AltaEndpoints.cs`: `GET`/`PATCH /prestadores/yo` (guardado por paso con las validaciones de T075; Descripcion pasada por `ContactMasker`; PuntoBase = ubicación del navegador redondeada a 3 decimales o, si no la hay, el centroide del primer municipio de cobertura), `POST /prestadores/yo/consentimiento-sensible`, `POST /prestadores/yo/archivos/url-subida` y `POST /prestadores/yo/enviar-revision`; `POST /cuentas/yo/roles/prestador` crea el perfil en `Borrador`
- [ ] T080 [US1] Crear `api/src/Modulos/ServiYa.Documentos/Endpoints/DocumentosPropiosEndpoints.cs`: `POST /prestadores/yo/documentos` con FechaExpedicion "Obligatoria para el comprobante (máximo 3 meses) y la carta" y la regla de reemplazo **Revisión de seguridad (Etapa 3)**: después de la subida, leer con `IAlmacenArchivos.LeerInicioAsync`, validar con `ValidadorFirmaArchivo.EsValido` (tipo, firma y tamaño máximo; si no pasa, borrar el objeto) y fijar con `ConservarAsync` (copia con ETag a la clave definitiva y borra la de subida); guardar solo la clave definitiva.
- [ ] T081 [US1] Crear `api/src/Modulos/ServiYa.Prestadores/Endpoints/PortafolioEndpoints.cs`: `POST`/`DELETE /prestadores/yo/portafolio` con "Máximo 20 fotos y 1 video (90 s como máximo, 60 MB)" (la duración se valida en el navegador; el servidor valida tamaño y firma) y descripción con `ContactMasker` **Revisión de seguridad (Etapa 3)**: mismo flujo de validación y `ConservarAsync` que T080 antes de registrar la foto o el video.
- [ ] T082 [US1] Crear `api/src/Modulos/ServiYa.Administracion/Endpoints/RevisionesEndpoints.cs`: `GET /admin/revisiones?estado=EnRevision`, `GET /admin/prestadores/{id}` (documentos sin URLs), `POST /admin/documentos/{id}/ver` (URL de 5 min y bitácora `VerDocumento`), `POST /admin/documentos/{id}/decision`, `POST /admin/prestadores/{id}/decision` (`Aprobar`, `RequiereCorreccion` o `Rechazar`; 422 si faltan documentos aprobados) y `POST /admin/portafolio/{id}/decision` (al aprobar copia el objeto de `serviya-privado` a `serviya-publico`); cada acción en la bitácora sin datos personales en `Detalle`
- [ ] T083 [US1] Crear `INotificador` en `api/src/ServiYa.Comun/Notificaciones/INotificador.cs` con el canal de correo (push se agrega en US5) y avisar al prestador de cada cambio de estado con su motivo (FR-011): en revisión, requiere corrección, aprobado y rechazado
- [ ] T084 [US1] Crear la tarea `api/src/Modulos/ServiYa.Documentos/Tareas/AntecedentesPorVencer.cs` (FR-013): aviso por correo 30 días antes de `VenceEn` y, al vencer, documento en `Vencido` y retiro de la insignia `AntecedentesPresentados`, con prueba usando `FakeTimeProvider`
- [ ] T085 [US1] Conectar `web/src/app/features/alta-prestador/` y `web/src/app/features/admin/revisiones/` a la API real (`mocks.prestadores` y `mocks.admin` en `false`): subida directa con `PUT` a `urlSubida` con reintento (R3) y visor de documentos que carga la URL firmada sin guardarla ni ofrecer descarga; `Subidor` valida que `urlSubida` sea `https` del dominio de R2 configurado (hallazgo M2)
- [ ] T086 [US1] Crear las imágenes ficticias en `web/e2e/fixtures/documentos-ficticios/` y escribir `web/e2e/alta-prestador.spec.ts`: registro de prestador, 10 pasos con interrupción y regreso, envío, corrección de la INE pedida por el administrador, reenvío y aprobación
- [ ] T087 [US1] Cierre de las Etapas 3 y 4: ejecutar `security-reviewer`, `csharp-reviewer` y `database-reviewer` sobre Cuentas, Prestadores, Documentos y Administración, y confirmar al menos 80% de cobertura en Documentos y permisos con `dotnet test --collect:"XPlat Code Coverage"`

**Checkpoint**: US1 funciona de punta a punta con documentos ficticios.

---

## Fase 6: User Story 2 — Búsqueda y perfiles públicos (Priority: P1)

**Goal**: cualquier persona busca por oficio y zona y ve perfiles aprobados sin datos de contacto
(Etapa 5).

**Independent Test**: con prestadores ficticios aprobados, buscar "fontanero" en un municipio; solo
aparecen los aprobados que cubren esa zona y ninguna respuesta contiene datos privados (quickstart,
escenario 10).

### Tests for User Story 2 ⚠️

- [ ] T088 [P] [US2] Prueba de contrato en `api/tests/ServiYa.ContractTests/DtosPublicosTests.cs`: por reflexión, `TarjetaPrestador`, `PerfilPublico` y `ResenaPublica` no tienen propiedades privadas (teléfono, correo, dirección, punto base, fecha de nacimiento, nombre completo, documentos, claves de objeto privadas); además, las llaves del JSON real de `GET /prestadores` y `GET /prestadores/{id}` están en una lista permitida (SC-006)
- [ ] T089 [P] [US2] Pruebas de búsqueda en `api/tests/ServiYa.IntegrationTests/Prestadores/BusquedaTests.cs`: solo perfiles `Aprobado` con usuario `Activo` que cubren el municipio; filtros de calificación mínima, insignia y tarifa máxima; orden por `PuntajeRanking` y cercanía; paginación; `GET /prestadores/{id}` → 404 para `Borrador`, `EnRevision`, `RequiereCorreccion`, `Rechazado`, `Suspendido` y `Eliminado`
- [ ] T090 [P] [US2] Pruebas del ranking en `api/tests/ServiYa.UnitTests/Prestadores/CalculadoraRankingTests.cs`: `(C·m + Σ calificaciones) / (C + n)` con `C = 5`; una sola reseña de 5 no supera a 20 reseñas de 4.8 (US6, escenario 5)

### Implementation for User Story 2

- [ ] T091 [US2] Crear `api/src/Modulos/ServiYa.Prestadores/Servicios/CalculadoraRanking.cs` y el método `IPrestadoresService.RecalcularRanking(prestadorId)` (lo usa US6)
- [ ] T092 [US2] Crear `api/src/Modulos/ServiYa.Prestadores/Servicios/BusquedaPrestadores.cs`: la consulta vive en Prestadores porque usa sus tablas; Busqueda solo resuelve el municipio y su centroide con `ICatalogosService`; distancia con PostGIS desde `punto_base`, proyección directa al DTO `TarjetaPrestador` y paginación `{ items, total, pagina, tamano }`
- [ ] T093 [US2] Crear `api/src/Modulos/ServiYa.Prestadores/Endpoints/PublicoEndpoints.cs`: `GET /prestadores`, `GET /prestadores/{id}` (`PerfilPublico` con fotos del bucket público) con `Cache-Control: public, max-age=60`, y el DTO `ResenaPublica` con `GET /prestadores/{id}/resenas` devolviendo una página vacía hasta US6
- [ ] T094 [US2] Configurar SSR en `web/src/app/app.routes.server.ts`: `RenderMode.Server` para `/prestador/:id` y `/buscar`, `RenderMode.Client` para el resto; título, descripción y Open Graph con `Title` y `Meta` en `perfil-publico.ts`; estado 404 real con `RESPONSE_INIT` para perfiles no disponibles; presupuesto de JS inicial del perfil en `web/angular.json`
- [ ] T095 [US2] Conectar `web/src/app/features/busqueda/` a la API real (`mocks.busqueda: false`) y "Usar mi ubicación" con `navigator.geolocation` → `POST /catalogos/municipios/ubicar`
- [ ] T096 [US2] Escribir `web/e2e/busqueda.spec.ts`: buscar "Fontanero" en un municipio, abrir un perfil y comprobar que ni el DOM ni las respuestas de red contienen teléfonos, correos o direcciones

**Checkpoint**: US1 y US2 funcionan; la vitrina pública está lista.

---

## Fase 7: User Story 3 — Pago de acceso y desbloqueo de contacto (Priority: P1)

**Goal**: el cliente paga $39 MXN con tarjeta, OXXO o SPEI y, al confirmarlo Mercado Pago por
webhook, se desbloquean el chat y el teléfono (Etapa 6).

**Independent Test**: en modo de pruebas de Mercado Pago, pagar un contacto con tarjeta (se activa al
instante) y otro con OXXO (queda pendiente hasta el webhook `approved`); un webhook con firma alterada
devuelve 401 sin cambios (quickstart, escenarios 3, 4, 5 y 7).

**⚠️ Requisito previo**: cuenta de desarrollador de Mercado Pago con credenciales de prueba y clave
secreta del webhook, creada por el usuario.

### Tests for User Story 3 ⚠️ (módulo crítico: TDD)

- [ ] T097 [P] [US3] Pruebas de la firma en `api/tests/ServiYa.UnitTests/Pagos/ValidadorFirmaMpTests.cs`: HMAC-SHA256 sobre `id:{data.id};request-id:{x-request-id};ts:{ts};`; válida, inválida, `ts` alterado, encabezado ausente o mal formado
- [ ] T098 [P] [US3] Pruebas del webhook en `api/tests/ServiYa.IntegrationTests/Pagos/WebhookMercadoPagoTests.cs` con un `IMercadoPago` falso y respuestas grabadas en `api/tests/ServiYa.IntegrationTests/Pagos/RespuestasMp/*.json`: firma inválida → 401 sin cambios en la base; notificación duplicada (mismo `RequestId`) → 200 sin reprocesar; `pending` → `approved` activa el acceso; `rejected` → `Expirado`; segundo `approved` para un acceso activo → `Credito` `PagoDuplicado` y alerta; referencia vencida → `Expirado`; el estado siempre se toma de la consulta a Mercado Pago y no del cuerpo
- [ ] T099 [P] [US3] Pruebas de accesos en `api/tests/ServiYa.IntegrationTests/Pagos/AccesosTests.cs`: opciones con precios 3900 y 12900 y versión del aviso; `POST /accesos` `Individual` crea `Pago` `Creado` y acceso `Pendiente` con `checkoutUrl`; acceso vigente → no se cobra de nuevo (FR-025), incluido un caso de dos peticiones simultáneas contra el índice único parcial; el contacto 11 del día → 429 y alerta (FR-029); menor de 18 años → 403; prestador no aprobado → 422; vía `Credito` usa el crédito; `GET /accesos/{id}/contacto` → 403 salvo `Activo`; vencimiento a los 30 días; crédito tras 48 h sin respuesta (fijando `PrimerMensajeClienteEn` en la prueba); cambio de crédito por reembolso con `X-Idempotency-Key`; ampliar `PermisosPorRolTests.cs`

### Implementation for User Story 3

- [ ] T100 [P] [US3] Crear el dominio en `api/src/Modulos/ServiYa.Pagos/Dominio/`: `AccesoContacto` (Origen `Individual`, `Pase`, `Credito`; Estado `Pendiente`, `Activo`, `Vencido`, `Expirado`, `Reembolsado`; "VenceEn = ActivadoEn + 30 días"; `PrimerMensajeClienteEn`, `RespuestaPrestadorEn`; transiciones de data-model.md), `Pago` (Concepto `ContactoIndividual`, `PaseMensual`, `RenovacionPase`, `CuotaAdministracion`; MontoCentavos "3900, 12900 o 12900 menos el descuento"; DescuentoCentavos "≥ 0"; Metodo; Estado `Creado`, `Pendiente`, `Aprobado`, `Rechazado`, `Expirado`, `Reembolsado`; PreferenciaMpId; PagoMpId; AvisoMostradoVersion; ConfirmadoEn), `NotificacionMp` ("Se guarda antes de procesar"), `Credito` (Motivo `SinRespuesta48h`, `PrestadorSuspendido`, `PrestadorEliminado`, `PagoDuplicado`; Estado `Disponible`, `Usado`, `CambiadoPorReembolso`) y `SolicitudFactura` (Estado `Solicitada`, `Emitida`)
- [ ] T101 [US3] Crear `PagosDbContext` en `api/src/Modulos/ServiYa.Pagos/Datos/` (esquema `pagos`) con los índices: único parcial `acceso_contacto (cliente_id, prestador_id) WHERE estado IN ('Pendiente','Activo')`; único `pago (pago_mp_id) WHERE pago_mp_id IS NOT NULL`; único `notificacion_mp (request_id)`; `acceso_contacto (estado, vence_en)`; `acceso_contacto (primer_mensaje_cliente_en) WHERE respuesta_prestador_en IS NULL`; y su migración
- [ ] T102 [US3] Crear `IMercadoPago` y `ClienteMercadoPago` en `api/src/Modulos/ServiYa.Pagos/MercadoPago/` sobre `mercadopago-sdk` 3.7.1: preferencia de Checkout Pro (título sin datos personales, `external_reference` = `Pago.Id`, `notification_url`, `back_urls` a `/pago/resultado?pagoId=…`, `auto_return` aprobado, vencimiento de referencias en efectivo), consulta de pago y reembolso con `X-Idempotency-Key`; `ValidadorFirmaMp.cs` hasta que pase T097
- [ ] T103 [US3] Crear `IAccesosService` y `ICreditosService` en `api/src/Modulos/ServiYa.Pagos/Servicios/`: opciones, solicitar acceso (vías `Individual` y `Credito`), activar por pago, `RegistrarMensaje` (fija `PrimerMensajeClienteEn` y `RespuestaPrestadorEn`), `OtorgarPorPrestador(prestadorId, motivo)` (para US7 y US8) y la interfaz de evento `IAlActivarAcceso` (la implementa Chat en US5)
- [ ] T104 [US3] Crear `api/src/Modulos/ServiYa.Pagos/Endpoints/WebhookEndpoints.cs` siguiendo `contracts/webhooks.md`: sin cookie ni XSRF, firma → 401, inserción de `NotificacionMp` (duplicada → 200), respuesta 200 inmediata y procesamiento con un `Channel<Guid>` en memoria y su `BackgroundService`
- [ ] T105 [US3] Crear `api/src/Modulos/ServiYa.Pagos/Endpoints/AccesosEndpoints.cs`: `GET /accesos/opciones`, `POST /accesos` (FR-020, FR-022, FR-025), `GET /accesos/yo`, `GET /accesos/{id}/contacto` (teléfono con `ICuentasService.TelefonoDe` y `Cache-Control: no-store`), `GET /pagos/yo`, `GET /pagos/{id}`, `POST /creditos/{id}/cambiar-por-reembolso` y `POST /admin/pagos/{id}/reembolsar` (bitácora `Reembolsar`)
- [ ] T106 [US3] Crear las tareas en `api/src/Modulos/ServiYa.Pagos/Tareas/`: `VencerAccesos.cs` (`Activo` → `Vencido` en `VenceEn`), `ExpirarReferencias.cs` (`Pendiente` vencido → `Expirado` sin cobro), `CreditoSinRespuesta.cs` (48 h desde `PrimerMensajeClienteEn` sin `RespuestaPrestadorEn` → un solo `Credito` `SinRespuesta48h`) y `ReintentarNotificacionesMp.cs` (`ProcesadaEn` nulo)
- [ ] T107 [US3] Crear el límite diario y las alertas (FR-029): máximo 10 contactos nuevos por cliente por día calendario de `America/Mexico_City` en `IAccesosService`, entidad `Alerta` y `GET /admin/alertas` en `api/src/Modulos/ServiYa.Administracion/Endpoints/AlertasEndpoints.cs`
- [ ] T108 [US3] Decidir y registrar en `specs/001-serviya-mvp/research.md` (R14) cómo generar el PDF del comprobante antes de agregar una dependencia (opción a evaluar: QuestPDF con licencia Community); luego crear `GET /pagos/{id}/comprobante`, `POST /pagos/{id}/factura` (RFC de 12 o 13 caracteres, código postal de 5 dígitos, régimen y uso del CFDI de los catálogos del SAT), `GET /admin/facturas` y `POST /admin/facturas/{id}/emitida` en `api/src/Modulos/ServiYa.Pagos/Endpoints/FacturasEndpoints.cs`
- [ ] T109 [US3] Conectar `web/src/app/features/contacto/` a la API real (`mocks.accesos: false`): redirección a `checkoutUrl`, `resultado-pago.ts` consultando `GET /pagos/{id}` cada 3 s hasta 1 min, instrucciones de pago pendiente, teléfono en `mis-accesos.ts` y nueva pantalla `historial-pagos.ts` con comprobante y solicitud de factura
- [ ] T110 [US3] Escribir `web/e2e/pago-contacto.spec.ts` contra la API en el entorno `E2E`, que registra un `IMercadoPago` falso y un endpoint de desarrollo que envía el webhook firmado: pago aprobado → teléfono visible; pago pendiente → "Pago pendiente" hasta el webhook
- [ ] T111 [US3] Validación manual con credenciales de prueba de Mercado Pago de los escenarios 3, 4 y 5 de quickstart.md (tarjeta de prueba aprobada, OXXO y firma alterada) y registro del resultado en `docs/PROGRESO.md`
- [ ] T112 [US3] Ejecutar `security-reviewer` sobre Pagos y el webhook y confirmar al menos 80% de cobertura en Pagos y desbloqueo de contacto

**Checkpoint**: la plataforma ya puede cobrar contactos individuales.

---

## Fase 8: User Story 4 — Pase o suscripción mensual (Priority: P2)

**Goal**: pase de $129 MXN con 30 contactos por 30 días, como pago único o con renovación automática
cancelable en un clic (Etapa 6).

**Independent Test**: comprar un pase, desbloquear 30 contactos, comprobar que el 31.º pide pago y
cancelar la renovación en un clic (quickstart, escenario 8).

### Tests for User Story 4 ⚠️ (módulo crítico: TDD)

- [ ] T113 [P] [US4] Pruebas en `api/tests/ServiYa.IntegrationTests/Pagos/PasesTests.cs`: con pase activo el contacto se desbloquea sin pago y el contador baja en uno; 31 peticiones simultáneas dejan exactamente 30 contactos usados; el 31.º → 409 con oferta de contacto individual; oferta del pase = 12900 − lo pagado en contactos individuales en 30 días, sin bajar de 3900 (3900 → 9000; 7800 → 5100; 11700 o más → 3900); renovación: aviso una sola vez 3 días antes, cancelación en una acción (`PUT preapproval {status: cancelled}`), acceso hasta `FinEn` y sin más cobros; `subscription_authorized_payment` aprobado → nuevo periodo; al terminar el pase, sus accesos vencen y la conversación queda en solo lectura

### Implementation for User Story 4

- [ ] T114 [US4] Crear `PaseMensual` en `api/src/Modulos/ServiYa.Pagos/Dominio/PaseMensual.cs` (`InicioEn`, "FinEn (inicio + 30 días)", ContactosIncluidos (30), ContactosUsados, RenovacionAutomatica, SuscripcionMpId, RenovacionCanceladaEn, AvisoRenovacionEnviadoEn, Estado `Pendiente`, `Activo`, `Vencido`, `Cancelado`) con la regla "`ContactosUsados ≤ ContactosIncluidos`, con incremento atómico (`UPDATE ... WHERE contactos_usados < contactos_incluidos`)" y su migración
- [ ] T115 [US4] Ampliar `ClienteMercadoPago` con Suscripciones (`preapproval` mensual de 129 MXN con tarjeta y `start_date`) y su cancelación, y el webhook con los tópicos `subscription_preapproval` y `subscription_authorized_payment`
- [ ] T116 [US4] Crear `POST /pases` y `POST /pases/{id}/cancelar-renovacion` en `api/src/Modulos/ServiYa.Pagos/Endpoints/PasesEndpoints.cs`; agregar la vía `Pase` a `POST /accesos` y `pase` y `ofertaPase` a `GET /accesos/opciones` (FR-027: el descuento se cobra como pase de pago único y, si se pide renovación, la suscripción empieza al fin del periodo pagado)
- [ ] T117 [US4] Crear las tareas `api/src/Modulos/ServiYa.Pagos/Tareas/AvisoRenovacion.cs` (correo con fecha, monto y enlace para cancelar, 3 días antes) y `VencerPases.cs`; ajustar `VencerAccesos.cs`: los accesos de origen `Pase` vencen en el `FinEn` del pase
- [ ] T118 [US4] Conectar `web/src/app/features/contacto/pase.ts` a la API real y escribir `web/e2e/pase.spec.ts` (compra con renovación y cancelación en un clic)

**Checkpoint**: US1 a US4 funcionan; ingresos completos.

---

## Fase 9: User Story 5 — Chat y registro de contratación (Priority: P2)

**Goal**: chat en tiempo real con ocultamiento de contacto y registro de la contratación (Etapa 7).

**Independent Test**: mandar "llámame al 55 1234 5678" antes y después del pago (se oculta solo
antes) y registrar, confirmar y terminar una contratación (quickstart, escenarios 6 y 9).

### Tests for User Story 5 ⚠️ (ocultamiento: crítico)

- [ ] T119 [P] [US5] Pruebas en `api/tests/ServiYa.IntegrationTests/Chat/ChatTests.cs`: solo las partes leen y escriben; el cliente escribe solo con acceso `Activo`; sin acceso activo el texto del prestador se guarda con `ContactMasker`; con acceso activo no se oculta; un usuario bloqueado no puede enviar; `idCliente` repetido no duplica; el primer mensaje del cliente fija `PrimerMensajeClienteEn` y la primera respuesta del prestador `RespuestaPrestadorEn`; más de 30 mensajes por minuto → 429
- [ ] T120 [P] [US5] Pruebas en `api/tests/ServiYa.IntegrationTests/Contrataciones/ContratacionesTests.cs`: solo el cliente registra; solo el prestador confirma o rechaza una `Propuesta`; cualquiera termina o cancela una `Confirmada`; transiciones no válidas → 409; terminar dispara `IAlTerminarContratacion`

### Implementation for User Story 5

- [ ] T121 [P] [US5] Crear el dominio y los datos de Chat en `api/src/Modulos/ServiYa.Chat/`: `Conversacion` ("Clave única (cliente, prestador)", `UltimoMensajeEn`), `Mensaje` (Texto "≤ 2000", TextoMostrado, ClaveFoto, EnviadoEn, LeidoEn, OcultoPorModeracion), `SuscripcionPush` (`Endpoint`, `P256dh`, `Auth`); índice `mensaje (conversacion_id, enviado_en)`; `ChatDbContext` (esquema `chat`) y migración
- [ ] T122 [US5] Implementar `IAlActivarAcceso` en `api/src/Modulos/ServiYa.Chat/Servicios/AbrirConversacion.cs`: crea o reabre la conversación y emite `AccesoActualizado`
- [ ] T123 [US5] Crear el hub `api/src/Modulos/ServiYa.Chat/Hub/ChatHub.cs` en `/hubs/chat` según `contracts/webhooks.md` (`[Authorize]` por cookie, grupos por conversación, `EnviarMensaje` guarda primero y luego difunde, `MarcarLeido`, `Error` con `ProblemDetails`) y los endpoints `GET /conversaciones`, `GET /conversaciones/{id}/mensajes?antesDe` y `POST /conversaciones/{id}/mensajes` en `api/src/Modulos/ServiYa.Chat/Endpoints/ChatEndpoints.cs`, con un único servicio `ServicioMensajes` que aplica las reglas de T119
- [ ] T124 [US5] Agregar fotos al chat con `IArchivosService` (bucket privado; URL de lectura de 5 min solo para las partes)
- [ ] T125 [US5] Crear Web Push en `api/src/Modulos/ServiYa.Chat/Push/`: claves VAPID en opciones, `POST /notificaciones/push`, `NotificadorPush` con `WebPush` 1.0.13 y carga `{ titulo, cuerpo, url }` sin datos sensibles; correo si no hay push ni conexión (máximo uno por conversación cada 15 min); agregar el canal push a `INotificador`
- [ ] T126 [P] [US5] Crear Contrataciones en `api/src/Modulos/ServiYa.Contrataciones/`: `Contratacion` (Descripcion "≤ 500", FechaServicio, PrecioAcordadoCentavos "solo informativo; la plataforma no lo cobra", Estado `Propuesta`, `Confirmada`, `Rechazada`, `Terminada`, `Cancelada`, TerminadaEn), `ContratacionesDbContext` (esquema `contrataciones`), migración, endpoints `POST /conversaciones/{id}/contrataciones` y `POST /contrataciones/{id}/confirmar|rechazar|terminar|cancelar`, y la interfaz `IAlTerminarContratacion`
- [ ] T127 [US5] Instalar `@microsoft/signalr` (cliente oficial del hub; justificar en research.md) y crear `web/src/app/features/chat/conversaciones.ts` y `conversacion.ts`: conexión solo en el navegador (no en SSR), reconexión, envío optimista con `idCliente`, aviso de solo lectura, fotos con `subida-archivo`, tarjeta de contratación con el aviso "El pago del trabajo se hace directamente entre ustedes; ServiYa no lo cobra" (US5, escenario 5) y suscripción a push con `SwPush` después del primer mensaje
- [ ] T128 [US5] Escribir `web/e2e/chat.spec.ts` con dos contextos de navegador: mensaje en tiempo real, teléfono oculto antes del pago y visible después, y contratación registrada, confirmada y terminada

**Checkpoint**: US1 a US5 funcionan.

---

## Fase 10: User Story 6 — Calificaciones doble ciega (Priority: P2)

**Goal**: calificaciones bidireccionales en 4 ejes, ocultas hasta que ambos califican o pasan 14 días
(Etapa 8).

**Independent Test**: terminar una contratación ficticia, calificar desde ambas cuentas y comprobar
que aparecen solo al final y actualizan los promedios (quickstart, escenario 9).

### Tests for User Story 6 ⚠️

- [ ] T129 [P] [US6] Pruebas en `api/tests/ServiYa.IntegrationTests/Calificaciones/CalificacionesTests.cs`: contratación no `Terminada` → 422; segunda calificación del mismo autor → 409; oculta hasta que califican ambos o pasan 14 días (`FakeTimeProvider`); al publicarse se actualizan promedios por eje, `TotalResenas` y `PuntajeRanking`; respuesta del prestador una sola vez (409 la segunda); máximo 5 fotos; comentario con `ContactMasker`; ejes del prestador al cliente; ampliar `DtosPublicosTests.cs` con `ResenaPublica`

### Implementation for User Story 6

- [ ] T130 [US6] Crear en `api/src/Modulos/ServiYa.Calificaciones/` la entidad `Calificacion` (Direccion `ClienteAPrestador` o `PrestadorACliente`; Calidad, PrecioJusto, Puntualidad y Trato "smallint 1–5"; PuntualidadPago, TratoCliente y Claridad "smallint 1–5"; Comentario "≤ 1000, con `ContactMasker`"; ClavesFotos "Hasta 5"; Estado `Oculta`, `Publicada`, `RetiradaPorModeracion`; PublicadaEn; Respuesta y RespondidaEn), "Una calificación por autor y contratación", índice `calificacion (estado, created_at)`, `CalificacionesDbContext` y migración
- [ ] T131 [US6] Implementar `IAlTerminarContratacion` (abre el periodo de calificación) y los endpoints `POST /contrataciones/{id}/calificacion`, `POST /calificaciones/{id}/respuesta` y `GET /prestadores/{id}/resenas` en `api/src/Modulos/ServiYa.Calificaciones/Endpoints/CalificacionesEndpoints.cs`; al publicar llama a `IPrestadoresService.ActualizarPromedios` y `RecalcularRanking`
- [ ] T132 [US6] Crear la tarea `api/src/Modulos/ServiYa.Calificaciones/Tareas/PublicarCalificaciones.cs` (14 días después de terminada la contratación)
- [ ] T133 [US6] Crear `web/src/app/features/calificaciones/calificar.ts` (4 ejes de 1 a 5 como grupos de radio accesibles, comentario y hasta 5 fotos), `pendientes.ts` (enlazado desde Mi cuenta) y `responder.ts`, y mostrar reseñas con respuesta en `perfil-publico.ts`

---

## Fase 11: User Story 7 — Reportes, bloqueo y suspensión (Priority: P3)

**Goal**: reportar contenido, bloquear usuarios y suspender cuentas con motivo y aclaración
(Etapa 8).

**Independent Test**: reportar un mensaje ficticio, suspender la cuenta, enviar una aclaración y
reactivarla.

### Tests for User Story 7 ⚠️

- [ ] T134 [P] [US7] Pruebas en `api/tests/ServiYa.IntegrationTests/Reportes/ReportesTests.cs`: un reporte de mensaje llega a la cola con el contexto de la conversación; suspender sin motivo → 400; al suspender, el perfil sale de búsquedas, el usuario recibe el motivo (correo y 403 `cuenta-suspendida`), se registra en la bitácora y los clientes con acceso vigente reciben un `Credito` `PrestadorSuspendido` (FR-043); la aclaración permite mantener o levantar la suspensión con registro; ampliar `PermisosPorRolTests.cs`

### Implementation for User Story 7

- [ ] T135 [US7] Crear en `api/src/Modulos/ServiYa.Reportes/` las entidades `Reporte` (ObjetivoTipo `Perfil`, `Mensaje`, `Resena`; Motivo de catálogo; Estado `Abierto`, `Resuelto`, `Descartado`; Resolucion; ResueltoPor) y `Suspension` (Motivo, AplicadaPor, AplicadaEn, LevantadaEn, Aclaracion, AclaracionEn, Decision), `ReportesDbContext` y migración
- [ ] T136 [US7] Crear los endpoints de usuario `POST /reportes`, `POST /bloqueos`, `DELETE /bloqueos/{usuarioId}` y `POST /suspensiones/yo/aclaracion` en `api/src/Modulos/ServiYa.Reportes/Endpoints/ReportesEndpoints.cs`
- [ ] T137 [US7] Crear los endpoints de administración `GET /admin/reportes`, `POST /admin/reportes/{id}/resolver` (ocultar mensaje o retirar reseña), `POST /admin/usuarios/{id}/suspender` y `/reactivar` (estado del usuario y del perfil, cierre de sesiones con `UpdateSecurityStampAsync`, créditos con `ICreditosService` y aviso) y `GET /admin/bitacora?actor&accion&desde&hasta` en `api/src/Modulos/ServiYa.Administracion/Endpoints/ModeracionEndpoints.cs`
- [ ] T138 [US7] Crear en el frontend `web/src/app/shared/reportar/reportar.ts` (perfil, mensaje y reseña), "Bloquear" en `conversacion.ts`, `web/src/app/features/cuenta/suspendida.ts` (motivo y aclaración) y en `web/src/app/features/admin/` las pantallas `reportes/`, `bitacora/`, `alertas/` y `facturas/`

---

## Fase 12: User Story 8 — Privacidad y control de la cuenta (Priority: P3)

**Goal**: textos legales versionados, descarga de datos y eliminación de la cuenta (Etapa 9).

**Independent Test**: eliminar una cuenta de prestador ficticia y comprobar que el perfil desaparece,
sus documentos se borran y los pagos se conservan.

### Tests for User Story 8 ⚠️

- [ ] T139 [P] [US8] Pruebas en `api/tests/ServiYa.IntegrationTests/Cuentas/PrivacidadTests.cs`: `GET /cuentas/yo/datos` devuelve solo los datos propios; `DELETE /cuentas/yo` exige la contraseña, oculta el perfil de inmediato, borra los objetos de R2, anonimiza los datos personales, conserva los pagos, otorga `Credito` `PrestadorEliminado` a los clientes con acceso vigente y cierra la sesión; una versión legal nueva aparece en `aceptacionesPendientes` de la sesión

### Implementation for User Story 8

- [ ] T140 [US8] Crear los textos legales versionados en `api/src/Modulos/ServiYa.Cuentas/Legal/` (aviso de privacidad, términos y política de reembolsos marcados "BORRADOR: pendiente de revisión por un abogado"), `GET /legal/{documento}` y `aceptacionesPendientes` en `GET /cuentas/yo`; agregar el endpoint a `contracts/api.md`
- [ ] T141 [US8] Crear la interfaz `IExportadorDatos` en `api/src/ServiYa.Comun/Privacidad/` con una implementación por módulo y `GET /cuentas/yo/datos` que las combina en un JSON descargable
- [ ] T142 [US8] Crear la interfaz `IAlEliminarCuenta` con una implementación por módulo (Prestadores → `Eliminado`; Documentos y portafolio → borrar objetos; Chat → autor "Usuario eliminado" y borrar suscripciones push; Pagos → conservar pagos y otorgar créditos) y `DELETE /cuentas/yo` → 202 en `api/src/Modulos/ServiYa.Cuentas/Endpoints/CuentasEndpoints.cs`
- [ ] T143 [US8] Crear `web/src/app/features/cuenta/privacidad.ts` (textos legales, descargar mis datos y eliminar cuenta con contraseña y confirmación) y el aviso para aceptar versiones nuevas de los textos

---

## Fase 13: Pulido, seguridad y despliegue (Etapa 9)

**Purpose**: requisitos transversales antes del piloto.

- [ ] T144 [P] Configurar en `web/src/server.ts` la CSP (`default-src 'self'`; imágenes y video del dominio público de R2 y `blob:`; `connect-src` a la API y `wss:`; scripts con nonce mediante `ngCspNonce`), HSTS, `Referrer-Policy` y `Permissions-Policy` (cámara y ubicación solo en el propio sitio)
- [ ] T145 [P] Ajustar presupuestos en `web/angular.json` (JS inicial < 300 KB gzip en la app y < 150 KB en el perfil público) y medir con Lighthouse en perfil móvil que el LCP de búsqueda y perfil sea < 2.5 s; registrar los resultados en `docs/PROGRESO.md`
- [ ] T146 [P] Escribir `web/e2e/accesibilidad.spec.ts` con `@axe-core/playwright` (dependencia de desarrollo; justificar en research.md) sobre inicio, resultados, perfil, alta, opciones de pago y chat
- [ ] T147 [P] Crear `api/Dockerfile` (multietapa, usuario sin privilegios), `web/Dockerfile` (Node con SSR), `render.yaml` con los dos servicios y el endpoint de salud `GET /salud` (verifica la base de datos) en `api/src/ServiYa.Api/Program.cs`
- [ ] T148 Preparar producción con el usuario: Neon con PostGIS, rol `serviya_app` sin `UPDATE` ni `DELETE` en la bitácora y migraciones con `dotnet ef migrations bundle`; buckets de R2 (privado sin acceso público, público con dominio propio, CORS de `PUT` solo desde `serviya.mx` y regla que borra subidas no registradas después de 1 día); credenciales de producción de Mercado Pago y URL del webhook
- [ ] T149 Dejar FR-014 listo para activarse: opción `Prestadores:CuotaAdministracion:Activa` en `false`; el cobro al aprobar se especifica con `/speckit-specify` antes de encenderlo
- [ ] T150 Ejecutar los 10 escenarios de "Etapas 3 a 9" de `specs/001-serviya-mvp/quickstart.md` y registrar el resultado
- [ ] T151 Revisión final con `security-reviewer`, `database-reviewer`, `csharp-reviewer` y `typescript-reviewer`, y reporte de cobertura con al menos 80% en Pagos, permisos, Documentos, desbloqueo de contacto y `ContactMasker`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Fase 1 (frontend)**: sin dependencias.
- **Fase 2 (prototipo)**: depende de la Fase 1. Termina con la aprobación del usuario.
- **Fase 3 (backend)**: depende de la aprobación del prototipo y de que Docker esté instalado.
- **Fase 4 (fundamentos)**: depende de la Fase 3 y **bloquea todas las historias**.
- **US1 (Fase 5)**: depende de la Fase 4.
- **US2 (Fase 6)**: depende de la Fase 4; para probarla con datos reales necesita perfiles aprobados
  de US1 (o una semilla de prestadores ficticios).
- **US3 (Fase 7)**: depende de US1 (prestadores aprobados) y de la cuenta de Mercado Pago.
- **US4 (Fase 8)**: depende de US3.
- **US5 (Fase 9)**: depende de US3 (`IAccesosService`, `IAlActivarAcceso`).
- **US6 (Fase 10)**: depende de US5 (contrataciones terminadas) y de T091 (ranking).
- **US7 (Fase 11)**: depende de US3 (`ICreditosService`) y de US5 (mensajes que reportar).
- **US8 (Fase 12)**: depende de que existan los módulos que exportan y eliminan datos (US1 a US7).
- **Fase 13**: depende de las historias que entren al piloto.

### Within Each User Story

- Pruebas primero (deben fallar), luego dominio, datos, servicios, endpoints, conexión del frontend y
  E2E.
- Cada historia termina con su checkpoint y la actualización de `docs/PROGRESO.md` en el mismo commit.

### Parallel Opportunities

- Fase 1: T003 a T008.
- Fase 2: los componentes compartidos (T016 a T018) y las pantallas marcadas [P] de cada historia.
- Fase 4: T047, T049 a T055 y T060 son archivos independientes.
- En cada historia: todas sus pruebas [P] al mismo tiempo, y los dominios [P] de módulos distintos
  (por ejemplo T075 y T077).

---

## Parallel Example: User Story 1

```bash
# Pruebas de US1 juntas (deben fallar):
Task: "T071 Pruebas de la máquina de estados en api/tests/ServiYa.UnitTests/Prestadores/PerfilPrestadorEstadoTests.cs"
Task: "T072 Pruebas del nombre público en api/tests/ServiYa.UnitTests/Prestadores/NombrePublicoTests.cs"
Task: "T073 Pruebas de Documentos en api/tests/ServiYa.IntegrationTests/Documentos/DocumentosTests.cs"
Task: "T074 Pruebas del alta en api/tests/ServiYa.IntegrationTests/Prestadores/AltaPrestadorTests.cs"

# Dominios de módulos distintos juntos:
Task: "T075 Dominio de Prestadores en api/src/Modulos/ServiYa.Prestadores/Dominio/"
Task: "T077 Dominio y datos de Documentos en api/src/Modulos/ServiYa.Documentos/"
```

---

## Implementation Strategy

### MVP primero

1. Fases 1 y 2: prototipo navegable e instalable en el celular → **el usuario lo valida**.
2. Fases 3 y 4: base del backend y cuentas.
3. Fase 5 (US1): prestadores verificados. **Detenerse y validar** con documentos ficticios.
4. Fases 6 y 7 (US2 y US3): vitrina y cobro del contacto. Con esto, el MVP ya genera ingresos.

### Entrega incremental

- Después de US3: US4 (pase) → US5 (chat y contratación) → US6 (calificaciones) → US7 (reportes) →
  US8 (privacidad) → Fase 13 → piloto (Etapa 10).
- Antes del piloto deben estar US7, US8 y la Fase 13 (moderación, obligaciones legales y seguridad).

### Pendientes que bloquean tareas concretas

- Ciudad o zona del piloto → semilla de municipios de producción (T066).
- Oficios del piloto → semilla de oficios (T065).
- Textos legales revisados por un abogado → reemplazar los borradores de T140 antes del piloto.

---

## Notes

- [P] = archivos distintos y sin dependencias pendientes.
- [USx] liga la tarea con su historia de `spec.md`.
- Commit al cerrar cada tarea o grupo lógico, con `docs/PROGRESO.md` actualizado.
- Evitar: tareas vagas, dos tareas sobre el mismo archivo en paralelo y dependencias entre historias
  que rompan su independencia.
