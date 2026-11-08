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
npm start          # todas las áreas simuladas (environment.mocks)
```

Abrir `http://localhost:4200` en el emulador de celular del navegador (Pixel 7 y iPhone 14) y
comprobar:

1. Navegar por el alta del prestador en sus 10 pasos, cerrar a la mitad, volver y continuar (US1).
2. Buscar "Fontanero" en un municipio y abrir un perfil: no hay teléfono, correo ni dirección (US2).
3. Pulsar "Contactar" → registro → opciones de $39 y $129 con el aviso de no garantía (US3, US4).
4. Ver la cola del administrador con documentos ficticios y aprobar o rechazar con motivo (US1).
5. Instalar la app en la pantalla de inicio (PWA) desde Chrome en Android.

Los puntos 1 a 4 están automatizados en `web/e2e/*.prototipo.spec.ts` (ver `web/e2e/prototipo.spec.ts`).

### Cómo usar el prototipo

- La barra amarilla "Prototipo · ver como" cambia entre visitante, cliente, prestador y
  administración sin registrarse. "Reiniciar datos" borra todo lo capturado en ese navegador.
- El código de verificación de correo y de recuperación es siempre `123456`. No se guardan
  contraseñas: cualquier contraseña de 8 caracteres o más funciona.
- La pasarela de pago es simulada: "Pagar con tarjeta" se aprueba al instante, "OXXO" queda
  pendiente y en "Mis contactos" hay un botón para simular que se acreditó.
- Todo lo que se captura queda solo en el navegador (`localStorage`). Nunca uses documentos
  reales: las subidas no salen del navegador y el visor muestra imágenes "DOCUMENTO FICTICIO".

### Probar en el celular e instalar la app (Android)

Instalar una PWA exige HTTPS o `localhost`. Con el reenvío de puertos de Chrome, el celular abre el
prototipo como `localhost`:

1. En `web/`, compilar y servir el prototipo como en producción (puerto 4000):

   ```bash
   npm run serve:prototipo
   ```

2. En el celular Android, activar las opciones de desarrollador y la depuración por USB, y
   conectarlo a la computadora con un cable.
3. En Chrome de la computadora, abrir `chrome://inspect/#devices`, pulsar "Port forwarding…",
   agregar `4000` → `localhost:4000` y marcar "Enable port forwarding".
4. En Chrome del celular, abrir `http://localhost:4000`. Esperar unos segundos y usar el menú ⋮ →
   "Instalar app" (o "Agregar a la pantalla principal").

Verificado en la computadora (2026-11-08): el servidor entrega el manifiesto y registra el service
worker. Falta la prueba en un celular real (T037).

**iPhone**: Safari solo instala apps desde HTTPS, así que hace falta un túnel HTTPS hacia el puerto
4000 y agregar su dominio a `security.allowedHosts` en `web/angular.json`. Sin instalarla, el
prototipo se puede revisar en el emulador de iPhone 14 del navegador.

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
