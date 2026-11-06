# Guía de validación: ServiYa MVP

Cómo comprobar de punta a punta que el MVP funciona. Endpoints en [contracts/api.md](contracts/api.md);
entidades en [data-model.md](data-model.md).

## Requisitos previos

| Herramienta | Versión | Estado en el equipo (2026-11-05) |
|---|---|---|
| .NET SDK | 10.0.x | ✅ 10.0.402 |
| Node.js | 24 LTS | ✅ 24.22 |
| Angular CLI | 22.x (`npx @angular/cli@22`) | Se instala con el proyecto |
| Docker Desktop (con WSL 2) | reciente | ❌ **Falta**: necesario para PostgreSQL local y Testcontainers |
| Cuenta de Mercado Pago (desarrollador) | — | Pendiente: credenciales de prueba y clave secreta del webhook |
| Cuenta de Cloudflare R2 | — | Pendiente (en desarrollo se puede usar MinIO en Docker) |

Variables de entorno (en `dotnet user-secrets` para desarrollo; nunca en el repositorio):
`ConnectionStrings__Default`, `Storage__Endpoint`, `Storage__AccessKey`, `Storage__SecretKey`,
`MercadoPago__AccessToken`, `MercadoPago__WebhookSecret`, `Email__ApiKey`, `WebPush__PublicKey` y
`WebPush__PrivateKey`.

## Etapa 2: prototipo con datos ficticios (solo frontend)

```bash
cd web
npm install
npm start          # con environment.useMocks = true
```

Abrir `http://localhost:4200` en el emulador de celular del navegador (Pixel 7 y iPhone 14) y
comprobar:

1. Navegar por el alta del prestador en sus 10 pasos, cerrar a la mitad, volver y continuar (US1).
2. Buscar "Fontanero" en un municipio y abrir un perfil: no hay teléfono, correo ni dirección (US2).
3. Pulsar "Contactar" → registro → opciones de $39 y $129 con el aviso de no garantía (US3, US4).
4. Ver la cola del administrador con documentos ficticios y aprobar o rechazar con motivo (US1).
5. Instalar la app en la pantalla de inicio (PWA) desde Chrome en Android.

## Etapas 3 a 9: sistema completo

```bash
docker compose up -d            # PostgreSQL 17 con PostGIS y MinIO
cd api && dotnet ef database update && dotnet run --project src/ServiYa.Api
cd web && npm start             # environment.useMocks = false
```

Escenarios a validar (cada uno corresponde a sus escenarios de aceptación en [spec.md](spec.md)):

| # | Escenario | Resultado esperado |
|---|---|---|
| 1 | Registrar un prestador con documentos **ficticios**, enviarlo a revisión y aprobarlo desde administración | Perfil publicado; bitácora con `VerDocumento` y `AprobarPerfil` |
| 2 | Pedir corrección de la INE | El prestador ve el motivo y solo vuelve a subir la INE |
| 3 | Pagar un contacto con la tarjeta de prueba aprobada de Mercado Pago | El acceso se activa en menos de 1 minuto (SC-005); aparecen el teléfono y el chat |
| 4 | Pagar con OXXO en modo de prueba y simular la acreditación | El acceso queda "Pago pendiente" hasta el webhook `approved` |
| 5 | Enviar al webhook una notificación con la firma alterada | 401, sin cambios en la base de datos |
| 6 | Escribir "llámame al 55 1234 5678" sin acceso activo | Llega oculto al destinatario |
| 7 | Adelantar el reloj 48 h sin respuesta del prestador | El cliente recibe un crédito |
| 8 | Comprar el pase con renovación y cancelarla | Un solo clic; el acceso se conserva hasta `FinEn` |
| 9 | Registrar una contratación, terminarla y calificar desde una sola cuenta | La calificación no es visible hasta que califica la otra parte o pasan 14 días |
| 10 | Pedir el perfil público y la tarjeta de búsqueda | Ningún campo privado en la respuesta (prueba de contrato, SC-006) |

## Pruebas automatizadas

```bash
cd api && dotnet test                       # unitarias e integración (requiere Docker)
cd web && npx ng test --no-watch            # unitarias del frontend
cd web && npx playwright test               # E2E en perfiles de celular
```

Criterio de salida: todas las pruebas pasan y los módulos críticos (Pagos, permisos, Documentos,
desbloqueo de contacto y `ContactMasker`) tienen al menos 80% de cobertura (constitución, principio
VI).
