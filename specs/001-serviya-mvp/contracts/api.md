# Contrato REST: API de ServiYa v1

Base: `https://api.serviya.mx/api/v1`. JSON en camelCase. Autenticación por cookie HttpOnly (research
R2). Todas las peticiones que modifican datos (`POST`, `PUT`, `PATCH`, `DELETE`) requieren el
encabezado `X-XSRF-TOKEN`. Errores en formato `application/problem+json` (RFC 9457) con `type`,
`title`, `status`, `detail` y, en validaciones, `errors` por campo. Paginación con `?pagina=1&tamano=20`
y la respuesta `{ items, total, pagina, tamano }`. Límites de uso: 429 con `Retry-After`.

Leyenda de acceso: **P** público · **C** cliente · **Pr** prestador · **A** administrador · **U**
cualquier usuario con sesión.

Las especificaciones de detalle (OpenAPI) se generan desde la API (research R10). Este documento es el
contrato funcional que siguen el prototipo y la API.

---

## Cuentas

| Método y ruta | Acceso | Cuerpo / respuesta | Requisito |
|---|---|---|---|
| `POST /cuentas/registro` | P | `{ email, password, nombreCompleto, fechaNacimiento, rol: "Cliente"\|"Prestador", aceptaciones: [{documento, version}] }` → 201 | FR-001, FR-004, FR-044 |
| `POST /cuentas/verificar-correo` | P | `{ email, codigo }` → 204 e inicia la sesión | FR-001 |
| `POST /cuentas/reenviar-codigo` | P | `{ email }` → 204 (máximo 3 por hora) | |
| `POST /cuentas/login` | P | `{ email, password }` → 200 `Sesion` | |
| `POST /cuentas/logout` | U | → 204 | |
| `POST /cuentas/recuperar` / `POST /cuentas/restablecer` | P | `{ email }` / `{ email, codigo, nuevaPassword }` | FR-001 |
| `GET /cuentas/yo` | U | → `Sesion { id, nombreCompleto, email, roles[], estado, esMayorDeEdad, aceptacionesPendientes[] }` | FR-044 |
| `POST /cuentas/yo/consentimientos` | U | `{ aceptaciones: [{documento, version}] }` → 204 (cuando cambia la versión de un texto legal) | FR-044 |
| `POST /cuentas/yo/roles/prestador` | U | Agrega el rol de prestador a un cliente → 204 | FR-002 |
| `GET /cuentas/yo/datos` | U | Descarga los datos personales (JSON) | FR-045 |
| `DELETE /cuentas/yo` | U | `{ password }` → 202 (eliminación y créditos a clientes afectados) | FR-043, FR-045 |
| `GET /cuentas/xsrf` | P | Emite la cookie `XSRF-TOKEN` | R2 |

## Alta del prestador

| Método y ruta | Acceso | Cuerpo / respuesta | Requisito |
|---|---|---|---|
| `GET /prestadores/yo` | Pr | → `PerfilPropio` (incluye datos privados, estado, paso, motivos pendientes) | FR-005, FR-011 |
| `PATCH /prestadores/yo` | Pr | Guardado parcial por paso: `{ paso, datosPersonales?, oficios?, cobertura?, disponibilidad?, descripcion? }` → 200 | FR-005 |
| `POST /prestadores/yo/consentimiento-sensible` | Pr | `{ version }` → 204 | FR-007 |
| `POST /prestadores/yo/archivos/url-subida` | Pr | `{ destino: "Documento"\|"Portafolio", tipo, tipoContenido, tamanoBytes }` → `{ archivoId, urlSubida, expiraEn }` | R3 |
| `POST /prestadores/yo/documentos` | Pr | `{ archivoId, tipo, fechaExpedicion? }` → 201 `DocumentoPropio` | FR-006 |
| `POST /prestadores/yo/portafolio` | Pr | `{ archivoId, tipo, descripcion }` → 201 | FR-008 |
| `DELETE /prestadores/yo/portafolio/{id}` | Pr | → 204 | |
| `POST /prestadores/yo/enviar-revision` | Pr | → 204 o 422 con la lista de faltantes | FR-009 |

