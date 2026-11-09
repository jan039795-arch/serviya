# CLAUDE.md — ServiYa

Plataforma web instalable (PWA, Android/iPhone) que conecta a clientes con prestadores de oficios
verificados en México. La plataforma **solo cobra por el contacto** ($39 MXN por prestador o
$129 MXN por pase mensual) y **no cobra comisión** sobre el trabajo. Desarrollador único: el usuario
(senior en C# y Java, conoce Angular), con ayuda de Claude.

## Al iniciar cada sesión

1. Lee [docs/PROGRESO.md](docs/PROGRESO.md): estado actual, siguiente paso exacto y decisiones
   tomadas. **No vuelvas a analizar el proyecto desde cero** ni a reabrir decisiones registradas ahí,
   salvo que el usuario lo pida.
2. Si el trabajo toca una funcionalidad, abre su carpeta en `specs/` (la activa está en
   `.specify/feature.json`).

## Después de cada cambio

Actualiza `docs/PROGRESO.md` **en el mismo commit que el cambio**:

- "Estado actual" y "Siguiente paso exacto" (archivos, tareas y orden).
- Una línea nueva en la "Bitácora" con fecha, qué se hizo y su commit si ya existe.
- Toda decisión nueva en la tabla de "Decisiones", con su motivo.

## Documentos fuente (en orden de autoridad)

1. [.specify/memory/constitution.md](.specify/memory/constitution.md): principios obligatorios
   (privacidad, solo contacto, confianza, mobile-first, simplicidad, pruebas en lo crítico).
2. `specs/<feature>/spec.md`, `plan.md` y `tasks.md`: qué se construye y en qué orden.
3. [.claude/rules/serviya-overrides.md](.claude/rules/serviya-overrides.md): ajustes sobre las reglas
   de ECC en `.claude/rules/`.

## Stack

- Frontend: Angular 22 (standalone, signals, PWA, SSR para perfiles públicos).
- Backend: ASP.NET Core 10 Web API, monolito modular, EF Core, ASP.NET Identity, SignalR.
- Base de datos: PostgreSQL. Archivos: Cloudflare R2 (privado). Pagos: Mercado Pago (tarjeta, OXXO,
  SPEI).

## Flujo de trabajo

- Repositorio: `https://github.com/jan039795-arch/serviya` (privado), rama `main`. Subir con
  `git push` solo cuando el usuario lo pida.
- Spec Kit: `/speckit-specify` → `/speckit-clarify` (opcional) → `/speckit-plan` → `/speckit-tasks`
  → `/speckit-implement`. El CLI `specify.exe` está bloqueado por Windows Application Control; los
  scripts de `.specify/scripts/powershell/` sí funcionan.
- Revisores al cerrar cada etapa o a pedido: `csharp-reviewer`, `typescript-reviewer`,
  `security-reviewer`, `database-reviewer`.
- Prototipo con datos ficticios antes de manejar documentos o dinero reales. Nunca usar documentos
  reales en pruebas.
- Commits convencionales (`feat:`, `fix:`, `docs:`...). Documentación y textos en español de México.

## Comandos

Frontend (desde `web/`; requiere Node 24):

```bash
npm start                    # servidor de desarrollo en http://localhost:4200 (datos ficticios)
npm run start:prototipo      # prototipo optimizado, todo simulado
npm run serve:prototipo      # compila el prototipo y lo sirve como en producción (puerto 4000, PWA)
npx ng build                 # compilación de producción
npx ng test --no-watch       # pruebas unitarias (Vitest)
npx playwright test          # E2E en Pixel 7 e iPhone 14 (levanta npm start)
npm run lint                 # ESLint
npm run format               # Prettier
```

Backend (desde `api/`; requiere .NET SDK 10 y, para la base de datos, Docker Desktop):

```bash
powershell -ExecutionPolicy Bypass -File ../scripts/configurar-desarrollo.ps1  # una vez: .env y user-secrets
docker compose up -d                          # desde la raíz: PostgreSQL + PostGIS y RustFS (S3)
dotnet ef database update                     # migraciones (cuando existan, Fase 4)
dotnet run --project src/ServiYa.Api          # API en http://localhost:5189
dotnet build                                  # advertencias = errores
dotnet test                                   # xUnit v3 (Microsoft.Testing.Platform)
dotnet test --coverage --coverage-output-format cobertura   # con cobertura
```
