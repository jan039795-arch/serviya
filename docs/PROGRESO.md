# Progreso de ServiYa

> Memoria del proyecto. Léela al iniciar cada sesión y actualízala con cada cambio (ver `CLAUDE.md`).

## Estado actual

**Etapa 2 — Prototipo** (en curso). Terminadas la Fase 1 (T001–T009), la base del prototipo (T010–T018) todas las pantallas de US1 (T019–T025) y las de US2 (T026–T029).

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
- 65 pruebas unitarias y 44 E2E (Pixel 7 e iPhone 14) pasan.
- Sin backend todavía. **Docker no está instalado** (lo necesitan PostgreSQL local y Testcontainers).
- Repositorio respaldado en GitHub (privado): `https://github.com/jan039795-arch/serviya`, rama `main`.

## Siguiente paso exacto

1. US3 y US4 en el prototipo (T030–T033): `web/src/app/features/contacto/` (`opciones.ts`,
   `checkout-simulado.ts`, `resultado-pago.ts`, `mis-accesos.ts`, `pase.ts`) y el manejador
   `web/src/app/core/mocks/handlers/accesos.ts`. Reemplazar la ruta `Pendiente` de
   `contactar/:prestadorId` en `web/src/app/app.routes.ts` (el botón "Contactar" del perfil ya
   apunta ahí). El teléfono sale de `PrestadorDirectorio.telefono` solo con acceso `Activo`.
2. Después: cierre del prototipo (T034–T037).
3. El prototipo termina cuando el usuario lo aprueba en su celular (T037).
4. Antes de la Fase 3: el usuario instala Docker Desktop con WSL 2. Antes de la Fase 7 (pagos): crea
   la cuenta de desarrollador de Mercado Pago.

## Etapas del proyecto

| # | Etapa | Estado |
|---|---|---|
| 1 | Definición (constitución, especificación, plan, tareas) | Terminada |
| 2 | Prototipo móvil con datos ficticios | En curso (base lista; faltan pantallas) |
| 3 | Cuentas y perfiles | Pendiente |
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

## Pendientes abiertos

- Nombre definitivo de la plataforma ("ServiYa" es provisional).
- Ciudad o zona del piloto.
- Oficios incluidos en el piloto.
- Revisión de textos legales con un abogado y del régimen fiscal con un contador.
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
