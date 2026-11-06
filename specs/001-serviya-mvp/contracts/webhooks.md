# Contrato: webhooks de Mercado Pago y tiempo real

## Webhook `POST /api/v1/webhooks/mercadopago`

Sin cookie ni XSRF; lo autentica la firma. Tópicos atendidos: `payment`, `subscription_preapproval` y
`subscription_authorized_payment`.

**Procesamiento (en este orden):**

1. Leer los encabezados `x-signature` (`ts=…,v1=…`) y `x-request-id`, y `data.id` de la URL.
2. Validar `v1` = HMAC-SHA256(clave secreta, `id:{data.id};request-id:{x-request-id};ts:{ts};`),
   con el validador del SDK de C#. Si no coincide → **401**, y no se procesa nada.
3. Insertar `NotificacionMp` con `RequestId` único. Si ya existía → **200** (reintento duplicado).
4. Responder **200** de inmediato y procesar en segundo plano (cola en memoria más reintento por la
   tarea programada si falla):
   - `payment`: `GET /v1/payments/{data.id}` a Mercado Pago, buscar el `Pago` por
     `external_reference` y aplicar el estado (research R6). Si ya estaba en ese estado, no hacer nada.
   - Pago `approved` de un contacto → acceso `Activo`, `VenceEn = ahora + 30 días`, se notifica al
     cliente (push y correo) y se abre la conversación.
   - Pago `approved` de un pase → `PaseMensual` `Activo`.
   - `subscription_authorized_payment` aprobado → nuevo periodo del pase.
   - Pago duplicado (segundo `approved` para un acceso ya activo) → se registra un `Credito` con
     motivo `PagoDuplicado` y una alerta para el administrador.
5. Nunca se confía en montos ni estados del cuerpo del webhook: siempre se consulta a Mercado Pago.

**Pruebas obligatorias (principio VI):** firma válida, firma inválida, `ts` alterado, notificación
duplicada, pago `pending` que pasa a `approved`, `rejected`, pago duplicado y referencia vencida.

---

## Hub SignalR `/hubs/chat`

Autenticado por cookie. Al conectarse, el usuario se une a los grupos de sus conversaciones.

| Dirección | Método | Datos |
|---|---|---|
| Cliente → servidor | `EnviarMensaje` | `{ conversacionId, texto?, archivoId?, idCliente }` (`idCliente` evita duplicados al reintentar) |
| Cliente → servidor | `MarcarLeido` | `{ conversacionId, hastaMensajeId }` |
| Servidor → cliente | `MensajeRecibido` | `Mensaje` (con `textoMostrado`) |
| Servidor → cliente | `AccesoActualizado` | `{ conversacionId, estado, venceEn }`, por ejemplo cuando se activa el pago o vence el acceso |
| Servidor → cliente | `Error` | `{ idCliente, problem }`, por ejemplo acceso vencido (solo lectura) o usuario bloqueado |

Reglas: el servidor valida que el autor sea parte de la conversación, que el acceso esté `Activo` y que
no exista un bloqueo. Si no hay acceso activo y el autor es el prestador, el texto se guarda con
`ContactMasker` aplicado (FR-033). El primer mensaje del cliente fija `PrimerMensajeClienteEn` y la
primera respuesta del prestador fija `RespuestaPrestadorEn` (FR-026).

## Web Push

Carga útil `{ titulo, cuerpo, url }` sin datos personales sensibles; por ejemplo "Tienes un mensaje
nuevo de Juan P." con enlace a la conversación.