## Búsqueda y perfiles públicos

| Método y ruta | Acceso | Cuerpo / respuesta | Requisito |
|---|---|---|---|
| `GET /catalogos/oficios` | P | → `Oficio[]` | |
| `GET /catalogos/municipios` | P | → `Municipio[]` (sin polígono) | |
| `POST /catalogos/municipios/ubicar` | P | `{ lat, lng }` → `Municipio` o 404 (en el cuerpo para que la ubicación no quede en URLs ni logs) | R5 |
| `GET /prestadores?oficio&municipio&calificacionMin&insignia&tarifaMax&pagina` | P | → página de `TarjetaPrestador { id, nombrePublico, colonia, municipio, oficios[], tarifaDesde, promedios, totalResenas, insignias[], fotoPrincipalUrl }` | FR-018, FR-019 |
| `GET /prestadores/{id}` | P | → `PerfilPublico` (**nunca** teléfono, correo, dirección ni documentos); 404 si no está aprobado | FR-017, US2 |
| `GET /prestadores/{id}/resenas?pagina` | P | → página de `ResenaPublica` | FR-039 |

## Acceso al contacto y pagos

| Método y ruta | Acceso | Cuerpo / respuesta | Requisito |
|---|---|---|---|
| `GET /accesos/opciones?prestadorId` | C | → `{ accesoVigente?, pase?: {contactosDisponibles, finEn}, creditosDisponibles, ofertaPase?: {montoCentavos, descuentoCentavos}, aviso: {version, texto}, precios }` | FR-022, FR-025, FR-027 |
| `POST /accesos` | C | `{ prestadorId, via: "Individual"\|"Pase"\|"Credito", avisoVersion }` → 201 `{ acceso, checkoutUrl? }` (`checkoutUrl` solo si se paga) | FR-020, FR-025, FR-029 |
| `POST /pases` | C | `{ renovacionAutomatica, avisoVersion }` → 201 `{ pase, checkoutUrl }` | FR-021, FR-027 |
| `POST /pases/{id}/cancelar-renovacion` | C | → 204 (una sola acción) | FR-028 |
| `GET /accesos/yo` | C | → `AccesoContacto[]` con estado y vencimiento | FR-024 |
| `GET /accesos/{id}/contacto` | C | → `{ telefono, whatsapp? }` solo si está `Activo`; 403 si no | FR-020 |
| `GET /pagos/yo` | C | → historial con comprobante | FR-031 |
| `GET /pagos/{id}/comprobante` | C | → `Comprobante` (JSON); la web lo muestra como página imprimible o guardable en PDF desde el navegador | FR-031 |
| `POST /pagos/{id}/factura` | C | `SolicitudFactura` → 201 | FR-031, R13 |
| `POST /creditos/{id}/cambiar-por-reembolso` | C | → 202 | FR-026 |

El regreso de Checkout Pro llega a `https://serviya.mx/pago/resultado?pagoId=…`, y la web consulta
`GET /pagos/{id}`. **El acceso se activa solo por webhook** (ver [webhooks.md](webhooks.md)).

## Chat y contrataciones

| Método y ruta | Acceso | Cuerpo / respuesta | Requisito |
|---|---|---|---|
| `GET /conversaciones` | U | → lista con último mensaje y no leídos | FR-032 |
| `GET /conversaciones/{id}/mensajes?antesDe` | U (parte) | → página de `Mensaje` | |
| `POST /conversaciones/{id}/mensajes` | U (parte) | `{ texto?, archivoId? }` → 201 (también por SignalR) | FR-032, FR-033 |
| `POST /archivos/url-subida` | U | `{ destino: "Chat"\|"Calificacion", tipoContenido, tamanoBytes }` → `{ archivoId, urlSubida, expiraEn }` | R3, R8 |
| `POST /notificaciones/push` | U | `PushSubscription` → 201 | R8 |
| `POST /conversaciones/{id}/contrataciones` | C | `{ descripcion, fechaServicio, precioAcordadoCentavos }` → 201 | FR-035 |
| `POST /contrataciones/{id}/confirmar` · `/rechazar` · `/terminar` · `/cancelar` | parte según estado | → 204 | FR-035 |

