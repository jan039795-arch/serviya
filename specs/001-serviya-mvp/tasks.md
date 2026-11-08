---

description: "Lista de tareas del MVP de ServiYa"
---

# Tasks: ServiYa MVP — Plataforma de contacto con prestadores de oficios verificados

**Input**: documentos de diseño en `specs/001-serviya-mvp/` ([plan.md](plan.md), [spec.md](spec.md),
[research.md](research.md), [data-model.md](data-model.md), [contracts/api.md](contracts/api.md),
[contracts/webhooks.md](contracts/webhooks.md), [quickstart.md](quickstart.md))

**Prerequisites**: plan.md y spec.md (listos); constitución v1.0.1

**Tests**: la constitución (principio VI) y `.claude/rules/common/testing.md` las exigen. Son
**TDD obligatorio con 80% de cobertura** en los módulos críticos: Pagos y webhook, permisos por rol,
desbloqueo de contacto, Documentos (URLs firmadas y bitácora) y `ContactMasker`. En el resto se prueba
el comportamiento y los caminos de error sin porcentaje fijo. Las tareas de prueba de cada historia van
**antes** de su implementación y deben fallar primero.

**Organization**: tareas agrupadas por historia de usuario. El orden de fases sigue las etapas de
`plan.md` (la constitución exige el prototipo con datos ficticios antes que el backend).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin depender de tareas incompletas)
- **[Story]**: historia de `spec.md` a la que pertenece (US1 … US8)
- Cada tarea indica las rutas exactas de los archivos

## Path Conventions

- **Backend**: `api/src/ServiYa.Api/`, `api/src/ServiYa.Comun/`, `api/src/Modulos/ServiYa.<Modulo>/`
  con las carpetas `Dominio/`, `Datos/` (DbContext, configuraciones y `Migraciones/`), `Endpoints/`,
  `Servicios/` y `Tareas/`. Cada módulo expone `Modulo<Nombre>.cs` con `Agregar<Nombre>(this
  IServiceCollection, IConfiguration)` y `Mapear<Nombre>(this RouteGroupBuilder)`.
- **Contratos entre módulos**: interfaces y DTOs en `api/src/ServiYa.Comun/Contratos/<Modulo>/`; la
  implementación vive en su módulo y `ServiYa.Api` la registra. Ningún módulo referencia a otro módulo
  ni consulta sus tablas.
- **Pruebas backend**: `api/tests/ServiYa.UnitTests/`, `api/tests/ServiYa.IntegrationTests/`,
  `api/tests/ServiYa.ContractTests/`, organizadas por módulo.
- **Frontend**: `web/src/app/core/`, `web/src/app/shared/`, `web/src/app/features/<area>/<pantalla>/`
  con `<pantalla>.ts`, `.html` y `.scss` (sin sufijo `.component`). E2E en `web/e2e/`.
- **Migraciones**: `dotnet ef migrations add <Nombre> --project src/Modulos/ServiYa.<Modulo>
  --startup-project src/ServiYa.Api --context <Modulo>DbContext --output-dir Datos/Migraciones`
  (desde `api/`).

## Mapa de fases y etapas

| Fase | Etapa de `plan.md` | Historias | Requiere Docker |
|---|---|---|---|
| 1. Setup | 2 y 3 | — | No (la parte de `api/` solo compila) |
| 2. Prototipo | 2 | US1–US4 (pantallas) | No |
| 3. Fundamentos del backend | 3 | — | Sí (o la CI de GitHub) |
| 4. US1 | 3 y 4 | US1 | Sí |
| 5. US2 | 5 | US2 | Sí |
| 6. US3 | 6 | US3 | Sí |
| 7. US4 | 6 | US4 | Sí |
| 8. US5 | 7 | US5 | Sí |
| 9. US6 | 8 | US6 | Sí |
| 10. US7 | 8 | US7 | Sí |
| 11. US8 | 9 | US8 | Sí |
| 12. Cierre | 9 y 10 | — | Sí |

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: crear los dos proyectos, las herramientas y la CI. La parte de `web/` desbloquea el
prototipo; la de `api/` solo necesita el SDK de .NET 10.

