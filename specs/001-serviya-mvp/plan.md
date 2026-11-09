# Implementation Plan: ServiYa MVP — Plataforma de contacto con prestadores de oficios verificados

**Branch**: `001-serviya-mvp` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-serviya-mvp/spec.md`

## Summary

PWA instalable (Android/iPhone) donde los prestadores de oficios se dan de alta con documentos que
revisa un administrador, y los clientes pagan $39 MXN por contacto o $129 MXN por pase mensual para
chatear con el prestador y ver su teléfono. Enfoque técnico: monolito modular en **ASP.NET Core 10**
(EF Core + PostgreSQL/PostGIS, Identity con cookie, SignalR) y frontend en **Angular 22** (signals,
PWA, SSR para perfiles públicos). Los archivos van a Cloudflare R2 privado, los pagos se hacen con
Checkout Pro y Suscripciones de Mercado Pago, y el webhook firmado es la única vía para activar un
acceso. Primero se entrega un prototipo de Angular con datos ficticios. El detalle de las decisiones
está en [research.md](research.md).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (LTS); TypeScript 5.x / Angular 22

**Primary Dependencies**: ASP.NET Core 10 (Minimal APIs, Identity, SignalR, OpenAPI, antiforgery,
rate limiting), EF Core 10 + Npgsql 10.0.3 + NetTopologySuite, `mercadopago-sdk` 3.7.1, `AWSSDK.S3`
4.0 (R2), `WebPush` 1.0.13; Angular 22 + `@angular/pwa` + `@angular/ssr`; `openapi-typescript`
(desarrollo)

**Storage**: PostgreSQL 17 + PostGIS (Neon en producción, Docker en desarrollo); Cloudflare R2
(`serviya-privado`, `serviya-publico`; RustFS en desarrollo)

**Testing**: xUnit + AwesomeAssertions + NSubstitute; WebApplicationFactory + Testcontainers
(`postgis/postgis:17`); Vitest (Angular CLI); Playwright 1.65 con perfiles de celular

**Target Platform**: Chrome en Android y Safari en iOS recientes (PWA; push en iOS 16.4+ con la app
instalada); API en un contenedor Linux (Render)

**Project Type**: aplicación web (API + frontend con SSR y PWA)

**Performance Goals**: LCP < 2.5 s en búsqueda y perfil con 4G de gama media; mensajes de chat
entregados en < 1 s; acceso activado en < 1 min tras confirmarse un pago con tarjeta (SC-005)

**Constraints**: datos sensibles nunca públicos (principio I); JS inicial < 300 KB gzip en páginas de
la app y < 150 KB en el perfil público; funciona desde 360 px de ancho; hosting del piloto
≤ 15 USD/mes; un solo desarrollador

**Scale/Scope**: piloto en una ciudad: ~100–500 prestadores, ~2 000 clientes, ~100 pagos por semana;
~35 pantallas; 10 módulos de backend

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Cumplimiento en el diseño | Estado |
|---|---|---|
| I. Privacidad | Bucket privado y URLs firmadas de 5 min con bitácora por emisión (R3, FR-016); DTOs públicos dedicados con prueba de contrato ([contracts/api.md](contracts/api.md)); dirección y punto base privados; claves de objeto GUID; consentimiento de datos sensibles antes de subir documentos; al eliminar la cuenta se borran sus objetos | ✅ |
| II. Solo contacto | Solo existen los conceptos de pago `ContactoIndividual`, `PaseMensual`, `RenovacionPase` y `CuotaAdministracion`; el precio acordado de una contratación es solo informativo; aviso versionado mostrado antes de pagar; renovación opcional con aviso 3 días antes y cancelación en un clic; `ContactMasker` en el chat y en los textos públicos | ✅ |
| III. Confianza verificable | Publicación solo tras aprobación humana con motivo y autor; calificación solo con contratación `Terminada`, doble ciega por 14 días, bidireccional y en 4 ejes; carta de antecedentes opcional; suspensión con aclaración; mayoría de edad validada en el servidor | ✅ |
| IV. Mobile-first | PWA de Angular, diseño desde 360 px, guardado por paso (`PasoAlta`), compresión de imágenes en el navegador, subidas reintentables con URLs firmadas, Playwright con perfiles de celular | ✅ |
| V. Simplicidad | Un solo backend (monolito modular, un `DbContext` por módulo) y un frontend; sin colas ni Hangfire (`BackgroundService` con advisory lock); sin repositorio genérico; cada dependencia nueva está justificada en research | ✅ |
| VI. Pruebas en lo crítico | TDD y 80% de cobertura en Pagos y webhook, permisos, Documentos, desbloqueo de contacto y `ContactMasker`; Mercado Pago en modo de pruebas; prototipo con datos ficticios en la Etapa 2; solo documentos de prueba ficticios | ✅ |

**Reevaluación tras la Fase 1**: sin violaciones. La constitución nombraba "Angular 20"; se enmienda a
v1.0.1 para decir "Angular estable vigente (22)". Es una aclaración sin cambio de principio.

## Project Structure

### Documentation (this feature)

```text
specs/001-serviya-mvp/
├── plan.md              # Este archivo
├── research.md          # Fase 0: decisiones técnicas
├── data-model.md        # Fase 1: entidades, estados e índices
├── quickstart.md        # Fase 1: guía de validación
├── contracts/
│   ├── api.md           # Contrato REST v1
│   └── webhooks.md      # Webhook de Mercado Pago, hub SignalR y Web Push
├── checklists/
│   └── requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
api/
├── ServiYa.sln
├── src/
│   ├── ServiYa.Api/                  # Host: Program.cs, composición, middleware, OpenAPI y hubs
│   ├── ServiYa.Comun/                # ContactMasker, TimeProvider, almacenamiento R2, correo y bitácora
│   └── Modulos/
│       ├── ServiYa.Cuentas/          # Identity, códigos, consentimientos y bloqueos
│       ├── ServiYa.Prestadores/      # Perfil, oficios, cobertura, portafolio y ranking
│       ├── ServiYa.Documentos/       # Documentos privados y decisiones de revisión
│       ├── ServiYa.Busqueda/         # Catálogos y consultas PostGIS
│       ├── ServiYa.Pagos/            # Accesos, pases, pagos, créditos, Mercado Pago y webhook
│       ├── ServiYa.Chat/             # Conversaciones, mensajes y push
│       ├── ServiYa.Contrataciones/
│       ├── ServiYa.Calificaciones/
│       ├── ServiYa.Reportes/         # Reportes y suspensiones
│       └── ServiYa.Administracion/   # Endpoints de administración y alertas
│   # Cada módulo: Dominio/, Datos/ (DbContext, configuraciones y migraciones), Endpoints/, Servicios/, Tareas/
└── tests/
    ├── ServiYa.UnitTests/            # ContactMasker, reglas de estado, descuentos y ranking
    ├── ServiYa.IntegrationTests/     # WebApplicationFactory + Testcontainers por módulo
    └── ServiYa.ContractTests/        # DTOs públicos sin campos privados y webhook firmado

