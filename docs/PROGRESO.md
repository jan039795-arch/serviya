# Progreso de ServiYa

> Memoria del proyecto. Léela al iniciar cada sesión y actualízala con cada cambio (ver `CLAUDE.md`).

## Estado actual

**Etapa 2 — Prototipo**: terminada salvo la aprobación del usuario en su celular (T037).
**Etapa 3 — Cuentas y perfiles** (en curso): Fase 3 lista (T038, T040–T043) salvo probar
`docker-compose.yml` (T039), que espera a que el usuario instale Docker. De la Fase 4 están hechas
T044, T045, T047, T048, T051, T053–T057 y T060; lo que falta necesita PostgreSQL o RustFS.

- Constitución v1.0.1 (Angular estable vigente, hoy 22).
- Especificación del MVP aprobada por el usuario: `specs/001-serviya-mvp/spec.md` (8 historias,
  FR-001 a FR-046, SC-001 a SC-010).
- Plan técnico listo en `specs/001-serviya-mvp/`: `plan.md`, `research.md` (R1–R13),
  `data-model.md`, `contracts/api.md`, `contracts/webhooks.md` y `quickstart.md`.
- Tareas listas: `specs/001-serviya-mvp/tasks.md` (T001–T151 en 13 fases; TDD en módulos críticos).
- Skills, reglas y agentes de ECC copiados y adaptados en `.claude/`.
- `web/`: Angular 22.2 con SSR, PWA, ESLint, Prettier, Vitest y Playwright 1.65 (Pixel 7 e iPhone 14);
  entornos con `mocks` por área y configuración `prototipo`; idioma es-MX; estilos base con contraste AA.
  Íconos de la PWA provisionales (los de Angular) hasta tener nombre y logotipo definitivos.
- Base del prototipo: tipos de los contratos (`core/api/modelos.ts`), datos ficticios en `src/mocks/`
  (12 oficios, 6 municipios inventados, 14 prestadores, 34 reseñas, 3 sesiones, 2 revisiones e
  imágenes SVG "DOCUMENTO FICTICIO" que solo se sirven en desarrollo y prototipo),
  `MockApiInterceptor` con rutas por área cargadas de forma diferida, `ErrorApi` y avisos,
  `SesionService` y guards `canMatch`, armazón con navegación inferior y selector de rol del
  prototipo, componentes de UI, pipe `mxn` y subida con compresión, avance y reintento.
- Pantallas de US1 (T019–T024): registro, verificación con código (en el prototipo, `123456`),
  entrar con ruta de regreso validada y recuperación de contraseña; alta del prestador en 10 pasos
  con Signal Forms, guardado por paso y regreso al último paso; consentimiento, documentos con
  fecha de expedición, video y fotos, revisión con faltantes y estado del perfil con corrección de
  documentos señalados. Los manejadores simulados de cuentas, catálogos y prestadores comparten su
  estado en `core/mocks/estado/` (usuarios y perfiles en `localStorage`; nunca contraseñas).
- Revisión del administrador (T025): cola por fecha de envío con aviso de "Corrección reenviada",
  detalle con datos privados, visor de documentos que registra `VerDocumento` en una bitácora
  simulada, decisiones por documento y por foto con motivo obligatorio, y decisión del perfil
  (aprobar solo con los obligatorios aprobados, pedir correcciones o rechazar). La cola se siembra
  con `revisiones.json` y comparte estado con el alta, así que el flujo completo de US1 se prueba
  de punta a punta en E2E.
- Búsqueda y perfil público (T026–T029): inicio con oficio, municipio y "Usar mi ubicación";
  resultados con filtros en la URL (calificación, insignia, tarifa máxima) y "Ver más"; perfil
  público con oficios, tarifas, horario, fotos, calificaciones por eje y reseñas con respuesta, y
  página "no disponible" para perfiles no aprobados. El directorio simulado
  (`core/mocks/estado/directorio.ts`) une `prestadores.json` con los perfiles aprobados en el
  prototipo y arma los DTO públicos campo por campo; una prueba unitaria revisa que nunca salgan
  datos privados. Orden con promedio bayesiano más un pequeño bono de cercanía.