- [ ] T001 Crear las carpetas `api/`, `web/` y `docker/` en la raíz y ampliar `.gitignore` con `coverage/`, `TestResults/`, `*.trx`, `web/test-results/` y `web/playwright-report/`
- [ ] T002 [P] Crear `.editorconfig` en la raíz: UTF-8, LF, 4 espacios en `*.cs` (namespaces de archivo, `var` cuando el tipo es evidente), 2 espacios en `*.ts`, `*.html`, `*.scss`, `*.json` y `*.yml`
- [ ] T003 Generar el proyecto Angular 22 en `web/` con `npx @angular/cli@22 new web --ssr --style=scss --prefix=sy --strict --skip-git` (standalone, sin zone.js por defecto en 22) y dejar `web/package.json` con versiones exactas (`--save-exact`)
- [ ] T004 Agregar la PWA con `ng add @angular/pwa@22` en `web/`; ajustar `web/public/manifest.webmanifest` (`name` "ServiYa", `short_name` "ServiYa", `lang` "es-MX", `display` "standalone", `start_url` "/", `theme_color` y `background_color` de los tokens, íconos 192, 512 y `maskable`) y `web/ngsw-config.json` (sin cachear `/api/**`)
- [ ] T005 [P] Agregar ESLint con `ng add angular-eslint` y Prettier (`web/.prettierrc`: `singleQuote`, `printWidth` 100) en `web/eslint.config.js`, con la regla `no-console` en error; scripts `lint` y `format` en `web/package.json`
- [ ] T006 [P] Configurar el idioma: `registerLocaleData(localeEsMx)` y `{ provide: LOCALE_ID, useValue: 'es-MX' }` en `web/src/app/app.config.ts`, `<html lang="es-MX">` y `<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">` en `web/src/index.html`
- [ ] T007 Crear `web/src/environments/environment.ts` (`useMocks: false`, `apiBaseUrl: '/api/v1'`, `hubUrl: '/hubs/chat'`) y `web/src/environments/environment.prototipo.ts` (`useMocks: true`); agregar la configuración `prototipo` con `fileReplacements` en `web/angular.json` y los scripts `start` (`ng serve --configuration prototipo`) y `start:api` (`ng serve --proxy-config proxy.conf.json`) en `web/package.json`
- [ ] T008 [P] Crear `web/proxy.conf.json` que envíe `/api` y `/hubs` (con `ws: true`) a `http://localhost:5080`, para que en desarrollo la web y la API compartan origen y cookies
- [ ] T009 [P] Configurar las pruebas unitarias de Angular CLI 22 (Vitest) con cobertura en `web/angular.json` y el script `test` (`ng test --no-watch`) en `web/package.json`
- [ ] T010 [P] Instalar `@playwright/test@1.65` exacto y crear `web/playwright.config.ts` con los proyectos `Pixel 7` e `iPhone 14` (dispositivos de Playwright), `trace: 'off'`, `webServer` con `npm start` en `http://localhost:4200` y `testIdAttribute: 'data-testid'`
- [ ] T011 [P] Crear la solución `api/ServiYa.sln` con `api/global.json` (SDK `10.0.402`, `rollForward: latestFeature`), `api/Directory.Build.props` (`net10.0`, `LangVersion` 14, `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true, `AnalysisLevel` latest-recommended) y `api/Directory.Packages.props` (gestión central con las versiones de research.md: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 y su NetTopologySuite 10.0.3, mercadopago-sdk 3.7.1, AWSSDK.S3 4.0.104, WebPush 1.0.13, AwesomeAssertions 9.6.0, Testcontainers.PostgreSql 4.16.0, xunit, NSubstitute, Microsoft.AspNetCore.Mvc.Testing 10, Microsoft.Extensions.TimeProvider.Testing 10)
- [ ] T012 Crear los proyectos en `api/src/`: `ServiYa.Api` (`dotnet new web`), `ServiYa.Comun` (classlib) y en `api/src/Modulos/` los 10 classlib `ServiYa.Cuentas`, `ServiYa.Prestadores`, `ServiYa.Documentos`, `ServiYa.Busqueda`, `ServiYa.Pagos`, `ServiYa.Chat`, `ServiYa.Contrataciones`, `ServiYa.Calificaciones`, `ServiYa.Reportes` y `ServiYa.Administracion`, todos agregados a `api/ServiYa.sln`; referencias: cada módulo → `ServiYa.Comun`; `ServiYa.Api` → `ServiYa.Comun` y los 10 módulos
- [ ] T013 Crear en `api/tests/` los proyectos xUnit `ServiYa.UnitTests`, `ServiYa.IntegrationTests` y `ServiYa.ContractTests` con AwesomeAssertions, NSubstitute y `coverlet.collector`; `ServiYa.IntegrationTests` y `ServiYa.ContractTests` referencian `ServiYa.Api` y `Microsoft.AspNetCore.Mvc.Testing`; agregar `<InternalsVisibleTo>` de la API a las pruebas en `api/src/ServiYa.Api/ServiYa.Api.csproj`
- [ ] T014 [P] Crear `docker-compose.yml` en la raíz con `postgis/postgis:17-3.5` (puerto 5432, volumen `pgdata`, script `docker/postgres/init.sql`) y `minio/minio` (puertos 9000 y 9001) más un contenedor `minio/mc` que crea los buckets `serviya-privado` y `serviya-publico` (este último con lectura anónima)
- [ ] T015 [P] Crear `docker/postgres/init.sql`: base `serviya`, extensión `postgis`, rol `serviya_migraciones` (dueño de los esquemas) y rol `serviya_app` (solo `SELECT`, `INSERT`, `UPDATE`, `DELETE` y uso de secuencias, sin DDL)
- [ ] T016 [P] Crear `.github/workflows/ci.yml` con dos trabajos en `ubuntu-latest`: `web` (Node 24, `npm ci`, `npm run lint`, `npm test`, `npx ng build`) y `api` (.NET 10, `dotnet build -warnaserror`, `dotnet test` con Testcontainers, que en GitHub sí tiene Docker); el trabajo `api` se salta mientras `api/` no tenga pruebas
- [ ] T017 Registrar en la sección "Comandos" de `CLAUDE.md` los comandos de compilación, pruebas y ejecución de `web/` y `api/`

**Checkpoint**: `npm start` abre una página vacía de Angular en `http://localhost:4200`, `dotnet build api/ServiYa.sln` compila y la CI pasa en GitHub.

---

## Phase 2: Prototipo con datos ficticios (Etapa 2)

**Purpose**: validar en el celular las pantallas de US1–US4 antes de manejar documentos o dinero
reales (constitución, flujo de desarrollo; research R10). Solo frontend: el `MockApiInterceptor`
responde según [contracts/api.md](contracts/api.md) con datos de `web/src/mocks/*.json`. **Todos los
datos son inventados**; los teléfonos de ejemplo usan el formato `55 0000 00xx` y las imágenes de
documentos llevan la marca "DOCUMENTO FICTICIO".

**Pasos del alta del prestador (10)**: 1 Datos personales · 2 Oficios y experiencia · 3 Tarifas de
referencia · 4 Zona de cobertura · 5 Disponibilidad · 6 Descripción · 7 Consentimiento de datos
sensibles · 8 Documentos · 9 Video y portafolio · 10 Revisar y enviar.

### Base del prototipo

- [ ] T018 Escribir los tipos de los contratos en `web/src/app/core/api/modelos.ts` (`Sesion`, `PerfilPropio`, `DocumentoPropio`, `TarjetaPrestador`, `PerfilPublico`, `ResenaPublica`, `Oficio`, `Municipio`, `OpcionesAcceso`, `AccesoContacto`, `PaseMensual`, `Pago`, `Pagina<T>`, `ProblemDetails` y los enums de estado de data-model.md); se reemplazarán por los tipos generados de OpenAPI en la Fase 3
- [ ] T019 [P] Crear los tokens de diseño en `web/src/styles/_tokens.scss` (colores con contraste AA de 4.5:1 en texto, tipografía base de 16 px, espaciados y radio) y los estilos globales en `web/src/styles.scss` (botones y campos con área táctil de 44 px o más, foco visible, `prefers-reduced-motion`, ancho mínimo 360 px)
- [ ] T020 [P] Crear los datos ficticios en `web/src/mocks/`: `oficios.json` (los 12 oficios semilla de data-model.md), `municipios.json` (6 municipios de ejemplo, sin polígono), `prestadores.json` (al menos 15 tarjetas y perfiles públicos inventados con promedios, insignias y tarifas), `resenas.json`, `sesiones.json` (cliente, prestador y administrador de demostración), `perfil-propio.json`, `revisiones.json` (3 perfiles en revisión con documentos) y `pagos.json`
- [ ] T021 [P] Generar imágenes de ejemplo en `web/public/mocks/` (fotos de portafolio genéricas y documentos con la marca de agua "DOCUMENTO FICTICIO", sin rostros ni datos reales) y un video corto de muestra
- [ ] T022 Implementar el `HttpInterceptorFn` de simulación en `web/src/app/core/mocks/mock-api-interceptor.ts`: tabla de rutas (método + patrón de `contracts/api.md`) → manejador, estado en memoria para escrituras (alta por pasos, decisiones del administrador, accesos), latencia de 300 ms, errores como `ProblemDetails` y paginación `{ items, total, pagina, tamano }`; registrarlo en `web/src/app/app.config.ts` solo si `environment.useMocks`
- [ ] T023 [P] Crear la pantalla de pasarela simulada en `web/src/app/core/mocks/checkout-simulado/checkout-simulado.ts` (ruta `/prototipo/checkout?pagoId`, solo con `useMocks`) con los botones "Pagar con tarjeta (aprobado)", "Generar referencia OXXO (pendiente)", "Acreditar OXXO" y "Rechazar", que cambian el estado del pago en el interceptor y regresan a `/pago/resultado?pagoId=…`
- [ ] T024 [P] Crear el selector de cuenta de demostración en `web/src/app/core/mocks/selector-cuenta-demo/selector-cuenta-demo.ts` (cliente, prestador, administrador o sin sesión), visible solo con `useMocks`
- [ ] T025 Implementar el estado de sesión con signals en `web/src/app/core/sesion/sesion-store.ts` (`sesion`, `tieneSesion`, `esCliente`, `esPrestador`, `esAdmin`, `cargar()` con `GET /cuentas/yo`, `cerrar()`) y los guards funcionales `requiereSesion` y `requiereRol(rol)` en `web/src/app/core/sesion/guards.ts` (sin sesión → `/cuenta/iniciar-sesion?volverA=` con ruta relativa validada)
- [ ] T026 [P] Implementar `web/src/app/core/http/errores-interceptor.ts`: convierte `ProblemDetails` en mensajes simples en español, 401 → inicio de sesión, 403 con `type: "/errores/cuenta-suspendida"` → `/cuenta/suspendida`, 429 → "Espera un momento" con `Retry-After`
- [ ] T027 Crear el armazón en `web/src/app/shell/shell.ts` (barra superior, navegación inferior móvil con Buscar, Mensajes y Cuenta, enlace "Saltar al contenido", `aria-current` en la sección activa) y las rutas perezosas por área en `web/src/app/app.routes.ts`
- [ ] T028 [P] Definir los modos de renderizado en `web/src/app/app.routes.server.ts`: `RenderMode.Server` para `''`, `buscar` y `prestadores/:id`; `RenderMode.Client` para el resto (cuenta, alta, contacto, chat y admin)
- [ ] T029 [P] Crear los componentes compartidos en `web/src/app/shared/ui/`: `tarjeta-prestador/`, `calificacion-ejes/` (los 4 ejes con estrellas y texto accesible "4.5 de 5"), `insignia/`, `estado-vacio/`, `aviso/` (info, éxito, error con `role="status"` o `role="alert"`) y `paso-indicador/` ("Paso 3 de 10")
- [ ] T030 [P] Crear el pipe `web/src/app/shared/pipes/moneda-mxn.ts` (centavos → "$39 MXN"; con decimales solo si no son cero) y su prueba en `web/src/app/shared/pipes/moneda-mxn.spec.ts`
- [ ] T031 [P] Implementar `web/src/app/shared/archivos/comprimir-imagen.ts` (`<canvas>`, lado mayor de 1600 px como máximo, WebP o JPEG con calidad 0.8, research R4) y `web/src/app/shared/archivos/validar-video.ts` (duración ≤ 90 s leída de los metadatos y tamaño ≤ 60 MB), con pruebas en `web/src/app/shared/archivos/archivos.spec.ts` para el cálculo de dimensiones y los límites
- [ ] T032 Crear el componente `web/src/app/shared/archivos/subida-archivo/subida-archivo.ts`: elegir o tomar foto, previsualización, compresión, barra de progreso, cancelar y **reintentar sin repetir el formulario** (FR-046); en el prototipo la subida la simula el interceptor

### Pantallas de US1 (cuenta, alta y revisión)

- [ ] T033 [P] [US1] Crear `web/src/app/features/cuenta/registro/registro.ts` con Signal Forms: correo, contraseña, nombre completo (3–120 caracteres), fecha de nacimiento, rol ("Busco un servicio" / "Ofrezco un servicio") y casillas de aviso de privacidad y términos con su versión; mensaje claro si es menor de 18 años
- [ ] T034 [P] [US1] Crear `web/src/app/features/cuenta/verificar-correo/verificar-correo.ts` (código de 6 dígitos con `autocomplete="one-time-code"` e `inputmode="numeric"`, reenviar con espera visible) y `web/src/app/features/cuenta/iniciar-sesion/iniciar-sesion.ts`
- [ ] T035 [P] [US1] Crear `web/src/app/features/cuenta/recuperar-contrasena/recuperar-contrasena.ts` (pedir código y restablecer en dos pasos, con el mismo mensaje exista o no el correo)
- [ ] T036 [US1] Implementar el estado del alta en `web/src/app/features/alta-prestador/alta-store.ts`: carga `GET /prestadores/yo`, retoma desde `pasoAlta`, guarda cada paso con `PATCH /prestadores/yo` al pulsar "Siguiente" y con espera de 1 s al escribir, y deja un respaldo local en `localStorage` (con `try/catch`) que se descarta al confirmar el guardado
- [ ] T037 [US1] Crear el contenedor `web/src/app/features/alta-prestador/alta-prestador.ts` con el indicador de pasos, navegación Anterior/Siguiente, rutas hijas `/alta/:paso` y el aviso "Guardado" tras cada paso
- [ ] T038 [P] [US1] Crear los pasos 1–3 en `web/src/app/features/alta-prestador/pasos/datos-personales/`, `pasos/oficios/` (oficio y años de experiencia 0–60) y `pasos/tarifas/` (mínimo y máximo en pesos y unidad `Hora`, `Dia` o `Servicio`, con mínimo ≤ máximo)
- [ ] T039 [P] [US1] Crear los pasos 4–6 en `web/src/app/features/alta-prestador/pasos/cobertura/` (municipios cubiertos, al menos uno, y colonia base), `pasos/disponibilidad/` (días y franjas horarias) y `pasos/descripcion/` (0–600 caracteres con contador y aviso de que no debe incluir teléfonos ni correos)
- [ ] T040 [P] [US1] Crear los pasos 7–8 en `web/src/app/features/alta-prestador/pasos/consentimiento/` (texto de datos sensibles y biométricos con aceptación explícita; sin aceptar no se avanza, US8 escenario 1) y `pasos/documentos/` (INE frente, INE reverso, selfie, comprobante de domicilio con fecha de expedición, y carta de no antecedentes opcional con su fecha, usando `subida-archivo`)
- [ ] T041 [P] [US1] Crear los pasos 9–10 en `web/src/app/features/alta-prestador/pasos/portafolio/` (video opcional de hasta 90 s y hasta 20 fotos con descripción de máximo 200 caracteres) y `pasos/revisar-enviar/` (resumen, lista de faltantes y botón "Enviar a revisión")
- [ ] T042 [P] [US1] Crear `web/src/app/features/alta-prestador/estado-perfil/estado-perfil.ts`: estado del perfil en lenguaje simple, motivo de cada documento observado y botón "Volver a subir" solo para los documentos que lo requieren (US1 escenario 4)
- [ ] T043 [P] [US1] Crear `web/src/app/features/admin/cola-revision/cola-revision.ts` (perfiles `EnRevision` por fecha de envío) protegida con `requiereRol('Admin')`
- [ ] T044 [US1] Crear `web/src/app/features/admin/revision-prestador/revision-prestador.ts`: datos del perfil, documentos con Aprobar / Requiere corrección / Rechazar y **motivo obligatorio** salvo al aprobar, moderación de video y fotos, y aprobar o rechazar el perfil (deshabilitado hasta que todos los obligatorios estén aprobados)
- [ ] T045 [P] [US1] Crear `web/src/app/features/admin/visor-documento/visor-documento.ts`: pide `POST /admin/documentos/{id}/ver`, muestra la imagen o el PDF con `referrerpolicy="no-referrer"`, avisa "Esta consulta queda registrada" y descarta la URL al cerrar

### Pantallas de US2 (búsqueda y perfil público)

- [ ] T046 [P] [US2] Crear `web/src/app/features/busqueda/inicio/inicio.ts`: elegir oficio y municipio, botón "Usar mi ubicación" (Geolocation API → `POST /catalogos/municipios/ubicar`) y accesos rápidos a oficios
- [ ] T047 [P] [US2] Crear `web/src/app/features/busqueda/resultados/resultados.ts` (ruta `buscar?oficio&municipio`): filtros de calificación mínima, insignia y tarifa máxima en un panel plegable, lista de `tarjeta-prestador` y botón "Ver más"
- [ ] T048 [P] [US2] Crear `web/src/app/features/busqueda/perfil-publico/perfil-publico.ts` (ruta `prestadores/:id`): nombre público, colonia o municipio, oficios, experiencia, tarifas, disponibilidad, video (`preload="metadata"`), galería, insignias, calificaciones por eje, reseñas paginadas y botón "Contactar"; **sin** teléfono, correo, dirección ni documentos; `Title` y `Meta` por perfil

### Pantallas de US3 (contacto y pago)

- [ ] T049 [P] [US3] Crear `web/src/app/features/contacto/opciones-contacto/opciones-contacto.ts` (ruta `contactar/:prestadorId`, requiere sesión): contacto individual de $39 MXN (chat y teléfono por 30 días, crédito si no responde en 48 h) frente al pase de $129 MXN, el aviso "Pagas por acceso y contacto, no por el trabajo. No garantiza la contratación." con su versión, la advertencia de que OXXO o SPEI pueden tardar de 48 a 72 h y la recomendación de tarjeta; si ya hay acceso vigente, lo indica y no ofrece pagar (FR-022, FR-025)
- [ ] T050 [P] [US3] Crear `web/src/app/features/contacto/resultado-pago/resultado-pago.ts` (ruta `pago/resultado?pagoId`): consulta `GET /pagos/{id}` con reintentos (2, 4, 8 s… hasta 2 min) y muestra Aprobado (ir al chat y ver teléfono), Pendiente (instrucciones de OXXO o SPEI) o Rechazado (intentar de nuevo)
- [ ] T051 [P] [US3] Crear `web/src/app/features/contacto/mis-accesos/mis-accesos.ts`: accesos con estado y vencimiento, teléfono visible solo en los `Activo` (`GET /accesos/{id}/contacto`, enlace `tel:`) y créditos disponibles

### Pantallas de US4 (pase mensual)

- [ ] T052 [P] [US4] Crear `web/src/app/features/contacto/pase/pase.ts`: pase de 30 días con hasta 30 contactos, pago único (tarjeta, OXXO o SPEI) o renovación automática (solo tarjeta) explicada con el aviso de 3 días, y la oferta con descuento cuando aplica (por ejemplo, $90 MXN, FR-027)
- [ ] T053 [P] [US4] Crear `web/src/app/features/contacto/mi-pase/mi-pase.ts`: contactos disponibles ("12 de 30"), fecha de fin, estado de la renovación y botón "Cancelar renovación" que actúa con un solo toque y confirma "No se te volverá a cobrar; conservas el pase hasta el {fin}"
- [ ] T054 [P] [US4] Crear `web/src/app/features/cuenta/mi-cuenta/mi-cuenta.ts` con enlaces a mis accesos, mi pase, mis pagos, mi perfil de prestador, "Quiero ofrecer un servicio" y cerrar sesión

### Validación del prototipo

- [ ] T055 Escribir `web/e2e/prototipo.spec.ts` (Pixel 7 e iPhone 14, con `useMocks`): alta en 10 pasos interrumpida y retomada; búsqueda de "Fontanero" y perfil sin datos de contacto (sin coincidencias de patrones de teléfono, correo ni "calle" en el DOM); "Contactar" → registro → opciones de $39 y $129 con el aviso; pago simulado con tarjeta y con OXXO; cola del administrador con decisión y motivo
- [ ] T056 Validar en un celular real los 5 puntos de la Etapa 2 de `specs/001-serviya-mvp/quickstart.md` (incluida la instalación como PWA en Android) y anotar los cambios de pantallas pedidos en `docs/PROGRESO.md`

**Checkpoint**: el usuario navega el prototipo en su celular, lo instala y aprueba las pantallas antes de empezar el backend.

---

## Phase 3: Foundational (Blocking Prerequisites)

**Purpose**: host de la API, infraestructura común, cuentas con sesión segura y catálogos.

**⚠️ CRITICAL**: ninguna historia del backend empieza antes de terminar esta fase.
**Requisito**: Docker Desktop con WSL 2 para PostgreSQL local y Testcontainers. Mientras no esté
instalado, las pruebas de integración se ejecutan en la CI de GitHub.

### Host de la API

- [ ] T057 Configurar `api/src/ServiYa.Api/Program.cs`: `AddProblemDetails` y manejador de excepciones (sin detalles internos fuera de desarrollo), `AddOpenApi`/`MapOpenApi` (solo en desarrollo), JSON camelCase con enums como texto, grupo `/api/v1`, HTTPS y HSTS, encabezados reenviados, CORS con credenciales solo para `https://serviya.mx` y `http://localhost:4200`, `/health` y puerto local 5080 en `api/src/ServiYa.Api/Properties/launchSettings.json`
- [ ] T058 [P] Configurar el registro estructurado en `api/src/ServiYa.Api/Observabilidad/RegistroSeguro.cs`: sin cuerpos, cadenas de consulta ni encabezados de autenticación en los logs, con identificador de correlación (constitución I: datos personales nunca en logs)
- [ ] T059 [P] Crear las opciones validadas al arrancar en `api/src/ServiYa.Comun/Configuracion/` (`AlmacenamientoOpciones`, `CorreoOpciones`, `VersionesLegalesOpciones`) como `record` con `ValidateDataAnnotations().ValidateOnStart()`; los secretos solo de `dotnet user-secrets` o variables de entorno (nombres en quickstart.md)
- [ ] T060 [P] Definir las versiones vigentes de `AvisoPrivacidad`, `Terminos`, `PoliticaReembolsos`, `DatosSensibles` y del aviso de no garantía en `api/src/ServiYa.Api/appsettings.json` (sección `Legal`) leídas por `VersionesLegalesOpciones`

### Datos comunes

- [ ] T061 Crear `api/src/ServiYa.Comun/Datos/EntidadBase.cs` (`Id` uuid v7 con `Guid.CreateVersion7()`, `CreatedAt`, `UpdatedAt`) y el interceptor `api/src/ServiYa.Comun/Datos/MarcasDeTiempoInterceptor.cs` que usa `TimeProvider`; convención compartida en `api/src/ServiYa.Comun/Datos/ConvencionesModelo.cs`: nombres en snake_case (paquete `EFCore.NamingConventions`, se justifica en PROGRESO.md), enums guardados como texto, `timestamptz` en UTC y montos `integer` en centavos
- [ ] T062 [P] Registrar `TimeProvider.System` en `api/src/ServiYa.Api/Program.cs` y crear `api/src/ServiYa.Comun/Tiempo/ZonaMexico.cs` (`America/Mexico_City`) para "hoy", mayoría de edad y límites diarios

### ContactMasker (crítico, TDD)

- [ ] T063 [P] Escribir primero `api/tests/ServiYa.UnitTests/Comun/ContactMaskerTests.cs` con el catálogo `api/tests/ServiYa.UnitTests/Comun/CasosEvasion.cs`: teléfonos de 8 o más dígitos con espacios, guiones, puntos o paréntesis y con +52; dígitos con palabras ("cinco cinco uno dos…") y emojis de teclas (5️⃣); correos normales y con "arroba" y "punto com"; enlaces `wa.me`, `t.me`, `instagram.com`, `facebook.com`, `fb.me`, Telegram; "WhatsApp" seguido de números; y casos que **no** se ocultan (precios "$1,500", fechas "12/05/2026", "3 horas", "2 recámaras")
- [ ] T064 Implementar `api/src/ServiYa.Comun/Privacidad/ContactMasker.cs` como servicio puro (`Enmascarar(string) → ResultadoEnmascarado(Texto, HuboOcultamiento)`, reemplazo "[dato oculto]", expresiones regulares compiladas con tiempo límite) hasta que pase T063 con al menos 80% de cobertura

### Archivos (R2)

- [ ] T065 [P] Escribir `api/tests/ServiYa.UnitTests/Comun/FirmaArchivoTests.cs`: JPEG (`FF D8 FF`), PNG (`89 50 4E 47`), WebP (`RIFF….WEBP`), PDF (`%PDF`), MP4 y MOV (`ftyp` en el byte 4), y rechazo cuando la firma no coincide con el tipo declarado
- [ ] T066 Implementar `api/src/ServiYa.Comun/Archivos/FirmaArchivo.cs` hasta que pase T065
- [ ] T067 [P] Definir `api/src/ServiYa.Comun/Archivos/IAlmacenamientoArchivos.cs` (`CrearUrlSubidaAsync` PUT de 10 min, `CrearUrlLecturaAsync` de 5 min, `LeerPrimerosBytesAsync`, `ObtenerMetadatosAsync`, `CopiarAsync` de privado a público, `EliminarAsync`) e implementarla en `api/src/ServiYa.Comun/Archivos/R2Almacenamiento.cs` con `AWSSDK.S3` (`ServiceURL` configurable y `ForcePathStyle` para MinIO); claves de objeto GUID sin datos personales
- [ ] T068 Crear la entidad `ArchivoSubido` (`Id`, `UsuarioId`, `Destino` (`Documento`, `Portafolio`, `Chat`, `Calificacion`), `Bucket`, `ClaveObjeto`, `TipoContenido`, `TamanoBytesDeclarado`, `ExpiraEn`, `ConsumidoEn?`) con `ArchivosDbContext` en el esquema `archivos` en `api/src/ServiYa.Comun/Archivos/` y su migración inicial; el servicio `ServicioArchivos.cs` emite URLs de subida y, al consumir un `archivoId`, verifica que sea del usuario, no esté vencido ni consumido, exista en el bucket, su tamaño no supere el declarado y su firma coincida
- [ ] T069 [P] Documentar la regla CORS de subida directa (`PUT` desde `https://serviya.mx` y `http://localhost:4200`, encabezado `Content-Type`) en `docs/infra/r2-cors.json` y aplicarla a MinIO en el contenedor `minio/mc` de `docker-compose.yml`

### Bitácora, correo, notificaciones y tareas programadas

- [ ] T070 Crear `api/src/ServiYa.Comun/Contratos/Bitacora/IBitacora.cs` (`RegistrarAsync(actorId, accion, entidadTipo, entidadId, detalle, ip)`) y su implementación con `RegistroBitacora` (`Id`, `ActorId`, `Accion`, `EntidadTipo`, `EntidadId`, `Detalle` jsonb **sin datos personales**, `Ip`, `CreadoEn`) y `BitacoraDbContext` en el esquema `admin` en `api/src/ServiYa.Comun/Bitacora/`; la migración incluye `REVOKE UPDATE, DELETE ON admin.registro_bitacora FROM serviya_app` ("tabla de solo inserción")
- [ ] T071 [P] Crear `api/src/ServiYa.Comun/Correo/IEmailSender.cs` con la implementación de desarrollo `ConsolaEmailSender.cs` (escribe en la consola solo en `Development`) y plantillas de texto simple en `api/src/ServiYa.Comun/Correo/Plantillas/`
- [ ] T072 [P] Crear `api/src/ServiYa.Comun/Contratos/Notificaciones/INotificador.cs` (`NotificarAsync(usuarioId, Notificacion(Titulo, Cuerpo, Url))`) con la implementación inicial por correo en `api/src/ServiYa.Comun/Notificaciones/NotificadorCorreo.cs`; el canal push se agrega en US5
- [ ] T073 [P] Escribir `api/tests/ServiYa.IntegrationTests/Comun/EjecutorTareasTests.cs`: con `FakeTimeProvider` cada tarea corre cada 5 min, una tarea que falla no detiene a las demás y dos ejecutores simultáneos no corren la misma tarea (advisory lock)
- [ ] T074 Crear `api/src/ServiYa.Comun/Tareas/ITareaProgramada.cs` (`Nombre`, `EjecutarAsync(CancellationToken)`) y `api/src/ServiYa.Api/Tareas/EjecutorTareas.cs` (`BackgroundService` con `PeriodicTimer` de 5 min, `pg_try_advisory_lock` por tarea, un `scope` por ejecución y errores registrados sin datos personales, research R7) hasta que pase T073

### Infraestructura de pruebas de integración

- [ ] T075 Crear `api/tests/ServiYa.IntegrationTests/Infraestructura/ApiFactory.cs` (`WebApplicationFactory<Program>` con un contenedor `postgis/postgis:17-3.5` de Testcontainers por colección, migraciones aplicadas, limpieza de tablas entre pruebas, `FakeTimeProvider`, `AlmacenamientoFalso` en memoria y `CorreoFalso` que guarda los códigos enviados) y `api/tests/ServiYa.IntegrationTests/Infraestructura/ClienteApi.cs` (cookies, obtención y envío de `X-XSRF-TOKEN`, `IniciarSesionComoAsync(rol)`)

### Cuentas (Identity, sesión y roles)

- [ ] T076 [P] Escribir `api/tests/ServiYa.UnitTests/Cuentas/MayoriaDeEdadTests.cs`: cumple 18 hoy, cumple mañana, nacido un 29 de febrero y cálculo en la zona de México
- [ ] T077 [P] Escribir primero `api/tests/ServiYa.IntegrationTests/Cuentas/RegistroYSesionTests.cs`: registro válido → 201 y correo con código; correo repetido; nombre fuera de 3–120 caracteres; sin aceptar aviso de privacidad y términos → 422; verificar con código correcto inicia sesión; código vencido a los 10 min; 5 intentos fallidos lo invalidan; reenviar más de 3 veces por hora → 429; login sin correo verificado → 403; recuperar responde igual exista o no el correo; la cookie es `HttpOnly`, `Secure` y `SameSite=Lax`
- [ ] T078 [P] Escribir `api/tests/ServiYa.IntegrationTests/Cuentas/XsrfYLimitesTests.cs`: `POST`, `PUT`, `PATCH` y `DELETE` sin `X-XSRF-TOKEN` → 400; con el token correcto → pasa; el 6.º login por minuto desde la misma IP → 429 con `Retry-After`
- [ ] T079 [P] Escribir primero la matriz de permisos `api/tests/ServiYa.IntegrationTests/Cuentas/PermisosPorRolTests.cs` (prueba de teoría con cada endpoint de contracts/api.md y el resultado esperado para anónimo, cliente, prestador y administrador); cada historia agrega aquí sus endpoints (crítico, 80%)
- [ ] T080 [P] Escribir `api/tests/ServiYa.IntegrationTests/Cuentas/RolAdministradorTests.cs`: solo un administrador otorga o retira el rol (FR-003); queda en bitácora `OtorgarRolAdmin` o `RetirarRolAdmin`; no se puede retirar el rol al último administrador
- [ ] T081 Crear en `api/src/Modulos/ServiYa.Cuentas/Dominio/`: `Usuario.cs` (extiende `IdentityUser<Guid>`; `Email` "Único, verificado antes de usar la cuenta", `PhoneNumber` "Formato E.164 +52; privado", `NombreCompleto` "3–120 caracteres", `FechaNacimiento` date, `Estado` `Activo`/`Suspendido`/`Eliminado`, `EliminadoEn?`), `CodigoVerificacion.cs` (`Proposito` `VerificarCorreo`/`RecuperarContrasena`, `CodigoHash`, `ExpiraEn` 10 min, `Intentos` máximo 5, `UsadoEn?`), `Consentimiento.cs` (`Documento` `AvisoPrivacidad`/`Terminos`/`PoliticaReembolsos`/`DatosSensibles`, `Version`, `AceptadoEn`, `Ip`; "Solo se agregan registros; nunca se modifican") y `MayoriaDeEdad.cs`, hasta que pase T076
- [ ] T082 Crear `api/src/Modulos/ServiYa.Cuentas/Datos/CuentasDbContext.cs` (`IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>`, esquema `cuentas`, índice único en el correo normalizado), la semilla de roles `Cliente`, `Prestador` y `Admin`, y la migración `Inicial`
- [ ] T083 Configurar Identity y la cookie en `api/src/Modulos/ServiYa.Cuentas/ModuloCuentas.cs`: contraseña de 10 caracteres o más, bloqueo tras 5 intentos, correo confirmado obligatorio, cookie `HttpOnly`, `Secure`, `SameSite=Lax`, dominio padre configurable, expiración deslizante de 14 días, y 401/403 en lugar de redirecciones
- [ ] T084 Implementar la protección CSRF en `api/src/ServiYa.Api/Seguridad/FiltroXsrf.cs` (filtro de endpoint que llama a `IAntiforgery.ValidateRequestAsync` en métodos que modifican, aplicado a todo `/api/v1` salvo el webhook) y `GET /cuentas/xsrf` que emite la cookie legible `XSRF-TOKEN` con encabezado `X-XSRF-TOKEN` (research R2)
- [ ] T085 [P] Configurar los límites de uso en `api/src/ServiYa.Api/Seguridad/LimitesDeUso.cs` con el rate limiter de ASP.NET Core: `autenticacion` (5 por minuto por IP), `mensajes` (30 por minuto por usuario) y `general`; respuesta 429 como `ProblemDetails` con `Retry-After`
- [ ] T086 Implementar los endpoints de cuentas en `api/src/Modulos/ServiYa.Cuentas/Endpoints/CuentasEndpoints.cs` según contracts/api.md (registro con aceptaciones, verificar-correo, reenviar-codigo, login, logout, recuperar, restablecer, `GET /cuentas/yo` con `esMayorDeEdad`, `POST /cuentas/yo/roles/prestador`) con validación de entrada y códigos de 6 dígitos guardados como hash, hasta que pasen T077 y T078
- [ ] T087 [P] Crear `api/src/ServiYa.Comun/Contratos/Cuentas/ICuentasService.cs` (`ObtenerResumenAsync`, `ActualizarDatosPersonalesAsync`, `ObtenerTelefonoAsync`, `RegistrarConsentimientoAsync`, `TieneConsentimientoAsync`, `EsMayorDeEdadAsync`) e implementarlo en `api/src/Modulos/ServiYa.Cuentas/Servicios/CuentasService.cs`
- [ ] T088 Implementar `POST` y `DELETE /admin/usuarios/{id}/roles/admin` en `api/src/Modulos/ServiYa.Administracion/Endpoints/RolesAdminEndpoints.cs` y el comando de arranque `dotnet run -- crear-admin --email <correo>` (promueve una cuenta ya verificada; contraseña nunca por argumento) en `api/src/ServiYa.Api/Comandos/CrearAdministrador.cs`, hasta que pase T080
- [ ] T089 Definir las políticas de autorización `Cliente`, `Prestador` y `Admin` en `api/src/ServiYa.Api/Seguridad/Politicas.cs` y aplicarlas a cada grupo de endpoints; por defecto todo endpoint exige sesión salvo los marcados como públicos, hasta que pasen las filas actuales de T079

### Catálogos

- [ ] T090 Crear en `api/src/Modulos/ServiYa.Busqueda/Dominio/` `Oficio.cs` (`Id`, `Nombre`, `Slug`, `Icono`, `Activo`) y `Municipio.cs` (`Id`, `ClaveInegi`, `Nombre`, `Estado`, `Poligono` geography(MultiPolygon), `Centroide`), con `CatalogosDbContext` (esquema `catalogos`, índice GIST en `poligono`), semilla de los 12 oficios de data-model.md y migración `Inicial` en `api/src/Modulos/ServiYa.Busqueda/Datos/`
- [ ] T091 [P] Crear el comando `dotnet run -- importar-municipios <archivo.geojson>` en `api/src/ServiYa.Api/Comandos/ImportarMunicipios.cs` (Marco Geoestadístico de INEGI) y un archivo de desarrollo `api/datos/municipios-desarrollo.geojson` con algunos municipios de ejemplo hasta definir la zona del piloto
- [ ] T092 Implementar `GET /catalogos/oficios` y `GET /catalogos/municipios` (sin polígono, caché en memoria de 1 h) en `api/src/Modulos/ServiYa.Busqueda/Endpoints/CatalogosEndpoints.cs` y `api/src/ServiYa.Comun/Contratos/Busqueda/ICatalogosService.cs` (`ExisteOficioAsync`, `ExisteMunicipioAsync`, `ObtenerCentroideAsync`)

### Frontend conectado a la API

- [ ] T093 Agregar `openapi-typescript` (dependencia de desarrollo, research R10) y el script `api:tipos` en `web/package.json` que genera `web/src/app/core/api/esquema.d.ts` desde `http://localhost:5080/openapi/v1.json`; reescribir `web/src/app/core/api/modelos.ts` como alias de los tipos generados
- [ ] T094 Implementar `web/src/app/core/http/api-interceptor.ts`: antepone `environment.apiBaseUrl`, envía `withCredentials` y copia la cookie `XSRF-TOKEN` al encabezado `X-XSRF-TOKEN` en métodos que modifican (el soporte nativo de Angular no la envía a otro origen como `api.serviya.mx`); un inicializador en `web/src/app/app.config.ts` llama a `GET /cuentas/xsrf` y a `sesion-store.cargar()`
- [ ] T095 Escribir `web/e2e/cuentas.spec.ts` contra la API real: registro, verificación con el código leído del buzón de desarrollo, cierre e inicio de sesión

**Checkpoint**: con `docker compose up -d`, la API arranca, una persona se registra, verifica su correo e inicia sesión desde la web, y la matriz de permisos pasa.

---

## Phase 4: User Story 1 - Alta y verificación del prestador (Priority: P1) 🎯 MVP

**Goal**: el prestador se da de alta por pasos con documentos ficticios, el administrador los revisa y
el perfil se publica solo tras la aprobación.

**Independent Test**: registrar un prestador con documentos ficticios, pedir corrección de la INE,
volver a subirla, aprobar todo y comprobar que el perfil queda `Aprobado` con bitácora `VerDocumento`
y `AprobarPerfil` (quickstart, escenarios 1 y 2).

### Tests for User Story 1 ⚠️

> Escribirlas primero y comprobar que fallan.

- [ ] T096 [P] [US1] Escribir `api/tests/ServiYa.UnitTests/Prestadores/TransicionesPerfilTests.cs` con todas las transiciones válidas e inválidas del diagrama de data-model.md (`Borrador → EnRevision → Aprobado / RequiereCorreccion / Rechazado`, `RequiereCorreccion → EnRevision`, `Aprobado ↔ Suspendido`, `Aprobado → Eliminado`)
- [ ] T097 [P] [US1] Escribir `api/tests/ServiYa.UnitTests/Prestadores/NombrePublicoTests.cs` ("Juan Pérez López" → "Juan P."; nombres compuestos, acentos, espacios extra y un solo nombre)
- [ ] T098 [P] [US1] Escribir `api/tests/ServiYa.UnitTests/Documentos/ReglasDocumentoTests.cs`: comprobante con expedición de máximo 3 meses; carta con `VenceEn` = expedición + 12 meses; tipos `image/jpeg`, `image/png`, `image/webp`, `application/pdf`; tamaño ≤ 10 MB; versión nueva deja la anterior en `Reemplazado`
- [ ] T099 [P] [US1] Escribir `api/tests/ServiYa.IntegrationTests/Prestadores/AltaPorPasosTests.cs`: `PATCH /prestadores/yo` guarda cada paso y `pasoAlta`; la descripción pasa por `ContactMasker`; `enviar-revision` responde 422 con la lista de faltantes; menor de 18 años → 422 con el motivo (US1 escenario 7); con todo completo → `EnRevision`
- [ ] T100 [P] [US1] Escribir primero `api/tests/ServiYa.IntegrationTests/Documentos/SubidaDocumentosTests.cs` (crítico): pedir URL de subida de un documento sin consentimiento de datos sensibles → 422; tipo o tamaño no permitidos → 422; firma que no coincide → 422; `archivoId` de otro usuario o ya consumido → 404; un prestador nunca ve documentos de otro
- [ ] T101 [P] [US1] Escribir primero `api/tests/ServiYa.IntegrationTests/Documentos/RevisionDocumentosTests.cs` (crítico): `POST /admin/documentos/{id}/ver` devuelve una URL de 5 min y registra `VerDocumento`; un no administrador recibe 403 y no se registra nada; rechazo o corrección sin motivo → 422; "requiere corrección" deja el perfil en `RequiereCorreccion` y envía el motivo al prestador; aprobar el perfil con documentos pendientes → 422; aprobado → `Aprobado`, insignia `IdentidadVerificada`, `AntecedentesPresentados` solo si la carta está aprobada, y bitácora `AprobarPerfil`
- [ ] T102 [P] [US1] Escribir `api/tests/ServiYa.IntegrationTests/Prestadores/PortafolioTests.cs`: máximo 20 fotos y 1 video; descripción ≤ 200 con `ContactMasker`; aprobar copia el objeto al bucket público; rechazar exige motivo; lo no aprobado nunca tiene URL pública
- [ ] T103 [P] [US1] Escribir `api/tests/ServiYa.ContractTests/Administracion/PerfilAdminSinUrlsTests.cs`: la respuesta de `GET /admin/prestadores/{id}` no contiene URLs ni claves de objeto de documentos

### Implementation for User Story 1

- [ ] T104 [P] [US1] Crear en `api/src/Modulos/ServiYa.Prestadores/Dominio/`: `PerfilPrestador.cs` (`UsuarioId` 1:1; `NombrePublico` "Calculado: nombre de pila + inicial del apellido"; `Descripcion` "0–600 caracteres; pasa por ContactMasker"; `Direccion` **privada**; `ColoniaBase` pública; `PuntoBase` geography(Point) "Aproximado (centroide de la colonia); privado"; `Disponibilidad` jsonb; `Estado`; `PasoAlta`; `EnviadoRevisionEn?`; `Insignias` text[] `IdentidadVerificada`/`AntecedentesPresentados`; `PromedioCalidad`, `PromedioPrecio`, `PromedioPuntualidad`, `PromedioTrato` numeric(3,2); `TotalResenas`; `PuntajeRanking`), `EstadoPerfil.cs` con los métodos de transición y `NombrePublico.cs`, hasta que pasen T096 y T097
- [ ] T105 [P] [US1] Crear `api/src/Modulos/ServiYa.Prestadores/Dominio/OficioPrestador.cs` (`AniosExperiencia` "0–60", `TarifaMin` y `TarifaMax` en centavos con mínimo ≤ máximo, `UnidadTarifa` `Hora`/`Dia`/`Servicio`, "Clave única (prestador, oficio)") y `CoberturaPrestador.cs` ("Clave única. Al menos una por prestador para enviar a revisión")
- [ ] T106 [P] [US1] Crear `api/src/Modulos/ServiYa.Prestadores/Dominio/ElementoPortafolio.cs` (`Tipo` `Foto`/`Video`, `ClaveObjetoPrivado`, `ClaveObjetoPublico?`, `Descripcion` "≤ 200, con ContactMasker", `Orden`, `EstadoModeracion` `Pendiente`/`Aprobado`/`Rechazado`, `MotivoRechazo?`; "Máximo 20 fotos y 1 video (90 s como máximo, 60 MB)")
- [ ] T107 [US1] Crear `api/src/Modulos/ServiYa.Prestadores/Datos/PrestadoresDbContext.cs` (esquema `prestadores`; índices `perfil (estado, puntaje_ranking DESC)`, `cobertura (municipio_id)`, `oficio_prestador (oficio_id)` y GIST en `punto_base`) y la migración `Inicial`
- [ ] T108 [P] [US1] Crear `api/src/Modulos/ServiYa.Documentos/Dominio/Documento.cs` (`Tipo` `IneFrente`/`IneReverso`/`Selfie`/`ComprobanteDomicilio`/`CartaAntecedentes`; `ClaveObjeto` "Bucket privado; GUID sin datos personales"; `TipoContenido` `image/jpeg`/`image/png`/`image/webp`/`application/pdf`; `TamanoBytes` "≤ 10 MB"; `Estado` `Cargado`/`Aprobado`/`RequiereCorreccion`/`Rechazado`/`Vencido`/`Reemplazado`; `Motivo` "Obligatorio en rechazo o corrección"; `FechaExpedicion` "Obligatoria para el comprobante (máximo 3 meses) y la carta"; `VenceEn` "Carta: expedición + 12 meses"; `RevisadoPor`, `RevisadoEn`) y `ReglasDocumento.cs`, hasta que pase T098
- [ ] T109 [US1] Crear `api/src/Modulos/ServiYa.Documentos/Datos/DocumentosDbContext.cs` (esquema `documentos`, índice único parcial "solo un documento vigente por tipo" por `(prestador_id, tipo)` donde el estado no es `Reemplazado`) y la migración `Inicial`
- [ ] T110 [US1] Crear `api/src/ServiYa.Comun/Contratos/Documentos/IDocumentosService.cs` (`ObtenerFaltantesAsync`, `TodosLosObligatoriosAprobadosAsync`, `TieneCartaAprobadaAsync`, `EliminarDelPrestadorAsync`) y `api/src/ServiYa.Comun/Contratos/Prestadores/IPrestadoresService.cs` (`ObtenerEstadoAsync`, `CambiarEstadoAsync`, `AgregarInsigniaAsync`, `RetirarInsigniaAsync`) con sus implementaciones en `api/src/Modulos/ServiYa.Documentos/Servicios/DocumentosService.cs` y `api/src/Modulos/ServiYa.Prestadores/Servicios/PrestadoresService.cs`
- [ ] T111 [US1] Implementar `api/src/Modulos/ServiYa.Prestadores/Servicios/AltaPrestadorService.cs`: guardado parcial por paso con validación de cada sección; los datos personales (nombre, fecha de nacimiento y teléfono) se guardan con `ICuentasService`; recalcula `NombrePublico`; `PuntoBase` = ubicación compartida por el navegador redondeada a 2 decimales (≈1 km) o, si no la comparte, el centroide del primer municipio cubierto; `EnviarARevisionAsync` exige datos completos, al menos un oficio y una cobertura, INE frente y reverso, selfie y comprobante cargados, consentimiento `DatosSensibles` y 18 años o más, y devuelve los faltantes
- [ ] T112 [US1] Implementar en `api/src/Modulos/ServiYa.Prestadores/Endpoints/AltaPrestadorEndpoints.cs`: `GET /prestadores/yo` (`PerfilPropio` con estado, paso y motivos pendientes), `PATCH /prestadores/yo`, `POST /prestadores/yo/consentimiento-sensible`, `POST /prestadores/yo/archivos/url-subida` (documentos ≤ 10 MB en jpeg, png, webp o pdf; fotos ≤ 5 MB en jpeg o webp; video ≤ 60 MB en mp4 o quicktime; documentos solo con consentimiento) y `POST /prestadores/yo/enviar-revision` (422 con faltantes en `errors`), hasta que pase T099
- [ ] T113 [US1] Implementar `POST /prestadores/yo/documentos` en `api/src/Modulos/ServiYa.Documentos/Endpoints/DocumentosEndpoints.cs`: consume el `archivoId` con `ServicioArchivos`, aplica `ReglasDocumento`, marca la versión anterior como `Reemplazado` y, si el perfil está en `RequiereCorreccion`, solo acepta los tipos observados, hasta que pase T100
- [ ] T114 [US1] Implementar `POST` y `DELETE /prestadores/yo/portafolio` en `api/src/Modulos/ServiYa.Prestadores/Endpoints/PortafolioEndpoints.cs` (límites de fotos y video, `ContactMasker` en la descripción y borrado de los objetos al eliminar)
- [ ] T115 [US1] Implementar `api/src/Modulos/ServiYa.Administracion/Servicios/RevisionService.cs`: decidir documento (motivo obligatorio salvo al aprobar; `RequiereCorreccion` o `Rechazado` cambian el estado del perfil), decidir perfil (aprobar solo con todos los obligatorios aprobados; otorga `IdentidadVerificada` y, con carta aprobada, `AntecedentesPresentados`), moderar portafolio (al aprobar copia a `serviya-publico`), con bitácora (`AprobarDocumento`, `RechazarDocumento`, `PedirCorreccion`, `AprobarPerfil`, `RechazarPerfil`, `ModerarPortafolio`) y notificación con motivo al prestador (FR-011)
- [ ] T116 [US1] Implementar en `api/src/Modulos/ServiYa.Administracion/Endpoints/RevisionEndpoints.cs`: `GET /admin/revisiones?estado=EnRevision` (por `EnviadoRevisionEn`, paginado), `GET /admin/prestadores/{id}`, `POST /admin/documentos/{id}/ver` (registra `VerDocumento` **antes** de emitir la URL de 5 min), `POST /admin/documentos/{id}/decision`, `POST /admin/prestadores/{id}/decision` y `POST /admin/portafolio/{id}/decision`, hasta que pasen T101, T102 y T103
- [ ] T117 [US1] Implementar `api/src/Modulos/ServiYa.Documentos/Tareas/VencimientoAntecedentesTarea.cs`: 30 días antes de `VenceEn` avisa al prestador una sola vez; al vencer pasa la carta a `Vencido` y retira la insignia `AntecedentesPresentados` (FR-013), con su prueba en `api/tests/ServiYa.IntegrationTests/Documentos/VencimientoAntecedentesTests.cs`
- [ ] T118 [P] [US1] Redactar las plantillas de correo de cambio de estado del perfil y de documentos (con motivo y enlace a `/alta/estado`) en `api/src/ServiYa.Comun/Correo/Plantillas/EstadoPerfil.txt` y `DocumentoObservado.txt`
- [ ] T119 [US1] Agregar los endpoints de alta y de revisión a la matriz `api/tests/ServiYa.IntegrationTests/Cuentas/PermisosPorRolTests.cs`
- [ ] T120 [US1] Conectar a la API real `web/src/app/features/alta-prestador/alta-store.ts` y el componente `web/src/app/shared/archivos/subida-archivo/subida-archivo.ts` (URL de subida → `PUT` directo a R2 con reintentos → `POST /prestadores/yo/documentos` o `/portafolio`), y ajustar las pantallas de estado y de administración a las respuestas reales
- [ ] T121 [US1] Escribir `web/e2e/alta-prestador.spec.ts` contra la API: alta completa con imágenes ficticias de `web/e2e/fixtures/documentos-ficticios/`, corrección de la INE, aprobación y perfil publicado
- [ ] T122 [US1] Ejecutar `csharp-reviewer`, `security-reviewer` y `database-reviewer` sobre `api/src/Modulos/ServiYa.Prestadores/`, `api/src/Modulos/ServiYa.Documentos/`, `api/src/Modulos/ServiYa.Administracion/` y `api/src/ServiYa.Comun/`, `typescript-reviewer` sobre `web/src/app/features/alta-prestador/` y `web/src/app/features/admin/`, corregir lo CRITICAL y HIGH y anotarlo en `docs/PROGRESO.md`

**Checkpoint**: US1 funciona de punta a punta con documentos ficticios y la cobertura de Documentos y permisos es de 80% o más.

---

## Phase 5: User Story 2 - Búsqueda y perfiles públicos (Priority: P1)

**Goal**: cualquiera busca por oficio y zona y ve perfiles públicos sin datos de contacto.

**Independent Test**: con prestadores ficticios aprobados, buscar "fontanero" en un municipio y
comprobar que solo aparecen los aprobados que cubren esa zona y que no hay datos de contacto
(quickstart, escenario 10).

### Tests for User Story 2 ⚠️

- [ ] T123 [P] [US2] Escribir `api/tests/ServiYa.UnitTests/Prestadores/CalculadoraRankingTests.cs`: promedio bayesiano `(C·m + Σ calificaciones) / (C + n)` con `C = 5`; una sola reseña de 5 no supera a 20 reseñas de 4.8 (US6 escenario 5); sin reseñas vale `m`
- [ ] T124 [P] [US2] Escribir primero `api/tests/ServiYa.ContractTests/Publico/DtosPublicosSinDatosPrivadosTests.cs` (SC-006, crítico): por reflexión, `TarjetaPrestador` y `PerfilPublico` no tienen propiedades cuyo nombre contenga telefono, phone, email, correo, direccion, documento, clave, punto, fechaNacimiento ni nombreCompleto; y el JSON de un perfil sembrado no contiene su teléfono, correo, dirección ni apellido completo
- [ ] T125 [P] [US2] Escribir `api/tests/ServiYa.IntegrationTests/Prestadores/BusquedaTests.cs`: solo aparecen perfiles `Aprobado` que cubren el municipio y ofrecen el oficio; filtros de calificación mínima, insignia y tarifa máxima; orden por ranking y luego cercanía; `tamano` máximo 50; `GET /prestadores/{id}` responde 404 para `Borrador`, `EnRevision`, `RequiereCorreccion`, `Rechazado`, `Suspendido` y `Eliminado` (US2 escenario 3)
- [ ] T126 [P] [US2] Escribir `api/tests/ServiYa.IntegrationTests/Busqueda/UbicarMunicipioTests.cs`: un punto dentro de un polígono de prueba devuelve su municipio; fuera de la zona → 404; coordenadas fuera de rango → 422

### Implementation for User Story 2

- [ ] T127 [P] [US2] Implementar `api/src/Modulos/ServiYa.Prestadores/Servicios/CalculadoraRanking.cs` (pura) hasta que pase T123
- [ ] T128 [US2] Crear los DTOs públicos dedicados `TarjetaPrestador` (`id`, `nombrePublico`, `colonia`, `municipio`, `oficios[]`, `tarifaDesde`, `promedios`, `totalResenas`, `insignias[]`, `fotoPrincipalUrl`) y `PerfilPublico` (solo los campos de FR-017) en `api/src/Modulos/ServiYa.Prestadores/Endpoints/Publico/DtosPublicos.cs`, construidos con proyecciones explícitas (nunca se serializa una entidad), hasta que pase T124
- [ ] T129 [US2] Implementar `api/src/Modulos/ServiYa.Prestadores/Servicios/BusquedaPrestadoresConsulta.cs` dentro del módulo dueño de las tablas (el centroide del municipio llega de `ICatalogosService`): filtro por estado `Aprobado`, oficio y cobertura; filtros de FR-018 (calificación = promedio de los 4 ejes); orden por `PuntajeRanking` redondeado a 1 decimal descendente, luego distancia de `PuntoBase` al centroide y luego insignias; paginación con `tamano` de 20 por defecto y 50 como máximo
- [ ] T130 [US2] Implementar `GET /prestadores` y `GET /prestadores/{id}` en `api/src/Modulos/ServiYa.Prestadores/Endpoints/Publico/PerfilesPublicosEndpoints.cs` (públicos, `Cache-Control: public, max-age=60`, URLs de fotos del dominio público de R2) hasta que pase T125
- [ ] T131 [US2] Implementar `POST /catalogos/municipios/ubicar` con cuerpo `{ lat, lng }` (la ubicación no viaja en la URL ni en los logs, constitución I) usando `ST_Covers` en `api/src/Modulos/ServiYa.Busqueda/Endpoints/CatalogosEndpoints.cs`, hasta que pase T126
- [ ] T132 [US2] Agregar los endpoints públicos de búsqueda a la matriz `api/tests/ServiYa.IntegrationTests/Cuentas/PermisosPorRolTests.cs`
- [ ] T133 [US2] Conectar `web/src/app/features/busqueda/` a la API real; el perfil público se renderiza en el servidor reutilizando la respuesta con la caché de transferencia de `HttpClient`, con `<link rel="canonical">` y `NgOptimizedImage` en la galería
- [ ] T134 [P] [US2] Fijar presupuestos en `web/angular.json` (inicial ≤ 300 KB comprimido en la app y ≤ 150 KB en el perfil público; se verifica con el tamaño gzip del reporte de compilación)
- [ ] T135 [US2] Escribir `web/e2e/busqueda.spec.ts` contra la API: búsqueda con filtros, perfil sin datos de contacto y 404 de un perfil no aprobado
- [ ] T136 [US2] Ejecutar `csharp-reviewer`, `security-reviewer` y `database-reviewer` (plan de la consulta con `EXPLAIN ANALYZE` sobre 500 perfiles ficticios) sobre la búsqueda y `typescript-reviewer` sobre `web/src/app/features/busqueda/`; anotar los hallazgos en `docs/PROGRESO.md`

**Checkpoint**: US1 y US2 funcionan juntas: lo aprobado en US1 aparece en la búsqueda y nada privado sale en las respuestas públicas.

---

## Phase 6: User Story 3 - Pago de acceso y desbloqueo de contacto (Priority: P1)

**Goal**: el cliente paga $39 MXN con tarjeta, OXXO o SPEI y, solo con el webhook verificado, se
desbloquean el chat y el teléfono por 30 días.

**Independent Test**: en modo de pruebas de Mercado Pago, pagar un contacto con tarjeta y otro con
OXXO; el primero se activa en menos de 1 minuto y el segundo solo al confirmarse (quickstart,
escenarios 3, 4, 5 y 7).

**Depende de**: US1 (prestadores aprobados). El chat (US5) fija `PrimerMensajeClienteEn`; aquí se
expone el método que lo registra.

### Tests for User Story 3 ⚠️ (módulo crítico: TDD y 80%)

- [ ] T137 [P] [US3] Escribir `api/tests/ServiYa.UnitTests/Pagos/MapeoEstadoMercadoPagoTests.cs`: `approved` → pago `Aprobado` y acceso `Activo`; `pending` e `in_process` → `Pendiente`; `rejected`, `cancelled` y referencia vencida → `Expirado`; `refunded` → `Reembolsado`; estado desconocido → sin cambio y registro de error
- [ ] T138 [P] [US3] Escribir `api/tests/ServiYa.UnitTests/Pagos/TransicionesAccesoTests.cs`: `Pendiente → Activo` (fija `ActivadoEn` y `VenceEn` = `ActivadoEn` + 30 días), `Pendiente → Expirado`, `Activo → Vencido`, `Activo → Reembolsado`; cualquier otra transición es inválida
- [ ] T139 [P] [US3] Escribir `api/tests/ServiYa.UnitTests/Pagos/FirmaWebhookTests.cs`: firma válida; `v1` alterado; `ts` alterado; `x-request-id` distinto; encabezado ausente o mal formado; la comparación es de tiempo constante
- [ ] T140 [P] [US3] Escribir primero `api/tests/ServiYa.IntegrationTests/Pagos/WebhookMercadoPagoTests.cs` con `api/tests/ServiYa.IntegrationTests/Infraestructura/MercadoPagoFalso.cs` (`HttpMessageHandler` con respuestas grabadas y ficticias en `api/tests/ServiYa.IntegrationTests/Pagos/Grabaciones/`): firma válida; firma inválida → 401 sin cambios en la base; `ts` alterado → 401; notificación duplicada → 200 sin reprocesar; `pending` que pasa a `approved`; `rejected`; pago duplicado → `Credito` `PagoDuplicado` y alerta; referencia vencida; el cuerpo de la notificación nunca se usa para montos ni estados (contracts/webhooks.md)
- [ ] T141 [P] [US3] Escribir primero `api/tests/ServiYa.IntegrationTests/Pagos/DesbloqueoContactoTests.cs` (crítico): `POST /accesos` individual crea `Pago` `Creado` y acceso `Pendiente` con `checkoutUrl`; sin webhook no se activa; `GET /accesos/{id}/contacto` → 403 si está `Pendiente`, `Vencido`, `Expirado` o es de otro cliente y 200 con teléfono si está `Activo`; acceso vigente al mismo prestador → 409 sin cobro (FR-025); menor de 18 → 403; prestador no aprobado → 404; contactarse a sí mismo → 422; `avisoVersion` distinta a la vigente → 422; el 11.º contacto nuevo del día → 429 (FR-029)
- [ ] T142 [P] [US3] Escribir `api/tests/ServiYa.IntegrationTests/Pagos/TareasAccesoTests.cs` con `FakeTimeProvider`: a los 30 días el acceso pasa a `Vencido`; un pago OXXO vencido se consulta a Mercado Pago y pasa a `Expirado`; 48 h después del primer mensaje del cliente sin respuesta → un solo `Credito` `SinRespuesta48h`; con respuesta del prestador no hay crédito
- [ ] T143 [P] [US3] Escribir `api/tests/ServiYa.IntegrationTests/Pagos/CreditosYReembolsosTests.cs`: usar un crédito activa el acceso sin pago y lo marca `Usado`; `POST /creditos/{id}/cambiar-por-reembolso` → 202, reembolso en Mercado Pago con `X-Idempotency-Key`, crédito `CambiadoPorReembolso` y pago `Reembolsado`; un crédito ya usado no se reembolsa; `POST /admin/pagos/{id}/reembolsar` registra `Reembolsar` en la bitácora
- [ ] T144 [P] [US3] Escribir `api/tests/ServiYa.IntegrationTests/Pagos/ComprobanteYFacturaTests.cs`: `GET /pagos/yo` solo muestra pagos propios; comprobante de un pago `Aprobado`; solicitud de factura con RFC de 12 o 13 caracteres válido y código postal de 5 dígitos; datos inválidos → 422

### Implementation for User Story 3

- [ ] T145 [P] [US3] Crear en `api/src/Modulos/ServiYa.Pagos/Dominio/`: `AccesoContacto.cs` (`Origen` `Individual`/`Pase`/`Credito`; `PagoId?`, `PaseId?`, `CreditoId?` "según el origen"; `Estado` `Pendiente`/`Activo`/`Vencido`/`Expirado`/`Reembolsado`; `ActivadoEn`, `VenceEn` "VenceEn = ActivadoEn + 30 días"; `PrimerMensajeClienteEn?`; `RespuestaPrestadorEn?`), `Pago.cs` (`Concepto` `ContactoIndividual`/`PaseMensual`/`RenovacionPase`/`CuotaAdministracion`; `MontoCentavos` "3900, 12900 o 12900 menos el descuento"; `DescuentoCentavos` "≥ 0"; `Metodo?`; `Estado` `Creado`/`Pendiente`/`Aprobado`/`Rechazado`/`Expirado`/`Reembolsado`; `PreferenciaMpId?`; `PagoMpId?` "Único cuando no es nulo"; `AvisoMostradoVersion`; `ConfirmadoEn?`), `NotificacionMp.cs` (`RequestId` único, `Tipo`, `DataId`, `RecibidaEn`, `ProcesadaEn?`, `Error?`), `Credito.cs` (`Motivo` `SinRespuesta48h`/`PrestadorSuspendido`/`PrestadorEliminado`/`PagoDuplicado`, `AccesoOrigenId?`, `Estado` `Disponible`/`Usado`/`CambiadoPorReembolso`, `UsadoEnAccesoId?`) y `SolicitudFactura.cs` (`Rfc`, `RazonSocial`, `RegimenFiscal`, `CodigoPostal`, `UsoCfdi`, `Estado` `Solicitada`/`Emitida`, `EmitidaEn?`), hasta que pasen T138 y T137
- [ ] T146 [US3] Crear `api/src/Modulos/ServiYa.Pagos/Datos/PagosDbContext.cs` (esquema `pagos`) con los índices: único parcial `acceso_contacto (cliente_id, prestador_id) WHERE estado IN ('Pendiente','Activo')`; único `pago (pago_mp_id) WHERE pago_mp_id IS NOT NULL`; único `notificacion_mp (request_id)`; `acceso_contacto (estado, vence_en)`; `acceso_contacto (primer_mensaje_cliente_en) WHERE respuesta_prestador_en IS NULL`; y la migración `Inicial`
- [ ] T147 [P] [US3] Crear `api/src/Modulos/ServiYa.Pagos/MercadoPago/MercadoPagoOpciones.cs` (`AccessToken`, `WebhookSecret`, `UrlNotificacion`, `UrlRetorno`, validadas al arrancar) y `api/src/Modulos/ServiYa.Pagos/PreciosOpciones.cs` (3900, 12900, 30 días, 30 contactos, límite diario 10)
- [ ] T148 [US3] Crear `api/src/Modulos/ServiYa.Pagos/MercadoPago/IMercadoPagoCliente.cs` (`CrearPreferenciaAsync`, `ObtenerPagoAsync`, `ReembolsarAsync`) y su implementación `MercadoPagoCliente.cs` con un `HttpClient` tipado de `IHttpClientFactory` (para poder usar `MercadoPagoFalso` en pruebas); usar `mercadopago-sdk` 3.7.1 solo si permite inyectar el `HttpClient` y anotar la decisión en `docs/PROGRESO.md`; la preferencia lleva título genérico sin datos personales, `external_reference` = Id del pago, `notification_url`, `back_urls` a `/pago/resultado?pagoId=`, `auto_return` y vencimiento de 3 días para OXXO
- [ ] T149 [US3] Implementar `api/src/Modulos/ServiYa.Pagos/MercadoPago/ValidadorFirmaWebhook.cs` (HMAC-SHA256 de `id:{data.id};request-id:{x-request-id};ts:{ts};` con `CryptographicOperations.FixedTimeEquals`, o el validador del SDK si existe) hasta que pase T139
- [ ] T150 [US3] Implementar `api/src/Modulos/ServiYa.Pagos/Servicios/AccesosService.cs`: `ObtenerOpcionesAsync` (acceso vigente, créditos, aviso con versión y precios), `CrearAccesoAsync` (individual: `Pago` + acceso `Pendiente` + preferencia en una transacción, con la violación del índice único traducida a 409; crédito: lo consume de forma atómica y activa sin pago), `ActivarPorPagoAsync` y `ObtenerContactoAsync` (teléfono con `ICuentasService` solo si está `Activo`); cuenta los contactos nuevos del día en la zona de México y responde 429 al pasar el límite
- [ ] T151 [P] [US3] Crear `api/src/ServiYa.Comun/Contratos/Pagos/IAccesosService.cs` (`ObtenerAccesoActivoAsync(clienteId, prestadorId)`, `RegistrarMensajeAsync(accesoId, autorEsCliente)` que fija `PrimerMensajeClienteEn` o `RespuestaPrestadorEn`, `OtorgarCreditosPorPrestadorAsync(prestadorId, motivo)`) implementado por `AccesosService`
- [ ] T152 [US3] Implementar `POST /api/v1/webhooks/mercadopago` en `api/src/Modulos/ServiYa.Pagos/Endpoints/WebhookEndpoints.cs` sin cookie ni XSRF y con cuerpo de máximo 64 KB, siguiendo el orden de contracts/webhooks.md (validar firma → insertar `NotificacionMp` → 200 → procesar en segundo plano), y `api/src/Modulos/ServiYa.Pagos/Servicios/ProcesadorNotificaciones.cs` (cola `Channel` en memoria, consulta del pago a Mercado Pago, aplicación idempotente del estado y `Credito` `PagoDuplicado` ante un segundo `approved`), hasta que pase T140
- [ ] T153 [P] [US3] Implementar `api/src/Modulos/ServiYa.Pagos/Tareas/ReintentoNotificacionesTarea.cs`: reprocesa las `NotificacionMp` sin `ProcesadaEn` con más de 2 minutos
- [ ] T154 [US3] Implementar en `api/src/Modulos/ServiYa.Pagos/Endpoints/AccesosEndpoints.cs`: `GET /accesos/opciones`, `POST /accesos`, `GET /accesos/yo`, `GET /accesos/{id}/contacto`, `GET /pagos/{id}` y `POST /creditos/{id}/cambiar-por-reembolso`, hasta que pasen T141 y T143
- [ ] T155 [US3] Implementar en `api/src/Modulos/ServiYa.Pagos/Tareas/`: `VencimientoAccesosTarea.cs` (30 días; si el cliente tiene un pase `Activo`, el acceso individual extiende su `VenceEn` al fin del pase sin consumir contactos, US3 escenario 6), `ReferenciasVencidasTarea.cs` y `CreditoSinRespuestaTarea.cs` (48 h, FR-026), hasta que pase T142
- [ ] T156 [US3] Implementar `GET /pagos/yo`, `GET /pagos/{id}/comprobante` (datos del comprobante en JSON; la web lo muestra como página imprimible) y `POST /pagos/{id}/factura` en `api/src/Modulos/ServiYa.Pagos/Endpoints/PagosEndpoints.cs`, y `GET /admin/facturas` y `POST /admin/facturas/{id}/emitida` en `api/src/Modulos/ServiYa.Administracion/Endpoints/FacturasEndpoints.cs` (research R13), hasta que pase T144
- [ ] T157 [US3] Implementar `POST /admin/pagos/{id}/reembolsar` con motivo y bitácora `Reembolsar` en `api/src/Modulos/ServiYa.Administracion/Endpoints/PagosAdminEndpoints.cs`
- [ ] T158 [P] [US3] Implementar `GET /admin/alertas` en `api/src/Modulos/ServiYa.Administracion/Endpoints/AlertasEndpoints.cs` como consulta derivada (clientes que llegaron al límite diario en los últimos 7 días y créditos `PagoDuplicado`), sin tabla nueva
- [ ] T159 [P] [US3] Notificar al cliente por correo cuando se activa su acceso o vence su referencia, con la plantilla `api/src/ServiYa.Comun/Correo/Plantillas/AccesoActivado.txt` (sin el teléfono del prestador en el correo)
- [ ] T160 [US3] Agregar los endpoints de accesos, pagos, créditos y facturas a la matriz `api/tests/ServiYa.IntegrationTests/Cuentas/PermisosPorRolTests.cs`
- [ ] T161 [US3] Conectar `web/src/app/features/contacto/` a la API real (redirección a `checkoutUrl` con `window.location.assign`), y crear `web/src/app/features/cuenta/mis-pagos/mis-pagos.ts`, `web/src/app/features/cuenta/comprobante/comprobante.ts` (estilos de impresión) y `web/src/app/features/cuenta/solicitar-factura/solicitar-factura.ts`; en `mis-accesos` agregar "Cambiar crédito por devolución"
- [ ] T162 [US3] Validar a mano con credenciales y usuarios de prueba de Mercado Pago los escenarios 3, 4, 5 y 7 de `specs/001-serviya-mvp/quickstart.md` y anotar los tiempos de activación (SC-005) en `docs/PROGRESO.md`
- [ ] T163 [US3] Ejecutar `security-reviewer`, `csharp-reviewer` y `database-reviewer` sobre `api/src/Modulos/ServiYa.Pagos/` y `typescript-reviewer` sobre `web/src/app/features/contacto/`; verificar 80% o más de cobertura en Pagos y anotarlo en `docs/PROGRESO.md`

**Checkpoint**: el MVP puede cobrar: contacto individual por tarjeta, OXXO y SPEI, con webhook verificado, vencimientos y créditos.

---

## Phase 7: User Story 4 - Pase o suscripción mensual (Priority: P2)

**Goal**: pase de 30 días con hasta 30 contactos, pago único o renovación automática con tarjeta, aviso
3 días antes, cancelación en un clic y oferta con descuento al 2.º contacto.

**Independent Test**: comprar un pase, desbloquear 30 contactos, comprobar que el 31.º pide pago y que
la cancelación de la renovación funciona en un clic (quickstart, escenario 8).

**Depende de**: US3 (accesos, pagos y webhook).

### Tests for User Story 4 ⚠️ (módulo crítico)

- [ ] T164 [P] [US4] Escribir `api/tests/ServiYa.UnitTests/Pagos/OfertaPaseTests.cs`: con un contacto individual aprobado de $39 en los últimos 30 días la oferta es de 9000 centavos; con 3 es de 1200; los reembolsados no cuentan; si el monto resultante es ≤ 0 no hay oferta; sin contactos previos tampoco
- [ ] T165 [P] [US4] Escribir primero `api/tests/ServiYa.IntegrationTests/Pagos/PaseMensualTests.cs`: compra → `checkoutUrl`; webhook `approved` → pase `Activo` con `FinEn` = inicio + 30 días; contactar con el pase activa sin pago y suma uno a `ContactosUsados`; el 31.º → 409 con oferta de contacto individual; dos solicitudes simultáneas con 29 usados → solo una pasa; al terminar el pase, sus accesos pasan a `Vencido` (solo lectura)
- [ ] T166 [P] [US4] Escribir `api/tests/ServiYa.IntegrationTests/Pagos/RenovacionPaseTests.cs`: la renovación solo se crea con tarjeta; `POST /pases/{id}/cancelar-renovacion` con una sola llamada cancela la suscripción en Mercado Pago, fija `RenovacionCanceladaEn` y conserva el pase hasta `FinEn`; el aviso se envía una sola vez 3 días antes con fecha, monto y enlace para cancelar; `subscription_authorized_payment` aprobado abre un periodo nuevo; con oferta de descuento, la suscripción empieza al terminar el periodo pagado

### Implementation for User Story 4

- [ ] T167 [US4] Crear `api/src/Modulos/ServiYa.Pagos/Dominio/PaseMensual.cs` (`InicioEn`, `FinEn` "inicio + 30 días", `ContactosIncluidos` 30, `ContactosUsados`, `RenovacionAutomatica`, `SuscripcionMpId?`, `RenovacionCanceladaEn?`, `AvisoRenovacionEnviadoEn?`, `Estado` `Pendiente`/`Activo`/`Vencido`/`Cancelado`; regla "`ContactosUsados ≤ ContactosIncluidos`, con incremento atómico") y `OfertaPase.cs`, con la migración `PaseMensual` en `api/src/Modulos/ServiYa.Pagos/Datos/Migraciones/`, hasta que pase T164
- [ ] T168 [US4] Agregar a `api/src/Modulos/ServiYa.Pagos/MercadoPago/MercadoPagoCliente.cs` `CrearSuscripcionAsync` (`preapproval` de 129 MXN mensual, `start_date` configurable) y `CancelarSuscripcionAsync` (`PUT preapproval {status: cancelled}`) con sus grabaciones en `api/tests/ServiYa.IntegrationTests/Pagos/Grabaciones/`
- [ ] T169 [US4] Implementar `api/src/Modulos/ServiYa.Pagos/Servicios/PasesService.cs`: comprar (pago único por Checkout Pro o suscripción con tarjeta; con oferta, pase de pago único por `129 − lo pagado` y suscripción con inicio al fin del periodo, research R6), contactar con el pase (`UPDATE ... WHERE contactos_usados < contactos_incluidos`), cancelar renovación y procesar los tópicos `subscription_preapproval` y `subscription_authorized_payment` en `ProcesadorNotificaciones.cs`
- [ ] T170 [US4] Implementar `POST /pases` y `POST /pases/{id}/cancelar-renovacion` en `api/src/Modulos/ServiYa.Pagos/Endpoints/PasesEndpoints.cs`, y agregar `pase` y `ofertaPase` a `GET /accesos/opciones` y la vía `Pase` a `POST /accesos`, hasta que pasen T165 y T166
- [ ] T171 [US4] Implementar en `api/src/Modulos/ServiYa.Pagos/Tareas/`: `AvisoRenovacionTarea.cs` (3 días antes, una vez, con la plantilla `api/src/ServiYa.Comun/Correo/Plantillas/AvisoRenovacion.txt`) y `VencimientoPasesTarea.cs`
- [ ] T172 [US4] Agregar los endpoints de pases a la matriz `api/tests/ServiYa.IntegrationTests/Cuentas/PermisosPorRolTests.cs`
- [ ] T173 [US4] Conectar `web/src/app/features/contacto/pase/` y `web/src/app/features/contacto/mi-pase/` a la API real y mostrar la oferta con descuento en `opciones-contacto`
- [ ] T174 [US4] Ejecutar `security-reviewer` y `csharp-reviewer` sobre los cambios de pases en `api/src/Modulos/ServiYa.Pagos/`, verificar la cobertura de 80% y anotarlo en `docs/PROGRESO.md`

**Checkpoint**: contacto individual y pase conviven; la renovación se cancela en un clic.

---

## Phase 8: User Story 5 - Chat y registro de contratación (Priority: P2)

**Goal**: chat en tiempo real con texto y fotos, ocultamiento de contacto sin acceso activo,
notificaciones y registro de la contratación.

**Independent Test**: enviar un mensaje con un teléfono antes y después del pago y comprobar que se
oculta solo antes; registrar y confirmar una contratación (quickstart, escenario 6).

**Depende de**: US3 (accesos). `ContactMasker` ya existe (Fase 3).

### Tests for User Story 5 ⚠️ (ocultamiento en el chat es crítico)

- [ ] T175 [P] [US5] Escribir `api/tests/ServiYa.UnitTests/Contrataciones/TransicionesContratacionTests.cs`: `Propuesta → Confirmada` (prestador), `Propuesta → Rechazada`, `Confirmada → Terminada` (cualquiera; fija `TerminadaEn`), `Confirmada → Cancelada`; el resto es inválido
- [ ] T176 [P] [US5] Escribir primero `api/tests/ServiYa.IntegrationTests/Chat/ChatHubTests.cs` con `Microsoft.AspNetCore.SignalR.Client`: quien no es parte no recibe ni envía; el cliente sin acceso `Activo` recibe `Error` de solo lectura; el prestador sin acceso activo puede escribir y su texto se guarda con `ContactMasker` aplicado (el cliente recibe "[dato oculto]"); con acceso activo el texto llega igual; el primer mensaje del cliente fija `PrimerMensajeClienteEn` y la primera respuesta del prestador `RespuestaPrestadorEn`; `idCliente` repetido no duplica; más de 30 mensajes por minuto → `Error`
- [ ] T177 [P] [US5] Escribir `api/tests/ServiYa.IntegrationTests/Chat/MensajesRestTests.cs`: lista de conversaciones con no leídos; paginación `antesDe`; fotos solo con URL firmada de 5 min para las partes; `Texto` ≤ 2000
- [ ] T178 [P] [US5] Escribir `api/tests/ServiYa.IntegrationTests/Contrataciones/ContratacionesTests.cs`: solo el cliente la registra; solo el prestador confirma o rechaza; `Descripcion` ≤ 500; el precio es informativo; una parte ajena → 403

### Implementation for User Story 5

- [ ] T179 [P] [US5] Crear en `api/src/Modulos/ServiYa.Chat/Dominio/`: `Conversacion.cs` ("Clave única (cliente, prestador)", `UltimoMensajeEn`), `Mensaje.cs` (`Texto` "≤ 2000", `TextoMostrado` "con ContactMasker aplicado si no había acceso activo", `ClaveFoto?`, `EnviadoEn`, `LeidoEn?`, `OcultoPorModeracion`, `IdCliente` único por autor) y `SuscripcionPush.cs` (`Endpoint`, `P256dh`, `Auth`, `CreadaEn`)
- [ ] T180 [US5] Crear `api/src/Modulos/ServiYa.Chat/Datos/ChatDbContext.cs` (esquema `chat`, índice `mensaje (conversacion_id, enviado_en)`) y la migración `Inicial`
- [ ] T181 [P] [US5] Crear `api/src/ServiYa.Comun/Contratos/Chat/IConversacionesService.cs` (`AbrirAsync(clienteId, prestadorId)`) y `api/src/ServiYa.Comun/Contratos/Chat/INotificadorTiempoReal.cs` (`AccesoActualizadoAsync`), y llamarlos desde `AccesosService.ActivarPorPagoAsync` y las tareas de vencimiento en `api/src/Modulos/ServiYa.Pagos/`
- [ ] T182 [US5] Implementar `POST /archivos/url-subida` para usuarios con sesión (destinos `Chat` y `Calificacion`, fotos jpeg o webp ≤ 5 MB) en `api/src/ServiYa.Api/Endpoints/ArchivosEndpoints.cs` reutilizando `ServicioArchivos`
- [ ] T183 [US5] Implementar `api/src/Modulos/ServiYa.Chat/Servicios/MensajeriaService.cs`: valida que el autor sea parte, que no haya bloqueo (US7 agrega el chequeo con `IBloqueos`), que el cliente tenga acceso `Activo`; aplica `ContactMasker` al prestador sin acceso activo; guarda primero y difunde después; llama a `IAccesosService.RegistrarMensajeAsync`
- [ ] T184 [US5] Implementar el hub `/hubs/chat` en `api/src/Modulos/ServiYa.Chat/Endpoints/ChatHub.cs` (autenticado por cookie; al conectarse se une a sus conversaciones; métodos `EnviarMensaje` y `MarcarLeido`; eventos `MensajeRecibido`, `AccesoActualizado` y `Error` de contracts/webhooks.md; límite `mensajes`) y los endpoints REST en `api/src/Modulos/ServiYa.Chat/Endpoints/ConversacionesEndpoints.cs`, hasta que pasen T176 y T177
- [ ] T185 [US5] Implementar Web Push en `api/src/Modulos/ServiYa.Chat/Servicios/NotificadorPush.cs` con el paquete `WebPush` y claves VAPID de configuración (`WebPush__PublicKey`, `WebPush__PrivateKey`), `POST /notificaciones/push`, carga útil `{ titulo, cuerpo, url }` sin datos sensibles, borrado de suscripciones vencidas (410) y aviso por correo si el destinatario no está conectado (máximo uno por conversación cada 30 min)
- [ ] T186 [P] [US5] Crear `api/src/Modulos/ServiYa.Contrataciones/Dominio/Contratacion.cs` (`Descripcion` "≤ 500", `FechaServicio`, `PrecioAcordadoCentavos` "solo informativo; la plataforma no lo cobra", `Estado`, `TerminadaEn?`) con sus transiciones, `ContratacionesDbContext` (esquema `contrataciones`) y la migración `Inicial`, hasta que pase T175
- [ ] T187 [US5] Implementar `POST /conversaciones/{id}/contrataciones` y `POST /contrataciones/{id}/confirmar`, `/rechazar`, `/terminar` y `/cancelar` en `api/src/Modulos/ServiYa.Contrataciones/Endpoints/ContratacionesEndpoints.cs`, y `api/src/ServiYa.Comun/Contratos/Contrataciones/IContratacionesService.cs` (`ObtenerTerminadaAsync` para US6), hasta que pase T178
- [ ] T188 [US5] Agregar los endpoints de chat, push y contrataciones a la matriz `api/tests/ServiYa.IntegrationTests/Cuentas/PermisosPorRolTests.cs`
- [ ] T189 [US5] Agregar `@microsoft/signalr` (cliente oficial, necesario para SignalR) y crear `web/src/app/features/chat/chat-conexion.ts` (conexión con reconexión automática, solo en el navegador, nunca en SSR)
- [ ] T190 [US5] Crear `web/src/app/features/chat/conversaciones/conversaciones.ts` y `web/src/app/features/chat/conversacion/conversacion.ts` (mensajes, envío de texto y foto comprimida con `idCliente` y reintento, aviso de solo lectura con el motivo y botón "Volver a contactar")
- [ ] T191 [P] [US5] Crear `web/src/app/features/chat/contratacion/contratacion.ts` (formulario de descripción, fecha y precio acordado, tarjeta de estado con acciones según el rol y el aviso "El pago del trabajo se hace directamente entre ustedes; ServiYa no lo cobra", US5 escenario 5)
- [ ] T192 [P] [US5] Crear `web/src/app/core/notificaciones/notificaciones-push.ts` con `SwPush` (permiso solo tras una explicación en pantalla; en iPhone se indica que primero hay que instalar la app)
- [ ] T193 [US5] Escribir `web/e2e/chat.spec.ts` con dos contextos de navegador (cliente y prestador): mensaje en tiempo real, teléfono oculto sin acceso y contratación confirmada
- [ ] T194 [US5] Ejecutar `csharp-reviewer`, `security-reviewer` y `typescript-reviewer` sobre `api/src/Modulos/ServiYa.Chat/`, `api/src/Modulos/ServiYa.Contrataciones/` y `web/src/app/features/chat/`; anotar los hallazgos en `docs/PROGRESO.md`

**Checkpoint**: cliente y prestador conversan, el contacto se oculta cuando corresponde y queda la contratación registrada.

---

## Phase 9: User Story 6 - Calificaciones doble ciega (Priority: P2)

**Goal**: calificación bidireccional en ejes, doble ciega por 14 días, respuesta pública y promedios
que actualizan el ranking.

**Independent Test**: terminar una contratación ficticia, calificar desde ambas cuentas y comprobar
que las calificaciones aparecen solo al final y actualizan los promedios (quickstart, escenario 9).

**Depende de**: US5 (contrataciones terminadas).

### Tests for User Story 6 ⚠️

- [ ] T195 [P] [US6] Escribir `api/tests/ServiYa.UnitTests/Calificaciones/DobleCiegaTests.cs`: se publica cuando ambas partes califican o a los 14 días de `TerminadaEn`; antes queda `Oculta`
- [ ] T196 [P] [US6] Escribir `api/tests/ServiYa.IntegrationTests/Calificaciones/CalificacionesTests.cs`: contratación no terminada → 422; segunda calificación del mismo autor → 409; persona ajena → 403; ejes del cliente (`calidad`, `precioJusto`, `puntualidad`, `trato`) y del prestador (`puntualidadPago`, `tratoCliente`, `claridad`) obligatorios entre 1 y 5; comentario ≤ 1000 con `ContactMasker`; hasta 5 fotos; oculta para la otra parte y en público hasta publicarse; al publicarse se recalculan los promedios y el `PuntajeRanking`; una sola respuesta del prestador (409 la segunda) con `ContactMasker`
- [ ] T197 [P] [US6] Agregar `ResenaPublica` a `api/tests/ServiYa.ContractTests/Publico/DtosPublicosSinDatosPrivadosTests.cs`: solo nombre público del autor, sin Id de usuario ni datos privados

### Implementation for User Story 6

- [ ] T198 [US6] Crear `api/src/Modulos/ServiYa.Calificaciones/Dominio/Calificacion.cs` (`ContratacionId` "Debe estar Terminada"; `AutorId`, `DestinatarioId` "Una calificación por autor y contratación"; `Direccion` `ClienteAPrestador`/`PrestadorACliente`; `Calidad`, `PrecioJusto`, `Puntualidad`, `Trato` smallint 1–5; `PuntualidadPago`, `TratoCliente`, `Claridad` smallint 1–5; `Comentario` "≤ 1000, con ContactMasker"; `ClavesFotos` "Hasta 5"; `Estado` `Oculta`/`Publicada`/`RetiradaPorModeracion`; `PublicadaEn?`; `Respuesta`, `RespondidaEn` "Una sola respuesta del destinatario prestador"), `CalificacionesDbContext` (esquema `calificaciones`, único `(contratacion_id, autor_id)`, índice `calificacion (estado, created_at)`) y la migración `Inicial`, hasta que pase T195
- [ ] T199 [US6] Implementar `api/src/Modulos/ServiYa.Calificaciones/Servicios/CalificacionesService.cs` (calificar, publicar si califican ambos, responder) y agregar `ActualizarPromediosAsync` a `IPrestadoresService` en `api/src/ServiYa.Comun/Contratos/Prestadores/IPrestadoresService.cs` (recalcula los 4 promedios, `TotalResenas` y `PuntajeRanking` con `CalculadoraRanking` y `m` = promedio global)
- [ ] T200 [US6] Implementar `POST /contrataciones/{id}/calificacion`, `POST /calificaciones/{id}/respuesta` y `GET /prestadores/{id}/resenas` (solo `Publicada`, DTO `ResenaPublica`) en `api/src/Modulos/ServiYa.Calificaciones/Endpoints/CalificacionesEndpoints.cs`, hasta que pasen T196 y T197
- [ ] T201 [US6] Implementar `api/src/Modulos/ServiYa.Calificaciones/Tareas/PublicacionCalificacionesTarea.cs` (publica a los 14 días de `TerminadaEn` y recalcula promedios) con su prueba en `api/tests/ServiYa.IntegrationTests/Calificaciones/PublicacionPorPlazoTests.cs`
- [ ] T202 [US6] Agregar los endpoints de calificaciones a la matriz `api/tests/ServiYa.IntegrationTests/Cuentas/PermisosPorRolTests.cs`
- [ ] T203 [US6] Crear `web/src/app/features/calificaciones/calificar/calificar.ts` (ejes como grupos de radio accesibles con estrellas, comentario y fotos), `web/src/app/features/calificaciones/pendientes/pendientes.ts`, y en `web/src/app/features/busqueda/perfil-publico/` las reseñas reales con la respuesta del prestador y el formulario para responder
- [ ] T204 [US6] Ejecutar `csharp-reviewer` y `database-reviewer` sobre `api/src/Modulos/ServiYa.Calificaciones/` y `typescript-reviewer` sobre `web/src/app/features/calificaciones/`; anotar los hallazgos en `docs/PROGRESO.md`

**Checkpoint**: las reseñas son confiables (solo con contratación terminada, doble ciega) y mueven el ranking.

---

## Phase 10: User Story 7 - Reportes, bloqueo y suspensión (Priority: P3)

**Goal**: reportar, bloquear, suspender con motivo y aclaración, todo en la bitácora.

**Independent Test**: reportar un mensaje ficticio, suspender la cuenta desde administración, enviar
una aclaración y reactivar la cuenta.

**Depende de**: US5 (mensajes) y US6 (reseñas) para los objetivos del reporte; la suspensión usa US3
para los créditos.

### Tests for User Story 7 ⚠️ (permisos críticos)

- [ ] T205 [P] [US7] Escribir `api/tests/ServiYa.IntegrationTests/Reportes/BloqueosTests.cs`: el bloqueado no puede enviar mensajes a quien lo bloqueó (por SignalR y REST); desbloquear lo permite de nuevo; no se puede bloquear a sí mismo
- [ ] T206 [P] [US7] Escribir `api/tests/ServiYa.IntegrationTests/Reportes/ReportesTests.cs`: reportar perfil, mensaje o reseña; el reporte de un mensaje llega al administrador con el contexto de la conversación (solo para el administrador); resolver con `ocultarContenido` oculta el mensaje (`OcultoPorModeracion`) o retira la reseña (`RetiradaPorModeracion`); todo queda en la bitácora
- [ ] T207 [P] [US7] Escribir primero `api/tests/ServiYa.IntegrationTests/Reportes/SuspensionesTests.cs`: suspender sin motivo → 422; suspendido → el perfil deja de aparecer en la búsqueda y por enlace directo, el usuario recibe el motivo, bitácora `SuspenderCuenta`, y los clientes con acceso `Activo` a ese prestador reciben un `Credito` `PrestadorSuspendido` (FR-043); el suspendido recibe 403 `/errores/cuenta-suspendida` salvo en `GET /cuentas/yo` y en la aclaración; aclaración → el administrador mantiene o levanta con decisión registrada; al levantar, el perfil vuelve a `Aprobado`

### Implementation for User Story 7

- [ ] T208 [P] [US7] Crear `api/src/Modulos/ServiYa.Cuentas/Dominio/Bloqueo.cs` (`BloqueadorId`, `BloqueadoId`, `CreadoEn`; "Clave única (bloqueador, bloqueado)") con su migración, `POST /bloqueos` y `DELETE /bloqueos/{usuarioId}` en `api/src/Modulos/ServiYa.Cuentas/Endpoints/BloqueosEndpoints.cs`, e `IBloqueos` en `api/src/ServiYa.Comun/Contratos/Cuentas/IBloqueos.cs` usado por `MensajeriaService`, hasta que pase T205
- [ ] T209 [P] [US7] Crear en `api/src/Modulos/ServiYa.Reportes/Dominio/` `Reporte.cs` (`ObjetivoTipo` `Perfil`/`Mensaje`/`Resena`, `ObjetivoId`, `Motivo` de catálogo, `Detalle`, `Estado` `Abierto`/`Resuelto`/`Descartado`, `Resolucion?`, `ResueltoPor?`) y `Suspension.cs` (`Motivo`, `AplicadaPor`, `AplicadaEn`, `LevantadaEn?`, `Aclaracion?`, `AclaracionEn?`, `Decision?`), con `ReportesDbContext` (esquema `reportes`) y la migración `Inicial`
- [ ] T210 [US7] Implementar `api/src/Modulos/ServiYa.Reportes/Servicios/ReportesService.cs` y `SuspensionesService.cs`: crear y resolver reportes (ocultar contenido con interfaces en `api/src/ServiYa.Comun/Contratos/` para Chat y Calificaciones); suspender (estado de cuenta `Suspendido`, perfil `Suspendido`, `IAccesosService.OtorgarCreditosPorPrestadorAsync` que pasa los accesos activos a `Vencido` y da un crédito, notificación con motivo y bitácora); aclaración; mantener o levantar
- [ ] T211 [US7] Implementar `api/src/ServiYa.Api/Seguridad/FiltroCuentaSuspendida.cs`: 403 con `type: "/errores/cuenta-suspendida"` y el motivo para cuentas suspendidas, salvo `GET /cuentas/yo` y `POST /suspensiones/yo/aclaracion`, y cierre de las conexiones de SignalR del usuario
- [ ] T212 [US7] Implementar `POST /reportes` y `POST /suspensiones/yo/aclaracion` en `api/src/Modulos/ServiYa.Reportes/Endpoints/ReportesEndpoints.cs`, y `GET /admin/reportes`, `POST /admin/reportes/{id}/resolver`, `POST /admin/usuarios/{id}/suspender` y `/reactivar` en `api/src/Modulos/ServiYa.Administracion/Endpoints/ModeracionEndpoints.cs`, hasta que pasen T206 y T207
- [ ] T213 [P] [US7] Implementar `GET /admin/bitacora?actor&accion&desde&hasta` paginado y de solo lectura en `api/src/Modulos/ServiYa.Administracion/Endpoints/BitacoraEndpoints.cs` (FR-042)
- [ ] T214 [US7] Agregar los endpoints de reportes, bloqueos, suspensiones y bitácora a la matriz `api/tests/ServiYa.IntegrationTests/Cuentas/PermisosPorRolTests.cs`
- [ ] T215 [US7] Crear `web/src/app/shared/ui/reportar/reportar.ts` (diálogo accesible con motivos) usado en el perfil público, los mensajes y las reseñas; la acción "Bloquear" en `web/src/app/features/chat/conversacion/`; y `web/src/app/features/cuenta/suspendida/suspendida.ts` (motivo y formulario de aclaración)
- [ ] T216 [US7] Crear en `web/src/app/features/admin/`: `reportes/` (cola con contexto), `usuario/` (suspender o reactivar con motivo y otorgar o retirar el rol de administrador), `aclaraciones/`, `bitacora/` (filtros), `alertas/`, `facturas/` (marcar emitida) y `reembolsos/`
- [ ] T217 [US7] Ejecutar `security-reviewer` y `csharp-reviewer` sobre `api/src/Modulos/ServiYa.Reportes/`, `api/src/Modulos/ServiYa.Administracion/` y `api/src/ServiYa.Api/Seguridad/`, y `typescript-reviewer` sobre `web/src/app/features/admin/`; anotar los hallazgos en `docs/PROGRESO.md`

**Checkpoint**: la plataforma se puede moderar antes de abrirla a usuarios reales.

---

## Phase 11: User Story 8 - Privacidad y control de la cuenta (Priority: P3)

**Goal**: textos legales con versión y aceptación registrada, descarga de datos y eliminación de cuenta.

**Independent Test**: eliminar una cuenta de prestador ficticia y comprobar que el perfil desaparece y
los documentos se borran, conservando los registros de pagos.

### Tests for User Story 8 ⚠️

- [ ] T218 [P] [US8] Escribir `api/tests/ServiYa.IntegrationTests/Cuentas/EliminacionCuentaTests.cs`: contraseña incorrecta → 403; confirmada → 202, perfil `Eliminado` e invisible al instante, objetos de documentos y portafolio borrados del almacenamiento, datos personales anonimizados, sesiones invalidadas, clientes con acceso activo reciben `Credito` `PrestadorEliminado` y los pagos se conservan
- [ ] T219 [P] [US8] Escribir `api/tests/ServiYa.IntegrationTests/Cuentas/DescargaDatosTests.cs`: `GET /cuentas/yo/datos` incluye los datos de cada módulo del propio usuario y ningún dato personal de terceros
- [ ] T220 [P] [US8] Escribir `api/tests/ServiYa.IntegrationTests/Cuentas/ConsentimientosTests.cs`: al subir la versión de los términos, `GET /cuentas/yo` devuelve `aceptacionesPendientes`; `POST /cuentas/yo/consentimientos` registra la aceptación con fecha, versión e IP sin modificar las anteriores

### Implementation for User Story 8

- [ ] T221 [US8] Crear `api/src/ServiYa.Comun/Contratos/Privacidad/IDatosPersonalesModulo.cs` (`ExportarAsync(usuarioId)` y `EliminarAsync(usuarioId)`) e implementarlo en cada módulo (`Cuentas`, `Prestadores`, `Documentos`, `Pagos` (exporta pero conserva lo fiscal), `Chat`, `Contrataciones`, `Calificaciones` y `Reportes`) en su carpeta `Servicios/DatosPersonales.cs`
- [ ] T222 [US8] Implementar `GET /cuentas/yo/datos` y `DELETE /cuentas/yo` en `api/src/Modulos/ServiYa.Cuentas/Endpoints/PrivacidadEndpoints.cs` (estado `Eliminado`, `EliminadoEn`, anonimización, cambio del sello de seguridad, borrado por módulo y créditos a clientes afectados), hasta que pasen T218 y T219
- [ ] T223 [US8] Agregar `aceptacionesPendientes` a `GET /cuentas/yo` y el endpoint `POST /cuentas/yo/consentimientos` en `api/src/Modulos/ServiYa.Cuentas/Endpoints/CuentasEndpoints.cs`, hasta que pase T220
- [ ] T224 [P] [US8] Crear las páginas `web/src/app/features/legal/aviso-privacidad/`, `terminos/` y `politica-reembolsos/` con su versión y la marca "BORRADOR — pendiente de revisión por un abogado", enlazadas desde el pie, el registro y las opciones de pago
- [ ] T225 [US8] Crear `web/src/app/features/legal/aceptar-cambios/aceptar-cambios.ts` que aparece cuando `aceptacionesPendientes` no está vacío
- [ ] T226 [US8] Crear `web/src/app/features/cuenta/mis-datos/mis-datos.ts` (descargar en JSON) y `web/src/app/features/cuenta/eliminar-cuenta/eliminar-cuenta.ts` (explicación de qué se borra y qué se conserva por ley, contraseña y confirmación)
- [ ] T227 [P] [US8] Redactar `docs/legal/politica-conservacion.md` (qué se borra al eliminar la cuenta, qué se conserva —pagos, bitácora— y por cuánto tiempo) para revisión del abogado y el contador
- [ ] T228 [US8] Ejecutar `security-reviewer` sobre la eliminación y descarga de datos en todos los módulos y anotar los hallazgos en `docs/PROGRESO.md`

**Checkpoint**: se cumplen los derechos ARCO desde la app y queda registro de cada aceptación.

---

## Phase 12: Polish & Cross-Cutting Concerns (Etapas 9 y 10)

**Purpose**: seguridad, rendimiento, accesibilidad, despliegue y preparación del piloto.

- [ ] T229 [P] Agregar encabezados de seguridad a la API en `api/src/ServiYa.Api/Seguridad/EncabezadosSeguridad.cs` (`X-Content-Type-Options`, `Referrer-Policy: no-referrer`, `Cache-Control: no-store` en respuestas privadas)
- [ ] T230 [P] Activar `security.autoCsp` en `web/angular.json` y fijar la CSP y los encabezados en `web/src/server.ts` (`img-src` y `media-src` con los dominios de R2, `connect-src` con la API y `wss:`, `frame-ancestors 'none'`)
- [ ] T231 [P] Configurar el umbral de 80% de cobertura para `ServiYa.Pagos`, `ServiYa.Documentos` y `ServiYa.Comun` (`ContactMasker` y archivos) en `api/coverage.runsettings` y hacer que `.github/workflows/ci.yml` falle si no se alcanza
- [ ] T232 [P] Agregar `@axe-core/playwright` (desarrollo; justificado por la auditoría WCAG AA) y `web/e2e/accesibilidad.spec.ts` sobre inicio, resultados, perfil, alta, opciones de pago y chat
- [ ] T233 [P] Medir LCP < 2.5 s en búsqueda y perfil con 4G de gama media (Lighthouse en móvil) y anotar los resultados en `docs/PROGRESO.md`
- [ ] T234 [P] Generar `sitemap.xml` de perfiles aprobados y `robots.txt` (sin indexar `/cuenta`, `/alta`, `/admin` ni `/contactar`) en `web/src/server.ts`
- [ ] T235 Implementar el interruptor de cuota de administración (FR-014) en `api/src/Modulos/ServiYa.Prestadores/CuotaAdministracionOpciones.cs` (`Activa: false` en el piloto): cuando se active, aprobar el perfil crea un `Pago` `CuotaAdministracion` y lo publica al confirmarse
- [ ] T236 Implementar el proveedor de correo de producción (Resend o Brevo, research R9) en `api/src/ServiYa.Comun/Correo/` con dominio verificado (SPF y DKIM) y registrar la elección en `docs/PROGRESO.md`
- [ ] T237 [P] Crear `api/Dockerfile` (compilación en varias etapas, imagen final sin raíz) y `web/Dockerfile` (SSR en Node 24)
- [ ] T238 Crear `render.yaml` con los servicios `serviya-api` (siempre encendido, por R7) y `serviya-web`, con los nombres de las variables de entorno de quickstart.md y sin valores secretos
- [ ] T239 Escribir `docs/despliegue.md`: Neon con PostGIS y los roles `serviya_migraciones` y `serviya_app`, migraciones con `dotnet ef migrations bundle`, buckets y CORS de R2, credenciales de producción y URL del webhook de Mercado Pago, claves VAPID, dominio y respaldos diarios (restauración de prueba)
- [ ] T240 Actualizar `specs/001-serviya-mvp/quickstart.md` y la sección "Comandos" de `CLAUDE.md` con los comandos reales
- [ ] T241 Ejecutar los 10 escenarios de `specs/001-serviya-mvp/quickstart.md` con datos ficticios y una revisión final con los 4 agentes revisores; anotar el resultado en `docs/PROGRESO.md`
- [ ] T242 Preparar el piloto (Etapa 10) según los pendientes de `docs/PROGRESO.md`: importar los municipios de la zona elegida con `importar-municipios`, ajustar los oficios, publicar los textos legales revisados por el abogado, crear la cuenta de administrador y hacer un pago real pequeño con su reembolso

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Fase 1)**: sin dependencias.
- **Prototipo (Fase 2)**: depende de la parte de `web/` de la Fase 1 (T003–T010). No
  necesita la API ni Docker.
