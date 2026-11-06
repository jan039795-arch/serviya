# Progreso de ServiYa

> Memoria del proyecto. Léela al iniciar cada sesión y actualízala con cada cambio (ver `CLAUDE.md`).

## Estado actual

**Etapa 1 — Definición** (en curso).

- Constitución v1.0.0 aprobada como base.
- Especificación del MVP escrita y validada: `specs/001-serviya-mvp/spec.md` (8 historias de usuario,
  FR-001 a FR-046, SC-001 a SC-010). **El usuario la está revisando.**
- Skills, reglas y agentes de ECC copiados y adaptados en `.claude/`.
- Todavía no hay código.

## Siguiente paso exacto

1. Aplicar los cambios que pida el usuario tras revisar `specs/001-serviya-mvp/spec.md`.
2. Opcional: `/speckit-clarify` para resolver dudas restantes.
3. `/speckit-plan` con Angular 20+ + ASP.NET Core 10 + PostgreSQL → genera `plan.md`, `research.md`,
   `data-model.md`, `contracts/` y `quickstart.md`.
4. `/speckit-tasks`, y después la Etapa 2: prototipo móvil con datos ficticios.

## Etapas del proyecto

| # | Etapa | Estado |
|---|---|---|
| 1 | Definición (constitución, especificación, plan) | En curso |
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