## Calificaciones

| Método y ruta | Acceso | Cuerpo / respuesta | Requisito |
|---|---|---|---|
| `POST /contrataciones/{id}/calificacion` | parte | Cliente: `{ calidad, precioJusto, puntualidad, trato, comentario?, archivoIds[] }`; prestador: `{ puntualidadPago, tratoCliente, claridad, comentario? }` → 201; 409 si ya calificó; 422 si no está terminada | FR-036, FR-037 |
| `POST /calificaciones/{id}/respuesta` | Pr (destinatario) | `{ texto }` → 201; 409 si ya respondió | FR-038 |

## Reportes y bloqueo

| Método y ruta | Acceso | Cuerpo / respuesta | Requisito |
|---|---|---|---|
| `POST /reportes` | U | `{ objetivoTipo, objetivoId, motivo, detalle? }` → 201 | FR-040 |
| `POST /bloqueos` / `DELETE /bloqueos/{usuarioId}` | U | → 204 | FR-040 |
| `POST /suspensiones/yo/aclaracion` | U suspendido | `{ texto }` → 201 | FR-041 |

## Administración (rol `Admin`; todo queda en la bitácora)

| Método y ruta | Cuerpo / respuesta | Requisito |
|---|---|---|
| `GET /admin/revisiones?estado=EnRevision` | Cola de perfiles por fecha de envío | FR-010 |
| `GET /admin/prestadores/{id}` | Perfil completo con documentos (sin URLs) | FR-010 |
| `POST /admin/documentos/{id}/ver` | → `{ url, expiraEn }` (5 min); **registra `VerDocumento`** | FR-016 |
| `POST /admin/documentos/{id}/decision` | `{ decision: "Aprobar"\|"RequiereCorreccion"\|"Rechazar", motivo? }` (motivo obligatorio salvo al aprobar) | FR-010, FR-011 |
| `POST /admin/prestadores/{id}/decision` | `{ decision: "Aprobar"\|"Rechazar", motivo? }`; 422 si falta aprobar documentos | FR-009, FR-010 |
| `POST /admin/portafolio/{id}/decision` | `{ decision, motivo? }` | FR-012 |
| `GET /admin/reportes?estado` · `POST /admin/reportes/{id}/resolver` | `{ resolucion, ocultarContenido? }` | FR-041 |
| `POST /admin/usuarios/{id}/suspender` · `/reactivar` | `{ motivo }` | FR-041, FR-043 |
| `POST /admin/usuarios/{id}/roles/admin` · `DELETE …/roles/admin` | Otorga o retira el rol | FR-003 |
| `POST /admin/pagos/{id}/reembolsar` | `{ motivo }` | FR-026 |
| `GET /admin/facturas?estado` · `POST /admin/facturas/{id}/emitida` | | R13 |
| `GET /admin/bitacora?actor&accion&desde&hasta` | Solo lectura | FR-042 |
| `GET /admin/alertas` | Contactos anómalos (FR-029) y pagos duplicados | FR-029 |

## Reglas transversales

- Las respuestas públicas se construyen con DTOs específicos (`TarjetaPrestador`, `PerfilPublico`,
  `ResenaPublica`); **nunca** se serializa una entidad. Una prueba de contrato verifica que estos DTOs
  no contengan campos privados (SC-006).
- Límites de uso: login y códigos (5 por minuto por IP), `POST /accesos` (10 contactos nuevos por día
  por cliente, FR-029) y mensajes (30 por minuto por usuario).
- Un usuario suspendido recibe 403 con `type: "/errores/cuenta-suspendida"` y el motivo, salvo en
  `GET /cuentas/yo` y la aclaración.