- **Fundamentos (Fase 3)**: depende de la Fase 1 y de que el usuario apruebe el prototipo (T056).
  **Bloquea** todas las historias del backend.
- **US1 (Fase 4)** y **US2 (Fase 5)**: dependen de la Fase 3. US2 necesita perfiles aprobados para
  probarse de punta a punta, pero sus pruebas siembran sus propios datos.
- **US3 (Fase 6)**: depende de la Fase 3 y de US1 (prestadores aprobados).
- **US4 (Fase 7)**: depende de US3.
- **US5 (Fase 8)**: depende de US3 (accesos).
- **US6 (Fase 9)**: depende de US5 (contrataciones terminadas).
- **US7 (Fase 10)**: depende de US5 y US6 (objetivos de reporte) y de US3 (créditos).
- **US8 (Fase 11)**: depende de todos los módulos con datos personales (US1–US7).
- **Cierre (Fase 12)**: depende de las historias que entren al piloto.

### User Story Dependencies

```text
Setup ─▶ Prototipo ─▶ Fundamentos ─┬─▶ US1 ─▶ US3 ─┬─▶ US4
                                   │               └─▶ US5 ─▶ US6 ─▶ US7 ─▶ US8 ─▶ Cierre
                                   └─▶ US2
```

### Within Each User Story

