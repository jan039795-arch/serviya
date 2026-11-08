# Progreso de ServiYa

> Memoria del proyecto. Léela al iniciar cada sesión y actualízala con cada cambio (ver `CLAUDE.md`).

## Estado actual

**Etapa 1 — Definición** (terminada). Lo siguiente es la Etapa 2 (prototipo).

- Constitución v1.0.1 (Angular estable vigente, hoy 22).
- Especificación del MVP aprobada por el usuario: `specs/001-serviya-mvp/spec.md` (8 historias,
  FR-001 a FR-046, SC-001 a SC-010).
- Plan técnico listo en `specs/001-serviya-mvp/`: `plan.md`, `research.md` (R1–R13),
  `data-model.md`, `contracts/api.md`, `contracts/webhooks.md` y `quickstart.md`.
- Tareas listas: `specs/001-serviya-mvp/tasks.md` con 242 tareas (T001–T242) en 12 fases. Fase 1
  Setup (T001–T017), Fase 2 Prototipo (T018–T056), Fase 3 Fundamentos del backend (T057–T095), luego
  una fase por historia (US1–US8) y el cierre.
- Skills, reglas y agentes de ECC copiados y adaptados en `.claude/`.
- Todavía no hay código. **Docker no está instalado** (lo necesitan PostgreSQL local y Testcontainers).
- Repositorio respaldado en GitHub (privado): `https://github.com/jan039795-arch/serviya`, rama `main`.

## Siguiente paso exacto

1. (Opcional) El usuario revisa `tasks.md`, en especial las decisiones del 2026-11-08 de la tabla de
   abajo.
2. `/speckit-implement` empezando por la Fase 1 en este orden: T001 → T003 (proyecto Angular 22 en
   `web/`) → T004–T010 (PWA, ESLint, idioma, entornos, proxy, Vitest y Playwright). T011–T017 (`api/`,
   `docker-compose.yml` y CI) pueden ir después; no bloquean el prototipo.
3. Fase 2 (T018–T056): prototipo con datos ficticios. Solo necesita Node; no requiere Docker. Termina
   cuando el usuario lo valida en su celular (T056).
4. Antes de la Fase 3: el usuario instala Docker Desktop con WSL 2 (o se apoya en la CI de GitHub para
   las pruebas de integración) y crea las cuentas de desarrollador de Mercado Pago y Cloudflare.

## Etapas del proyecto

| # | Etapa | Estado |
|---|---|---|
| 1 | Definición (constitución, especificación, plan, tareas) | Terminada |
| 2 | Prototipo móvil con datos ficticios | Pendiente |
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
| 2026-11-08 | Alta del prestador en 10 pasos: datos personales, oficios, tarifas, cobertura, disponibilidad, descripción, consentimiento, documentos, video y portafolio, revisar y enviar | El plan decía "10 pasos" sin enumerarlos; el consentimiento va justo antes de los documentos (US8) |
| 2026-11-08 | Interfaces y DTOs entre módulos en `ServiYa.Comun/Contratos/<Modulo>/` | Evita referencias circulares entre proyectos de módulos |
| 2026-11-08 | La consulta de búsqueda vive en `ServiYa.Prestadores`; `ServiYa.Busqueda` solo tiene catálogos y la ubicación | Un módulo no consulta tablas de otro |
| 2026-11-08 | Entidad `ArchivoSubido` (esquema `archivos`) y `POST /archivos/url-subida` para fotos de chat y reseñas | Verificar que un `archivoId` sea del usuario; el contrato no tenía cómo subir fotos fuera del alta |
| 2026-11-08 | `ubicar` pasa a `POST` con `{ lat, lng }` en el cuerpo | Constitución I: datos personales nunca en URLs ni logs |
| 2026-11-08 | Comprobante en JSON y página imprimible en la web, no PDF en la API | Principio V: sin librería de PDF |
| 2026-11-08 | `POST /cuentas/yo/consentimientos` y `aceptacionesPendientes` en la sesión | Registrar la aceptación cuando cambia la versión de un texto legal (FR-044) |
| 2026-11-08 | `PuntoBase` = ubicación del navegador redondeada a 2 decimales (≈1 km) o centroide del municipio | No hay catálogo de colonias ni geocodificación de pago |
| 2026-11-08 | Con pase activo, un acceso individual que vence se extiende hasta el fin del pase sin consumir contactos | US3 escenario 6 ("salvo que tenga un pase activo") |
| 2026-11-08 | Al suspender o eliminar a un prestador, sus accesos activos pasan a `Vencido` y cada cliente recibe un crédito | FR-043 |
| 2026-11-08 | Nombres de tablas y columnas en snake_case con `EFCore.NamingConventions` | Los índices de data-model.md ya usan snake_case |
| 2026-11-08 | XSRF con interceptor propio en Angular y proxy de desarrollo | El soporte nativo de Angular no envía el encabezado a otro origen (`api.serviya.mx`) |

## Pendientes abiertos

- Nombre definitivo de la plataforma ("ServiYa" es provisional).
- Ciudad o zona del piloto.
- Oficios incluidos en el piloto.
- Revisión de textos legales con un abogado y del régimen fiscal con un contador.

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
| 2026-11-08 | `tasks.md` con 242 tareas en 12 fases; ajustes en `contracts/api.md` y `data-model.md` (ver decisiones del 2026-11-08) | ver `git log` |