- Contacto y pase (T030–T033): opciones de contacto con el aviso de no garantía, contacto de $39,
  pase de $129 con oferta de descuento al segundo contacto, uso de pase y de crédito; checkout
  simulado (tarjeta aprobada, OXXO pendiente, rechazo); resultado del pago con consulta cada 3 s;
  "Mis contactos" con teléfono y WhatsApp solo con acceso activo, acreditación y vencimiento
  simulados; pase con cancelación de la renovación en un clic. Un visitante que pulsa "Contactar"
  se registra y regresa a las opciones. Las reglas (vencimientos, sin doble cobro, oferta, límite
  de 10 contactos por día) están en `core/mocks/estado/reglas-acceso.ts` con pruebas unitarias.
- Cierre (T034–T037): Mi cuenta; `npm run serve:prototipo` y guía de prueba en Android en
  `quickstart.md`; botón "Reiniciar datos". `typescript-reviewer`: 0 CRITICAL, 3 HIGH, 8 MEDIUM,
  6 LOW. Corregidos H1 (la simulación ya no entra al paquete de producción: `core/prototipo/
  prototipo.ts` se reemplaza por `prototipo.prod.ts`; verificado con búsqueda en `dist/`), H3
  (sondeo del resultado de pago tras salir), M3, M4, M5, M6, M7, M8, L1 y L5. Quedan para conectar
  la API real: H2 (validar respuestas, en T068) y M2 (dominio de R2 en las subidas, en T085).
  También se corrigió `security.allowedHosts`: el servidor SSR rechazaba hasta `localhost`.
- 80 pruebas unitarias y 70 E2E (Pixel 7 e iPhone 14) pasan, estables en corridas repetidas.
- Backend (Fase 3): `api/ServiYa.sln` con `ServiYa.Api`, `ServiYa.Comun`, los 10 módulos (vacíos
  por ahora) y 3 proyectos de pruebas (xUnit v3 sobre Microsoft.Testing.Platform). Versiones
  centralizadas en `api/Directory.Packages.props`; advertencias como errores con
  `latest-recommended`. Opciones `Storage`, `MercadoPago`, `Email` y `WebPush` validadas al
  arrancar. 8 pruebas unitarias y 3 de integración (arranque de la API, sin Docker) pasan.
- `docker-compose.yml`: PostGIS 17 con roles `serviya_owner` y `serviya_app` (`docker/postgres/init.sql`)
  y RustFS 1.0.4 como S3 local con los buckets `serviya-privado` y `serviya-publico`. **Escrito pero
  sin probar: Docker ni WSL están instalados.**
- Secretos de desarrollo: `scripts/configurar-desarrollo.ps1` crea `.env` (ignorado por git) y los
  user-secrets de la API; ya se ejecutó en este equipo y la API arranca en desarrollo.
- CI en `.github/workflows/ci.yml` (API y web); sus pasos pasan en local; correrá en GitHub al subir.
- Host de la API (Fase 4): `IModulo` con los 10 módulos registrados en una lista explícita
  (`ServiYa.Api/Modulos/RegistroModulos.cs`) bajo `/api/v1`; errores como `ProblemDetails` con
  títulos en español; `TimeProvider` inyectable; HTTPS, HSTS, CORS con credenciales y encabezados
  de seguridad; límites `login-codigos` (5/min por IP) y `mensajes` (30/min por usuario) con 429 y
  `Retry-After`; registro JSON con solo método, ruta, estado y duración.
- `ContactMasker` (`ServiYa.Comun/Privacidad/`): oculta teléfonos (con separadores, `+52`, con
  palabras o mezclados), correos (también "arroba" y "punto com"), enlaces y cuentas de WhatsApp,
  Telegram, Instagram, Facebook y TikTok, y números tras "WhatsApp". Catálogo de 35 evasiones y 16
  textos que no se tocan en `CasosEvasion.cs`; cobertura 96%.
- Correo: `IEmailSender` con `ConsolaEmailSender` (memoria, `GET /api/v1/dev/correos` solo en
  desarrollo) y `ResendEmailSender` (HTTP sin SDK); plantillas de código de verificación y de
  recuperación.