- Las pruebas se escriben primero y deben fallar (obligatorio en los módulos críticos).
- Entidades → DbContext y migración → servicios → endpoints → frontend → E2E → revisores.
- Cada historia agrega sus endpoints a la matriz de permisos antes de cerrarse.
- Commit por tarea o grupo lógico, con `docs/PROGRESO.md` actualizado en el mismo commit.

### Parallel Opportunities

- Fase 1: T002, T005, T006, T009, T010, T011, T014, T015 y T016 en paralelo una vez creados los proyectos.
- Fase 2: las pantallas marcadas [P] de US1–US4 son archivos independientes.
- Fase 3: `ContactMasker`, `FirmaArchivo`, correo, notificador y catálogos son independientes entre sí.
- Dentro de cada historia: todas las pruebas [P] y las entidades [P].
- US2 puede avanzar en paralelo con US1 tras la Fase 3; US5 y US4 pueden avanzar en paralelo tras US3.

---

## Parallel Example: User Story 3

```bash
# Pruebas de US3 en paralelo (deben fallar primero):
Task: "MapeoEstadoMercadoPagoTests en api/tests/ServiYa.UnitTests/Pagos/"
Task: "TransicionesAccesoTests en api/tests/ServiYa.UnitTests/Pagos/"
Task: "FirmaWebhookTests en api/tests/ServiYa.UnitTests/Pagos/"
Task: "WebhookMercadoPagoTests en api/tests/ServiYa.IntegrationTests/Pagos/"
Task: "DesbloqueoContactoTests en api/tests/ServiYa.IntegrationTests/Pagos/"

# Después, en paralelo:
Task: "Entidades de Pagos en api/src/Modulos/ServiYa.Pagos/Dominio/"
Task: "MercadoPagoOpciones y PreciosOpciones"
Task: "IAccesosService en api/src/ServiYa.Comun/Contratos/Pagos/"
```