web/
├── src/
│   ├── app/
│   │   ├── core/                     # Sesión, interceptores (XSRF, errores, mocks) y guards por rol
│   │   ├── features/
│   │   │   ├── busqueda/             # Inicio, resultados y perfil público (SSR)
│   │   │   ├── cuenta/               # Registro, verificación, login, mi cuenta y eliminar cuenta
│   │   │   ├── alta-prestador/       # 10 pasos con guardado
│   │   │   ├── contacto/             # Opciones de pago, resultado del pago y pase
│   │   │   ├── chat/                 # Conversaciones y contratación
│   │   │   ├── calificaciones/
│   │   │   └── admin/                # Cola de revisión, visor de documentos, reportes y bitácora
│   │   └── shared/                   # Componentes de UI, pipes (moneda MXN) y subida de archivos
│   ├── mocks/                        # JSON ficticios para el prototipo (Etapa 2)
│   └── environments/
├── e2e/                              # Playwright
└── ngsw-config.json

docker-compose.yml                    # PostgreSQL con PostGIS y RustFS (S3) para desarrollo
```

**Structure Decision**: aplicación web con dos proyectos desplegables (`api/` y `web/`). El backend es
un monolito modular: cada módulo es un proyecto de clases con su propio esquema y `DbContext`, y solo
`ServiYa.Api` los compone. Los módulos se comunican por interfaces públicas (`IAccesosService`,
`IBitacora`, etc.), nunca a través de las tablas de otro módulo. Así se cumple el principio V y queda
abierta una separación futura sin costo actual.

## Etapas de implementación (insumo para /speckit-tasks)

| Etapa | Historias | Entregable verificable |
|---|---|---|
| 2. Prototipo | US1–US4 (pantallas) | `web/` con datos ficticios, navegable en celular e instalable |
| 3. Cuentas y perfiles | US1 (parte), FR-001–FR-005 | Registro, verificación por correo, roles y alta con guardado |
| 4. Documentos y administración | US1, FR-006–FR-017 | Subida a R2, cola de revisión, decisiones y bitácora |
| 5. Búsqueda | US2 | Catálogos de INEGI, búsqueda con PostGIS y perfil público con SSR |
| 6. Pagos | US3, US4 | Checkout Pro, webhook firmado, accesos, pases, créditos y suscripción |
| 7. Chat y contratación | US5 | SignalR, ContactMasker, push y contrataciones |
| 8. Calificaciones y reportes | US6, US7 | Doble ciega, ranking, reportes y suspensiones |
| 9. Legal y cierre | US8 | Consentimientos, descarga y eliminación de datos, textos legales, CSP y despliegue |

## Complexity Tracking

Sin violaciones de la constitución que justificar.
