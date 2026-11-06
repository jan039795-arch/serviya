# Avisos de terceros

## ECC (Everything Claude Code) v2.2.3

- Origen: https://github.com/affaan-m/ECC (copia local del fork `jan039795-arch/ECC`, commit `b38be6a7`)
- Licencia: MIT, Copyright (c) 2026 Affaan Mustafa. Texto completo en `ECC-LICENSE.txt`.
- Copiado el 2026-11-05.

### Piezas incluidas

- Skills: `angular-developer`, `dotnet-patterns`, `csharp-testing`, `security-review`, `api-design`,
  `postgres-patterns`, `database-migrations`, `e2e-testing`, `accessibility`, `frontend-a11y`,
  `deployment-patterns`, `seo`.
- Reglas: `common/` (coding-style, code-review, security, testing), `csharp/` (coding-style,
  patterns, security, testing), `typescript/` (coding-style, security, testing), `angular/`
  (coding-style, patterns, security, testing), `web/` (performance, security, testing).
- Agentes: `csharp-reviewer`, `typescript-reviewer`, `security-reviewer`, `database-reviewer`.

### Cambios respecto al original

- `rules/angular/*`: rutas para archivos sin sufijo `.component` (Angular 20+), sin `standalone: true`
  explícito, API de `resource` con `params`.
- `rules/csharp/patterns.md`: `ProblemDetails` en lugar de `ApiResponse<T>`; sin repositorio genérico.
- `rules/common/testing.md`, `rules/common/code-review.md`, `rules/csharp/testing.md`: cobertura de
  80% y TDD obligatorios solo en módulos críticos; tabla de agentes limitada a los incluidos.
- `skills/security-review`: eliminada la sección de seguridad blockchain (Solana).
- `skills/csharp-testing`: FluentAssertions → AwesomeAssertions.
- `agents/*`: descripciones sin delegación proactiva obligatoria.
- Ajustes generales del proyecto en `rules/serviya-overrides.md`.

### No incluido a propósito

Hooks (54 comandos, 16 en cada acción), comandos de planeación (se usa Spec Kit), reglas
`agents.md`, `development-workflow.md`, `git-workflow.md`, `hooks.md`, `patterns.md`,
`performance.md`, `web/design-quality.md` y skills ajenas al proyecto.