## Parallel Example: Prototipo (Fase 2)

```bash
Task: "Pasos 1–3 del alta en web/src/app/features/alta-prestador/pasos/"
Task: "Resultados de búsqueda en web/src/app/features/busqueda/resultados/"
Task: "Opciones de contacto en web/src/app/features/contacto/opciones-contacto/"
Task: "Cola de revisión en web/src/app/features/admin/cola-revision/"
```

---

## Implementation Strategy

### MVP primero

1. Fase 1 (Setup) → Fase 2 (Prototipo) → **validación del usuario en su celular**.
2. Fase 3 (Fundamentos) → Fase 4 (US1) → **detener y validar US1** con documentos ficticios.
3. Fase 5 (US2) y Fase 6 (US3): con ellas el MVP ya conecta clientes con prestadores y cobra.
   Es el mínimo para un piloto cerrado.

### Entrega incremental

1. Setup + prototipo → pantallas aprobadas.
2. Fundamentos + US1 → prestadores verificados (se puede empezar a captar prestadores).
3. US2 → vitrina pública.
4. US3 → primeros ingresos (contacto individual).
5. US4 → pase mensual.
6. US5 + US6 → chat, contrataciones y reputación.
7. US7 + US8 + cierre → moderación, privacidad, despliegue y piloto.

### Un solo desarrollador

Seguir el orden de prioridad; las marcas [P] sirven para agrupar tareas en una misma sesión de
trabajo con Claude, no para repartirlas entre personas.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- [Story] relaciona cada tarea con su historia de `spec.md`.
- Nunca usar documentos reales ni datos personales reales en pruebas, mocks o capturas.
- Verificar que las pruebas fallan antes de implementar en los módulos críticos.
- Cerrar cada fase con su checkpoint y los revisores indicados.