- Persistencia (T045): `AplicarConvencionesServiYa()` al final de cada `OnModelCreating` pone
  snake_case, `created_at`/`updated_at` (propiedades sombra que llena `MarcasTiempoInterceptor` con
  `TimeProvider`) y claves uuid v7; `AgregarDbContextDeModulo<T>` registra cada `DbContext` con
  PostGIS, su esquema y su historial de migraciones.
- Cuentas (T056, T057, T060): `Usuario : IdentityUser<Guid>` (nombre 3–120, fecha de nacimiento,
  estado, mayoría de edad con la hora de México), `CodigoVerificacion`, `Consentimiento` (solo se
  agrega, IP en `inet`) y `Bloqueo` (no a uno mismo); `ServicioCodigos` (6 dígitos, 10 min, 5
  intentos, 3 envíos por hora, solo el hash); `CuentasDbContext` en el esquema `cuentas` con las
  tablas de Identity renombradas (`usuarios`, `roles`...) y la migración `Inicial` generada (**sin
  aplicar todavía**). Una prueba falla si el modelo cambia sin su migración.
- 137 pruebas del backend pasan (unitarias e integración sin base de datos).
- Repositorio respaldado en GitHub (privado): `https://github.com/jan039795-arch/serviya`, rama `main`.

## Siguiente paso exacto

1. **El usuario prueba el prototipo en su celular** siguiendo "Probar en el celular e instalar la
   app" de `specs/001-serviya-mvp/quickstart.md` (`npm run serve:prototipo` + `chrome://inspect`)
   y anota en esta bitácora los ajustes que pida. Con su aprobación se marca T037.
2. **El usuario instala WSL 2 y Docker Desktop** (`wsl --install` como administrador, reiniciar y
   luego Docker Desktop). Después: `docker compose up -d` desde la raíz y comprobar que
   `almacen-buckets` termina bien y que `serviya_app` entra a PostgreSQL; con eso se marca T039.
3. Con Docker: aplicar la migración `Inicial` (`dotnet tool restore` y `dotnet ef database update`,
   ver `CLAUDE.md`) y seguir con T046 (fábrica de pruebas con PostGIS y RustFS; reutilizar
   `Infraestructura/FabricaSinBaseDeDatos.cs` y `RegistroEnMemoria.cs`), T049 (bitácora), T050
   (almacenamiento), T052 (tareas programadas), T058 (pruebas de cuentas y permisos, TDD), T059
   (Identity y cookie), T061–T064 y catálogos (T065–T067).
4. Sin Docker todavía se puede adelantar la parte pura de T050 (`ValidadorFirmaArchivo` y las URL
   firmadas, que se calculan sin conectarse) y de T052 (planificador con un candado simulado).
5. Decidir el pendiente de la oferta del pase cuando lo pagado supera $129 (ver "Pendientes").

## Etapas del proyecto

| # | Etapa | Estado |
|---|---|---|
| 1 | Definición (constitución, especificación, plan, tareas) | Terminada |
| 2 | Prototipo móvil con datos ficticios | Lista; falta la aprobación del usuario en su celular |
| 3 | Cuentas y perfiles | En curso: preparación del backend lista; falta Docker |
| 4 | Documentos y panel del administrador | Pendiente |
| 5 | Búsqueda y perfil público | Pendiente |
| 6 | Pagos y desbloqueo de contacto | Pendiente |
| 7 | Chat y registro de contratación | Pendiente |
| 8 | Calificaciones y reportes | Pendiente |
| 9 | Legal y detalles finales | Pendiente |
| 10 | Piloto | Pendiente |

## Decisiones

