# Ajustes de ServiYa sobre las reglas de ECC

Las reglas de `common/`, `csharp/`, `typescript/`, `angular/` y `web/` vienen de ECC (ver
`.claude/THIRD_PARTY_NOTICES.md`). Si alguna choca con lo siguiente o con
`.specify/memory/constitution.md`, mandan este archivo y la constitución.

## Flujo de trabajo

- El flujo es Spec Kit: especificación → plan → tareas → implementación. No se usan los comandos de
  planeación de ECC (`/plan`, `orch-*`, `prp-*`, `feature-dev`), que no están instalados.
- Los agentes revisores (`csharp-reviewer`, `typescript-reviewer`, `security-reviewer`,
  `database-reviewer`) se ejecutan al cerrar cada etapa o cuando el usuario lo pide, no en cada
  cambio.

## Backend (.NET)

- La inmutabilidad aplica a DTOs, comandos, eventos y opciones (`record`, `init`). Las entidades de
  EF Core sí se modifican a través del `DbContext`, que es como EF Core rastrea los cambios.
- Errores de la API como `ProblemDetails` (RFC 9457); sin sobre `ApiResponse<T>`.
- Sin `IRepository<T>` genérico sobre EF Core.
- Aserciones en pruebas: AwesomeAssertions (no FluentAssertions 8+, que es comercial).
- Pruebas de integración con `WebApplicationFactory` y Testcontainers (PostgreSQL real).
- `postgres-patterns` y `database-reviewer` mencionan RLS de Supabase: no aplica, porque la
  autorización se hace en la API. El resto (índices, tipos, consultas) sí aplica.
- `database-migrations` no cubre EF Core: se usan migraciones de EF Core siguiendo sus principios
  (cambios hacia adelante, expand-contract, índices concurrentes para tablas grandes).

## Frontend (Angular)

- Angular 20+: componentes standalone por defecto, archivos sin sufijo `.component`, `inject()`,
  signals, `resource({ params, loader })` y control de flujo `@if`/`@for`.
- Las secciones de React (props, hooks, `React.FC`) en `typescript/` y `frontend-a11y` no aplican;
  sus principios de HTML semántico y ARIA sí.
- No se agrega Zod por defecto: validar respuestas en el límite con tipos e interfaces y funciones de
  verificación; agregar una librería solo si el plan lo justifica.
- Pruebas E2E con Playwright (no Cypress); selectores estables con `data-testid`.
- Diseño: claridad y accesibilidad antes que estilo llamativo; lenguaje simple en español de México.

## Commits

- Mensajes en formato convencional (`feat:`, `fix:`, `docs:`...), con la atribución que indique el
  entorno de Claude Code.
