# Investigación técnica: ServiYa MVP

**Fecha**: 2026-11-05 · **Plan**: [plan.md](plan.md) · **Spec**: [spec.md](spec.md)

Versiones verificadas en npm y NuGet el 2026-11-05: Angular 22.2 (`@angular/core` 22.2.4,
`@angular/pwa` 22.2.6), .NET SDK 10.0.402, Npgsql EF Core 10.0.3 (con NetTopologySuite),
`mercadopago-sdk` 3.7.1, AwesomeAssertions 9.6.0, Testcontainers.PostgreSql 4.16.0, AWSSDK.S3 4.0.104,
WebPush 1.0.13, Playwright 1.65.

---

## R1. Versión de Angular

- **Decisión**: Angular 22 (estable vigente), con componentes standalone, signals, `resource`, Signal
  Forms para formularios nuevos, control de flujo `@if`/`@for`, `@angular/pwa` y `@angular/ssr`.
- **Razón**: la constitución decía "Angular 20" cuando la versión estable ya es la 22. Empezar en la
  última estable evita una migración inmediata. Se enmienda la constitución a 1.0.1 (aclaración).
- **Alternativas**: Angular 20 LTS (soporte más corto restante; sin ventaja real para un proyecto nuevo).

## R2. Autenticación

- **Decisión**: ASP.NET Core Identity con **cookie HttpOnly** (`Secure`, `SameSite=Lax`), sobre el mismo
  dominio padre (`serviya.mx` y `api.serviya.mx`). Protección CSRF con antiforgery de ASP.NET Core
  configurado para el par cookie `XSRF-TOKEN` / encabezado `X-XSRF-TOKEN`, que Angular `HttpClient`
  envía automáticamente. Código de un solo uso por correo para verificar la cuenta y recuperar la
  contraseña (6 dígitos, 10 minutos, 5 intentos). Roles: `Cliente`, `Prestador`, `Admin`.
- **Razón**: la cookie HttpOnly no queda expuesta a JavaScript (a diferencia de un JWT en
  `localStorage`), funciona con SignalR sin pasar tokens por la URL y con SSR reenviando la cookie.
- **Alternativas**: JWT con refresh tokens (más código y riesgo de exposición); proveedor externo como
  Auth0 o Clerk (costo y datos fuera de nuestro control).
- **Notas**: el SMS queda fuera del MVP (FR-001 se cumple con correo; teléfono como dato de contacto).
  Fecha de nacimiento obligatoria y validación de 18 años o más en el servidor (FR-004).

## R3. Almacenamiento de archivos

- **Decisión**: Cloudflare R2 mediante `AWSSDK.S3`, con dos buckets:
  - `serviya-privado`: documentos (INE, selfie, domicilio, antecedentes) y portafolio pendiente de
    moderar. Lectura solo con **URL firmada de 5 minutos** emitida por la API a un administrador, y
    cada emisión se registra en la bitácora (FR-016).
  - `serviya-publico`: portafolio aprobado, servido por el dominio público de R2 con caché.
- Carga directa desde el navegador con **URL firmada de subida (PUT, 10 min)**. La API valida
  después: tamaño, tipo declarado y firma del archivo (primeros bytes). Las claves de objeto son GUID
  y nunca contienen datos personales.
- Al aprobar un elemento del portafolio, la API lo copia de `privado` a `publico`. Al eliminar una
  cuenta, se borran sus objetos (FR-045).
- **Razón**: R2 no cobra por descarga y tiene 10 GB gratis. Con la subida directa, la API no
  transporta videos pesados.
- **Alternativas**: Supabase Storage (otro proveedor más); Azure Blob (cobra la salida de datos);
  disco del servidor (se pierde al redesplegar).

## R4. Imágenes y video

- **Decisión**: las fotos se comprimen en el navegador con `<canvas>` nativo (máximo 1600 px, JPEG o
  WebP calidad 0.8) antes de subirlas. El video se graba con el celular, se valida en el navegador
  (90 s como máximo, leído de los metadatos) y se limita a 60 MB. **Sin transcodificación en el MVP**:
  se reproduce con `<video>` y peticiones por rangos desde R2.
- **Razón**: sin dependencias nuevas y con costo cero. Los videos del celular en H.264/HEVC se
  reproducen en Android e iOS recientes.
- **Alternativas**: Cloudflare Stream o Mux (≈5 USD/mes; se adoptan si el tamaño de los videos
  resulta un problema en el piloto); `ffmpeg.wasm` (pesado para celulares de gama baja).

## R5. Ubicación y búsqueda