| Fecha | Decisión | Motivo |
|---|---|---|
| 2026-11-05 | Solo se cobra por contacto y administración; sin comisión ni manejo del pago del trabajo | Modelo legal y operativo simple para una sola persona |
| 2026-11-05 | $39 MXN por contacto individual (30 días); $129 MXN pase mensual con hasta 30 contactos | Modelo tipo Superprof; el pase conviene desde el 4.º contacto |
| 2026-11-05 | Pase como pago único (OXXO/SPEI/tarjeta) o renovación automática solo con tarjeta, aviso 3 días antes y cancelación en un clic | Mercado Pago solo renueva con tarjeta; evitar quejas por cobros inesperados |
| 2026-11-05 | Al 2.º contacto pagado en 30 días se ofrece el pase descontando lo pagado | Aumentar conversión a pase |
| 2026-11-05 | Crédito automático si el prestador no responde en 48 h; devolución en dinero si se pide | Confianza; los reembolsos cuestan comisión |
| 2026-11-05 | Límite de 10 contactos nuevos por día por cliente | Evitar extracción masiva de teléfonos |
| 2026-11-05 | Carta de no antecedentes voluntaria (insignia); exigirla por oficio requiere validación legal | Riesgo de discriminación por antecedentes penales |
| 2026-11-05 | Calificación en 4 ejes (calidad, precio justo, puntualidad/rapidez, trato), doble ciega, bidireccional, solo con contratación terminada | Reseñas confiables y no fabricables |
| 2026-11-05 | Alta del prestador gratis en el piloto; cuota de administración después, cobrada al aprobar | Conseguir prestadores es lo más difícil al inicio |
| 2026-11-05 | Stack: Angular 20+ (PWA+SSR), ASP.NET Core 10, EF Core, Identity, SignalR, PostgreSQL, Cloudflare R2, Mercado Pago | Experiencia del usuario en C#; menos memoria y hosting más barato que Spring Boot |
| 2026-11-05 | Monolito modular, sin microservicios | Un solo desarrollador |
| 2026-11-05 | Primero un prototipo con datos ficticios | Validar pantallas antes de manejar documentos y dinero reales |
| 2026-11-05 | Usar Spec Kit como flujo; de ECC solo 12 skills, 18 reglas y 4 agentes, sin hooks ni plugin completo | Evitar dos flujos de planeación y la lentitud de los hooks |
| 2026-11-05 | AwesomeAssertions en lugar de FluentAssertions | FluentAssertions 8+ requiere licencia comercial |
| 2026-11-05 | Errores de la API como ProblemDetails; sin repositorio genérico sobre EF Core | Estándar de ASP.NET Core; EF Core ya es unidad de trabajo |
| 2026-11-05 | Angular 22 (estable vigente) en lugar de 20 | Evitar una migración inmediata |
| 2026-11-05 | Sesión con cookie HttpOnly + antiforgery (XSRF), verificación por correo; SMS fuera del MVP | Más seguro que un JWT en localStorage; funciona con SignalR y SSR |
| 2026-11-05 | Zonas por municipio o alcaldía (polígonos INEGI) con PostGIS, sin geocodificación de pago | Costo cero; suficiente para una ciudad |
| 2026-11-05 | Checkout Pro para pagos únicos; Suscripciones solo con tarjeta; el descuento al pasar al pase se cobra como pase de pago único | Una sola integración; Mercado Pago no descuenta el primer cobro de una suscripción |
| 2026-11-05 | Tareas programadas con BackgroundService y advisory lock (sin Hangfire) | Principio V; requiere una instancia siempre encendida (~7 USD/mes) |
| 2026-11-05 | Facturas de cuotas: solicitud en la app y CFDI emitido fuera del sistema en el MVP | Bajo volumen en el piloto |
| 2026-11-08 | `ubicar` municipio por `POST` con `{ lat, lng }` en el cuerpo, no `GET ?lat&lng` (T067) | La ubicación es dato personal y no debe ir en la URL (principio I) |
| 2026-11-08 | Interceptor XSRF propio en Angular (T069) | El soporte XSRF de `HttpClient` omite URLs absolutas y la API estará en `api.serviya.mx` |
| 2026-11-08 | La búsqueda de prestadores vive en el módulo Prestadores; Busqueda solo da catálogos y municipios | Un módulo no consulta tablas de otro |
| 2026-11-08 | Los accesos obtenidos con pase vencen en el `FinEn` del pase | Coherente con US4 escenario 6 |
| 2026-11-08 | Prototipo con `mocks` por área en `environment` | Conectar la API real área por área sin tocar componentes |
| 2026-11-08 | En el prototipo todas las rutas se renderizan en el navegador (`RenderMode.Client`) | El SSR del perfil público llega en T094; evita prerenderizar con datos ficticios |
| 2026-11-08 | Subidas con `XMLHttpRequest` en `Subidor`, no con `HttpClient` | `withFetch()` no da avance de subida, y la URL firmada de R2 no debe recibir la cookie ni el XSRF de los interceptores |
| 2026-11-08 | Guards como `canMatch` | La ruta restringida no carga su código sin permiso (regla de Angular del proyecto) |
| 2026-11-08 | Presupuesto de Angular en tamaño sin comprimir: aviso 600 kB, error 1 MB | Angular mide sin gzip; el límite del plan es 300 KB gzip (hoy 89 KB) y se mide en T145 |
| 2026-11-08 | Imágenes ficticias en SVG y servidas solo en desarrollo y prototipo | Sin herramientas extra; no llegan al paquete de producción |
| 2026-11-08 | El correo por verificar se guarda en memoria y no en la URL (`/verificar` sin `?email=`) | Principio I: datos personales fuera de la URL |
| 2026-11-08 | Un menor puede registrarse, pero no pagar ni enviar su perfil (aviso en el registro y bloqueo al enviar) | US1 escenario 7 y FR-004; el bloqueo real está en la API |
| 2026-11-08 | El alta usa una ruta por paso (`/alta/1` a `/alta/10`) y `/alta/estado` | El botón Atrás del celular funciona y se puede retomar con un enlace |
| 2026-11-08 | Se agregó `motivoEstado` a `PerfilPropio` | El prestador debe ver el motivo de rechazo o corrección del perfil (FR-011) |
| 2026-11-08 | `POST /admin/prestadores/{id}/decision` acepta `RequiereCorreccion` (contrato actualizado) | El contrato no tenía cómo devolver el perfil al prestador tras marcar documentos para corregir (US1, escenario 4) |
| 2026-11-08 | El documento solo se muestra al pulsar "Ver documento", nunca al abrir el perfil | Cada vista queda en la bitácora (FR-016); abrir el perfil no debe generar consultas innecesarias |
| 2026-11-08 | `ubicar` ya es `POST` en el contrato; `GET /prestadores` acepta `tamano`, `oficio` como slug y `tarifaMax` en centavos | Principio I; URLs de búsqueda legibles y compartibles |
| 2026-11-08 | En plantillas y `computed` se lee un `resource` con `hasValue()` o `valorDe()` | `value()` lanza un error si la petición falló y congelaba la pantalla (se vio con un perfil 404) |
| 2026-11-08 | La cercanía suma un bono pequeño (0.15) al promedio bayesiano en el prototipo | FR-019 pide considerar cercanía; el peso definitivo se ajusta en T091 con datos del piloto |
| 2026-11-08 | Se agregó `GET /pases/yo` y `prestadorId` es opcional en `GET /accesos/opciones` (contrato actualizado) | La pantalla del pase necesita su estado y el aviso vigente sin un prestador |
| 2026-11-08 | La app solo redirige a pagar a la ruta interna del checkout simulado o a `https` de `mercadopago.com(.mx)` | Evita que una respuesta alterada mande al usuario a otro sitio |
| 2026-11-08 | El teléfono se pide al pulsar "Ver teléfono" y no se guarda en el navegador | Menos exposición del dato; la API lo entrega solo con acceso activo (FR-020) |
| 2026-11-08 | La oferta del pase descuenta lo pagado en contactos individuales de los últimos 30 días, sin bajar de $0 | FR-027; con 4 contactos pagados el pase saldría gratis, a revisar con el usuario antes del piloto |
| 2026-11-08 | Las piezas del prototipo se registran en `core/prototipo/prototipo.ts`, que producción reemplaza por un archivo vacío (`fileReplacements`) | Un `if` en tiempo de ejecución dejaba los datos ficticios y `/dev/*` en el paquete y el service worker los precargaba |
| 2026-11-08 | Acciones de prueba (simular pago, acreditar, vencer, sesión) detrás del token `ACCIONES_PROTOTIPO` | Las pantallas las usan solo si existen; en producción el token no se provee |
| 2026-11-08 | `security.allowedHosts`: `localhost`, `127.0.0.1`, `serviya.mx` y `*.serviya.mx` | El servidor SSR de Angular rechaza cualquier otro host; sin esto no abría ni en local |
| 2026-11-08 | Playwright local con 4 procesos y `test.slow()` en los flujos largos de pago | Con 6 procesos WebKit agotaba el tiempo de forma intermitente |
| 2026-11-08 | RustFS 1.0.4 como almacenamiento S3 en desarrollo y pruebas, en lugar de MinIO; buckets con `amazon/aws-cli` | MinIO dejó de publicar imágenes (Docker Hub y quay.io); RustFS es compatible con S3 y Apache-2.0. Producción sigue en R2 |
| 2026-11-08 | xUnit v3 sobre Microsoft.Testing.Platform (`api/global.json`) y cobertura con `Microsoft.Testing.Extensions.CodeCoverage` | El SDK de .NET 10 ya no ejecuta VSTest con esta plataforma; `coverlet.collector` solo funciona con VSTest |
| 2026-11-08 | Dos cadenas de conexión: `Default` (rol `serviya_app`) y `Migraciones` (rol `serviya_owner`) | Mínimo privilegio: la app no puede cambiar el esquema ni modificar la bitácora |
| 2026-11-08 | Secretos locales en `.env` y user-secrets generados por `scripts/configurar-desarrollo.ps1`; puertos de Docker solo en `127.0.0.1` | Ninguna contraseña en el repositorio, ni siquiera de desarrollo |
| 2026-11-08 | Opciones como `sealed class` con `init`, no `record` | El `ToString()` de un `record` imprimiría las claves si alguien registra las opciones |
| 2026-11-08 | Versiones centralizadas en `api/Directory.Packages.props` | Una sola versión por paquete en los 15 proyectos |
| 2026-11-08 | `ContactMasker` estático y puro; marcador `[contacto oculto]`; teléfono = 8 o más dígitos (cifras o palabras) unidos por separadores, o 4 o más tras "WhatsApp" | Un teléfono de México tiene 10 dígitos; con 8 ya se adivina. Si una expresión tarda demasiado se oculta todo el texto (falla cerrado) |
| 2026-11-08 | En `ContactMasker` la coma separa dígitos solo si va seguida de espacio, y las fechas (12/11/2026, 12-11-26, 2026-11-12) no cuentan | Evita ocultar precios como `$1,250,000.00` y fechas. Limitación conocida: una lista de años separados por comas y espacios se ocultaría |
| 2026-11-08 | Títulos de `ProblemDetails` en español para los errores estándar (404, 405, 429, 500...) | La app muestra `title` o `detail` al usuario |
| 2026-11-08 | `GET /api/v1/dev/correos` solo en `Development`; el proveedor `Consola` hace fallar el arranque fuera de desarrollo y pruebas | Los códigos solo se leen en local; un error de configuración no deja a producción sin correos |
| 2026-11-08 | Registro en consola simple en desarrollo y JSON en los demás entornos | Legible al programar; JSON para el hospedaje |
| 2026-11-08 | Orígenes de CORS en `Cors:Origenes`: `https://serviya.mx` en producción y `http://localhost:4200` solo en desarrollo; se expone `Retry-After`; además `X-Frame-Options: DENY` | Un origen local no debe tener acceso con credenciales en producción; la app necesita leer `Retry-After` |
| 2026-11-08 | El límite por IP usa `RemoteIpAddress`; al desplegar (T147) se configura `ForwardedHeaders` con las redes del proxy | Detrás del proxy todos compartirían la IP del proxy; confiar en cualquier `X-Forwarded-For` permitiría saltarse el límite |
| 2026-11-08 | Los límites de uso se prueban en un servidor mínimo con un endpoint de prueba | Aún no hay endpoints que los usen; se prueba la misma configuración (`LimitesUso`) |
| 2026-11-08 | `created_at` y `updated_at` como propiedades sombra (`MarcasTiempo.CreadoEn`/`ActualizadoEn`) | Todas las tablas las tienen, incluidas las de Identity, sin repetirlas en cada entidad |
| 2026-11-08 | Identity con el esquema 2 (sin passkeys) y tablas renombradas (`usuarios`, `roles`, `usuario_roles`...) | El MVP no usa passkeys; nombres en español y snake_case como el resto |
| 2026-11-08 | Correo único en la base (índice único en `normalized_email`) | FR-001; Identity solo lo valida en la app |
| 2026-11-08 | Los consentimientos no se borran en cascada (`RESTRICT`) | Son evidencia legal; la cuenta eliminada solo se anonimiza |
| 2026-11-08 | Hash del código = SHA-256 de usuario, propósito y código; se aceptan espacios al pegarlo | Un código no sirve para otro usuario ni propósito; vence en 10 min y tiene 5 intentos |
| 2026-11-08 | Quien nació un 29 de febrero cumple el 1 de marzo en años no bisiestos | Igual que `web/src/app/shared/fechas/edad.ts`; es la regla más estricta para la mayoría de edad |
| 2026-11-08 | `dotnet ef` usa la conexión `Migraciones` (`EF.IsDesignTime`) y la API la `Default` | Las migraciones las aplica el dueño del esquema; la app no puede cambiarlo |
| 2026-11-08 | Las migraciones se marcan como código generado en `api/.editorconfig` | Los analizadores con advertencias como errores rechazaban el código de `dotnet ef` |

## Pendientes abiertos

- Nombre definitivo de la plataforma ("ServiYa" es provisional).
- Ciudad o zona del piloto.
- Oficios incluidos en el piloto.
- Revisión de textos legales con un abogado y del régimen fiscal con un contador.
- Oferta del pase cuando lo pagado en contactos supera $129 (hoy queda en $0): ¿tope mínimo?
- Cómo generar el PDF del comprobante de pago (research R14, tarea T108; opción a evaluar: QuestPDF Community).

## Bitácora

| Fecha | Cambio | Commit |
|---|---|---|
| 2026-11-05 | Carpeta `D:\Git\serviya`, git y Spec Kit (copiado de LearnBook porque `specify.exe` está bloqueado) | commit inicial |
| 2026-11-05 | Constitución v1.0.0 | commit inicial |
| 2026-11-05 | Especificación `specs/001-serviya-mvp` y su lista de calidad | commit inicial |
| 2026-11-05 | Piezas de ECC copiadas y adaptadas en `.claude/` | commit inicial |
| 2026-11-05 | `CLAUDE.md` y esta memoria del proyecto | commit inicial |
| 2026-11-05 | Plan técnico: `plan.md`, `research.md`, `data-model.md`, `contracts/` y `quickstart.md`; constitución v1.0.1 | ver `git log` |
| 2026-11-05 | Repositorio subido a GitHub (`jan039795-arch/serviya`, privado) por el usuario | cde93cb |
| 2026-11-08 | `tasks.md` generado con `/speckit-tasks`: 151 tareas en 13 fases | ver `git log` |
| 2026-11-08 | Fase 1: proyecto `web/` (T001–T009); lint, pruebas unitarias, compilación y E2E en celular pasan | ver `git log` |
| 2026-11-08 | Base del prototipo (T010–T018): modelos, datos ficticios, simulación, errores, sesión, armazón, UI y subida | ver `git log` |
| 2026-11-08 | Pantallas de cuenta y alta del prestador del prototipo (T019–T024) | ver `git log` |
| 2026-11-08 | Revisión del administrador en el prototipo (T025) y contrato de decisión del perfil | ver `git log` |
| 2026-11-08 | Búsqueda y perfil público del prototipo (T026–T029) | ver `git log` |
| 2026-11-08 | Contacto pagado, checkout simulado, mis contactos y pase en el prototipo (T030–T033) | ver `git log` |
| 2026-11-08 | Cierre del prototipo (T034–T036), revisión con `typescript-reviewer` y correcciones (T037, falta la prueba en celular) | ver `git log` |
| 2026-11-08 | Preparación del backend (T038, T040–T043; T039 escrito sin probar): solución .NET, opciones validadas, docker-compose con PostGIS y RustFS, script de secretos y CI | ver `git log` |
| 2026-11-08 | Fase 4 sin base de datos (T044, T047, T048, T051, T053–T055): host de la API, `ContactMasker` con TDD, correo, límites de uso, encabezados y registro sin datos personales | ver `git log` |
| 2026-11-08 | Persistencia y Cuentas sin base de datos (T045, T056, T057, T060): convenciones, marcas de tiempo, dominio, códigos con TDD, `CuentasDbContext` y migración `Inicial` | ver `git log` |