- **Decisión**: PostgreSQL con **PostGIS** (Npgsql + NetTopologySuite). Se carga un catálogo de
  **municipios o alcaldías** de la zona del piloto con sus polígonos (Marco Geoestadístico de INEGI,
  datos públicos). El prestador elige los municipios que cubre y su colonia base (punto aproximado).
  El cliente elige un municipio o comparte su ubicación desde el navegador; la API la convierte a
  municipio con "punto dentro de polígono". El perfil público muestra solo colonia o municipio.
- **Orden de resultados**: promedio bayesiano `(C·m + Σ calificaciones) / (C + n)` con `C = 5` y
  `m` = promedio global, más la cercanía (distancia al punto base) y las insignias. Esto cumple
  FR-019 y el escenario 5 de US6.
- **Razón**: evita pagar un servicio de geocodificación (Google o Mapbox) y funciona bien para una
  sola ciudad. Neon y la imagen `postgis/postgis` incluyen PostGIS.
- **Alternativas**: radio en km desde un punto (más difícil de explicar al prestador); geocodificar
  direcciones (costo y datos personales enviados a terceros).

## R6. Pagos con Mercado Pago

- **Decisión**:
  - **Contacto individual y pase de pago único**: **Checkout Pro** (preferencia con
    `external_reference` = Id del pago interno y `notification_url` hacia la API). En México incluye
    tarjeta de crédito y débito, transferencia SPEI, OXXO, Paycash y saldo de Mercado Pago.
  - **Pase con renovación automática**: Suscripciones de Mercado Pago (`preapproval`) con tarjeta,
    129 MXN cada mes. El aviso 3 días antes y la cancelación en un clic los maneja la API
    (`PUT preapproval {status: cancelled}`).
  - **Descuento al pasar al pase (FR-027)**: se cobra como **pase de pago único** por
    `129 − lo pagado en los últimos 30 días`. Si el cliente quiere renovación automática, se crea la
    suscripción con `start_date` = fin del periodo ya pagado. Así no hace falta que Mercado Pago
    descuente el primer cobro.
  - **Webhook**: se valida `x-signature` (`ts` y `v1`, HMAC-SHA256 sobre
    `id:{data.id};request-id:{x-request-id};ts:{ts};` con la clave secreta), con el validador del SDK
    para C#. Sin firma válida se responde 401. Luego se **consulta el pago a la API de Mercado Pago**:
    nunca se confía en el cuerpo de la notificación. El procesamiento es idempotente (índice único por
    Id del pago de Mercado Pago) y se responde 200 rápido.
  - **Estados**: `approved` → acceso Activo; `pending` / `in_process` (OXXO, SPEI) → Pendiente;
    `rejected` / `cancelled` / referencia vencida → Expirado; `refunded` → Reembolsado.
  - **Reembolsos**: `POST /v1/payments/{id}/refunds`, con encabezado `X-Idempotency-Key`.
  - **Pruebas**: credenciales y usuarios de prueba de Mercado Pago; en las pruebas automatizadas, un
    `HttpMessageHandler` falso con respuestas grabadas.
- **Razón**: con Checkout Pro, la tarjeta nunca toca nuestros servidores (PCI fuera de alcance) y
  todos los medios se cubren con una sola integración.
- **Hallazgo importante**: los pagos en efectivo pueden tardar entre 48 y 72 horas hábiles en
  acreditarse, según el punto de pago, y la acreditación de SPEI depende del banco de origen. La
  pantalla de pago debe advertirlo y recomendar tarjeta para un contacto inmediato. SC-005 se mide
  desde la confirmación de Mercado Pago, no desde el depósito.
- **Alternativas**: Checkout API o Bricks (más control, más código y más alcance PCI); Stripe México
  (OXXO sí, pero SPEI y suscripciones limitados en MXN).

## R7. Tareas programadas

- **Decisión**: `BackgroundService` con `PeriodicTimer` cada 5 minutos, que ejecuta tareas
  idempotentes guiadas por la base de datos: crédito por falta de respuesta en 48 h (FR-026),
  vencimiento de accesos a los 30 días, referencias OXXO vencidas, aviso de renovación 3 días antes
  (FR-028), publicación de calificaciones a los 14 días (FR-037) y aviso de antecedentes por vencer
  (FR-013). Un candado de PostgreSQL (`pg_try_advisory_lock`) evita ejecuciones dobles.
- **Razón**: sin Hangfire ni Quartz (principio V); con una sola instancia es suficiente.
- **Implicación de costo**: el plan gratuito de Render "duerme" el servicio; producción necesita una
  instancia siempre encendida (≈7 USD/mes).

## R8. Chat en tiempo real y ocultamiento de contacto

- **Decisión**: SignalR con un hub `/hubs/chat` autenticado por la cookie. Cada conversación es un
  grupo. Los mensajes se guardan primero en PostgreSQL y luego se difunden. Las fotos siguen el flujo
  de R3. Las notificaciones push usan **Web Push** con VAPID (`SwPush` de Angular más el paquete
  `WebPush`), en iOS 16.4+ solo si la app está instalada en la pantalla de inicio. También se avisa
  por correo si el usuario no tiene la app abierta.
- **Ocultamiento (FR-033)**: un servicio puro `ContactMasker` aplica, del lado del servidor, patrones
  para teléfonos (8 o más dígitos con separadores, dígitos escritos con palabras en español),
  correos (incluido "arroba" y "punto com"), enlaces `wa.me`, `t.me`, Instagram, Facebook y Telegram,
  y la palabra "WhatsApp" seguida de números. Se aplica a mensajes sin acceso activo **y** a todos los
  textos públicos del perfil (descripción, títulos de portafolio, respuestas a reseñas). Se prueba con
  un catálogo de intentos de evasión (principio VI).
- **Alternativas**: un servicio de chat externo como Stream o Sendbird (costo y datos fuera); filtrar
  con un modelo de lenguaje (costo y latencia; posible mejora futura).

## R9. Correo electrónico

- **Decisión**: un proveedor transaccional con plan gratuito (Resend o Brevo) detrás de la interfaz
  `IEmailSender`. En desarrollo, los correos se escriben en la consola o en un buzón local.
- **Razón**: códigos de verificación, avisos de estado, recordatorios de renovación y comprobantes.

## R10. Prototipo con datos ficticios (Etapa 2)

- **Decisión**: el prototipo es solo la app Angular, con un `HttpInterceptor` de simulación
  (`MockApiInterceptor`) que responde según los [contratos](contracts/api.md) con datos de
  `src/mocks/*.json`. Se activa con `environment.useMocks`. Al conectar la API real se desactiva sin
  tocar los componentes.
- **Tipos compartidos**: la API publica OpenAPI con `Microsoft.AspNetCore.OpenApi` (incluido en .NET
  10) y el frontend genera sus tipos con `openapi-typescript` (solo como dependencia de desarrollo;
  justificada porque evita que los contratos se desincronicen).

## R11. Pruebas

- **Backend**: xUnit + AwesomeAssertions + NSubstitute; integración con `WebApplicationFactory` y
  **Testcontainers** (`postgis/postgis:17`).
  - ⚠️ **Docker no está instalado en el equipo.** Es requisito para Testcontainers: instalar Docker
    Desktop (gratuito para uso personal) con WSL 2.
- **Frontend**: el ejecutor de pruebas que configura Angular CLI 22 (Vitest) para servicios, guards y
  componentes.
- **E2E**: Playwright 1.65 con perfiles de celular (Pixel 7, iPhone 14) para los flujos críticos de
  alta, pago y chat.
- **Críticos con 80% de cobertura y TDD**: Pagos (incluido el webhook), permisos por rol, desbloqueo
  de contacto, Documentos (emisión de URLs firmadas) y `ContactMasker`.

## R12. Despliegue y costos

- **Decisión**:
  - API: contenedor Docker en **Render**, plan inicial de ≈7 USD/mes (siempre encendido, por R7).
  - Web (Angular SSR en Node): segundo servicio en Render (≈7 USD/mes), o el plan gratuito durante el
    piloto cerrado.
  - PostgreSQL: **Neon**, plan gratuito con PostGIS y respaldos automáticos (point-in-time restore).
  - Archivos: Cloudflare R2, 10 GB gratis.
  - Dominio `.mx` o `.com` con DNS en Cloudflare.
- **Total estimado del piloto**: 0–15 USD/mes más el dominio.
- **Alternativas**: Railway (sin plan gratuito permanente); Azure App Service (más caro para empezar);
  Cloudflare Pages para la web (las rutas SSR requieren adaptar Angular a Workers; se descarta por
  simplicidad).

## R13. Facturación de cuotas

- **Decisión**: en el MVP, el cliente solicita la factura desde su historial de pagos (RFC, razón
  social, régimen, código postal, uso del CFDI). Se guarda la solicitud y el administrador emite el
  CFDI fuera del sistema (portal del SAT o servicio de facturación). FR-031 se cumple con comprobante
  más solicitud.
- **Mejora futura**: integrar un PAC (Facturapi u otro) cuando el volumen lo justifique.
