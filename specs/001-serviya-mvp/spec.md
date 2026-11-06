# Feature Specification: ServiYa MVP — Plataforma de contacto con prestadores de oficios verificados

**Feature Branch**: `001-serviya-mvp`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "MVP de ServiYa: plataforma web instalable (Android/iPhone) donde prestadores de oficios crean cuenta y perfil con documentos (INE + selfie, domicilio, carta de no antecedentes opcional), portafolio y video; un administrador revisa y aprueba; visitantes ven perfiles sin contacto; clientes pagan $39 MXN por contacto o $129 MXN por pase/suscripción mensual; chat que oculta datos antes del pago; registro de contratación; calificaciones doble ciega en cuatro ejes; reportes y suspensiones; sin comisión sobre el trabajo."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Alta y verificación del prestador (Priority: P1)

Una persona que ofrece un oficio (por ejemplo, fontanero) crea su cuenta desde el celular, captura sus
datos personales, oficios, experiencia, zona de cobertura, disponibilidad y tarifas de referencia, sube
su INE por ambos lados, una selfie, su comprobante de domicilio y, si quiere, su carta de no
antecedentes penales, además de un video de presentación y fotos de trabajos realizados. Puede dejar el
proceso a medias y retomarlo después. Al terminar, envía su perfil a revisión. Un administrador revisa
cada documento y lo aprueba, lo rechaza o pide corrección indicando el motivo. El prestador recibe un
aviso, corrige solo lo señalado y, una vez aprobado, su perfil se publica.

**Why this priority**: sin prestadores verificados no hay nada que ofrecer a los clientes; es la base
de la confianza de toda la plataforma.

**Independent Test**: registrar un prestador con documentos ficticios, revisarlo desde la cuenta de
administrador y comprobar que el perfil aparece publicado solo después de la aprobación.

**Acceptance Scenarios**:

1. **Given** una persona sin cuenta, **When** se registra como prestador y verifica su correo o
   teléfono con un código, **Then** accede al formulario de alta por pasos.
2. **Given** un prestador que capturó la mitad de los pasos, **When** cierra la app y vuelve días
   después, **Then** encuentra guardado todo lo capturado y continúa donde se quedó.
3. **Given** un prestador con todos los documentos obligatorios cargados, **When** envía a revisión,
   **Then** su perfil queda en estado "En revisión" y aparece en la cola del administrador.
4. **Given** un perfil en revisión, **When** el administrador marca la INE como "requiere corrección"
   con el motivo "foto borrosa", **Then** el prestador recibe un aviso con ese motivo y solo debe volver
   a subir ese documento.
5. **Given** un perfil con todos los documentos obligatorios aprobados, **When** el administrador
   aprueba el perfil, **Then** se publica y queda registrado en la bitácora quién lo aprobó y cuándo.
6. **Given** un prestador que subió una carta de no antecedentes aprobada, **When** su perfil se
   publica, **Then** muestra la insignia "Antecedentes presentados"; sin carta, el perfil se publica
   igual pero sin esa insignia.
7. **Given** una persona cuya fecha de nacimiento indica menos de 18 años, **When** intenta enviar
   su perfil a revisión, **Then** el sistema lo impide y explica el motivo.

---

### User Story 2 - Búsqueda y perfiles públicos (Priority: P1)

Cualquier persona, aun sin cuenta, busca un oficio en su zona, filtra por calificación, insignias y
tarifa de referencia, y abre perfiles con video, fotos de trabajos, calificaciones y reseñas. No ve
teléfono, correo, dirección ni documentos del prestador; en su lugar, ve un botón para contactarlo.

**Why this priority**: es la vitrina que convierte visitantes en clientes que pagan.

**Independent Test**: con prestadores ficticios aprobados, buscar "fontanero" en una colonia y
comprobar que aparecen solo los aprobados que cubren esa zona y que ningún dato de contacto es visible.

**Acceptance Scenarios**:

1. **Given** varios prestadores aprobados, **When** un visitante busca un oficio en una zona,
   **Then** ve solo prestadores aprobados y activos que cubren esa zona, ordenados por relevancia.
2. **Given** un visitante en un perfil público, **When** revisa la página, **Then** ve nombre de
   pila e inicial del apellido, colonia o zona, oficios, experiencia, tarifas de referencia, video,
   fotos, insignias, calificaciones por eje y reseñas, pero no ve teléfono, correo, dirección ni
   documentos.
3. **Given** un perfil pendiente, rechazado o suspendido, **When** alguien intenta abrirlo,
   **Then** no se muestra en búsquedas ni es accesible por enlace directo.

---

### User Story 3 - Pago de acceso y desbloqueo de contacto (Priority: P1)

Un cliente se registra gratis y, al querer contactar a un prestador, elige entre pagar un contacto
individual de $39 MXN o un pase mensual de $129 MXN. Antes de pagar ve qué incluye, la vigencia, la
regla de crédito y el aviso de que paga por acceso y contacto, no por el trabajo, y que no se garantiza
la contratación. Paga con tarjeta, OXXO o SPEI. Al confirmarse el pago se desbloquean el chat y el
teléfono del prestador.

**Why this priority**: es la única fuente de ingresos de la plataforma.

**Independent Test**: en modo de pruebas de la pasarela, pagar un contacto con tarjeta y otro con
OXXO, y comprobar que el primero se desbloquea al instante y el segundo solo cuando el pago se confirma.

**Acceptance Scenarios**:

1. **Given** un visitante sin cuenta, **When** pulsa "Contactar", **Then** se le pide registrarse
   o iniciar sesión antes de ver las opciones de pago.
2. **Given** un cliente con sesión, **When** elige "Contacto individual", **Then** ve el precio de
   $39 MXN, que incluye chat y teléfono de ese prestador por 30 días, la regla de crédito si no
   responde en 48 horas y el aviso de no garantía, antes de confirmar.
3. **Given** un pago con tarjeta aprobado, **When** se confirma, **Then** el cliente ve de inmediato
   el chat y el teléfono del prestador.
4. **Given** un pago en OXXO o SPEI, **When** el cliente genera la referencia, **Then** el acceso
   queda "Pago pendiente" con instrucciones, y se desbloquea automáticamente solo cuando la pasarela
   confirma el pago.
5. **Given** una referencia OXXO no pagada en su plazo, **When** vence, **Then** el acceso pasa a
   "Expirado" sin cobro y el cliente puede intentar de nuevo.
6. **Given** un cliente que pagó un contacto individual hace 30 días, **When** se cumple la vigencia,
   **Then** el chat queda en solo lectura y el teléfono se oculta de nuevo, salvo que tenga un pase
   activo.
7. **Given** un cliente que pagó un contacto y el prestador no respondió en 48 horas desde el primer
   mensaje del cliente, **When** se cumple el plazo, **Then** el cliente recibe automáticamente un
   crédito por un contacto nuevo, que puede cambiar por devolución del dinero si lo solicita.

---

### User Story 4 - Pase o suscripción mensual (Priority: P2)

Un cliente que necesita contactar a varios prestadores compra un pase de 30 días por $129 MXN con hasta
30 contactos nuevos. Puede pagarlo una sola vez (tarjeta, OXXO o SPEI) o activar la renovación
automática con tarjeta. Si activó la renovación, recibe un aviso 3 días antes y puede cancelarla en un
clic. Cuando va a pagar su segundo contacto individual en 30 días, se le ofrece el pase descontando lo
que ya pagó en contactos individuales.

**Why this priority**: aumenta el ingreso por cliente, pero el MVP puede operar solo con contactos
individuales.

**Independent Test**: comprar un pase, desbloquear 30 contactos, comprobar que el 31.º pide pago y que
la cancelación de la renovación funciona en un clic.

**Acceptance Scenarios**:

1. **Given** un cliente con pase activo, **When** contacta a un prestador nuevo, **Then** se
   desbloquea sin pago y su contador de contactos disponibles baja en uno.
2. **Given** un cliente que ya usó 30 contactos del pase, **When** intenta contactar a otro
   prestador, **Then** se le informa del límite y se le ofrece pagar un contacto individual.
3. **Given** un cliente que pagó un contacto individual en los últimos 30 días, **When** va a pagar
   un segundo, **Then** se le ofrece el pase por $129 MXN menos lo pagado en contactos individuales
   de ese periodo (por ejemplo, $90 MXN).
4. **Given** un pase con renovación automática, **When** faltan 3 días para el cobro, **Then** el
   cliente recibe un aviso con la fecha, el monto y un enlace para cancelar.
5. **Given** un pase con renovación automática, **When** el cliente pulsa "Cancelar renovación",
   **Then** se cancela con esa sola acción, conserva el acceso hasta el fin del periodo pagado y no
   se le vuelve a cobrar.
6. **Given** un prestador ya contactado durante un pase, **When** el pase termina, **Then** la
   conversación permanece en solo lectura.

---

### User Story 5 - Chat y registro de contratación (Priority: P2)

Cliente y prestador conversan en tiempo real dentro de la app, enviando texto y fotos. Antes de que
exista un acceso pagado, cualquier teléfono, correo o enlace de mensajería escrito en el chat se oculta.
Cuando llegan a un acuerdo, el cliente registra la contratación (descripción, fecha y precio acordado) y
el prestador la confirma. Después pueden marcarla como terminada o cancelada.

**Why this priority**: da evidencia del acuerdo, habilita las calificaciones y protege el modelo de
cobro.

**Independent Test**: enviar un mensaje con un teléfono antes y después del pago y comprobar que se
oculta solo antes; registrar y confirmar una contratación.

**Acceptance Scenarios**:

1. **Given** un acceso pagado, **When** cliente y prestador intercambian mensajes, **Then** se
   entregan en tiempo real y el destinatario recibe una notificación si no está en la app.
2. **Given** un prestador que escribe "llámame al 55 1234 5678" a un cliente sin acceso pagado
   (por ejemplo, en respuesta a una solicitud vencida), **When** se envía, **Then** el número aparece
   oculto para el cliente.
3. **Given** una conversación activa, **When** el cliente registra la contratación con descripción,
   fecha y precio acordado, **Then** el prestador puede confirmarla o rechazarla, y queda el registro
   con su estado.
4. **Given** una contratación confirmada, **When** cualquiera de las partes la marca como terminada,
   **Then** se abre el periodo de calificación para ambos.
5. **Given** una contratación, **When** se muestra el precio acordado, **Then** la app indica que el
   pago del trabajo se hace directamente entre las partes y que la plataforma no lo cobra.

---

### User Story 6 - Calificaciones doble ciega (Priority: P2)

Al terminar una contratación, el cliente califica al prestador de 1 a 5 en calidad, precio justo,
puntualidad/rapidez y trato, y puede agregar un comentario y fotos. El prestador califica al cliente.
Ninguno ve la calificación del otro hasta que ambos califican o vence el plazo de 14 días. El prestador
puede responder públicamente a su reseña.

**Why this priority**: la reputación es lo que diferencia a la plataforma de un directorio cualquiera.

**Independent Test**: completar una contratación ficticia, calificar desde ambas cuentas y comprobar
que las calificaciones aparecen solo al final y actualizan los promedios del perfil.

**Acceptance Scenarios**:

1. **Given** un cliente sin contratación terminada con un prestador, **When** intenta calificarlo,
   **Then** el sistema no lo permite.
2. **Given** una contratación terminada, **When** solo el cliente ha calificado, **Then** su
   calificación no es visible para el prestador ni en el perfil público.
3. **Given** ambas partes calificaron, o pasaron 14 días, **When** se publica, **Then** el perfil
   del prestador actualiza sus promedios por eje y muestra la reseña.
4. **Given** una reseña publicada, **When** el prestador la responde, **Then** su respuesta aparece
   debajo de la reseña.
5. **Given** un prestador con pocas reseñas, **When** se muestra en búsquedas, **Then** su posición
   considera el número de reseñas y no solo el promedio, para que una sola reseña de 5 no supere a
   muchas reseñas altas.

---

### User Story 7 - Reportes, bloqueo y suspensión (Priority: P3)

Cualquier usuario puede reportar un perfil, mensaje o reseña, y bloquear a otro usuario. El
administrador revisa los reportes y puede ocultar contenido o suspender cuentas indicando el motivo. El
usuario suspendido recibe el motivo y puede enviar una aclaración, que el administrador resuelve.

**Why this priority**: necesario antes de abrir a usuarios reales, pero no para validar los flujos
principales.

**Independent Test**: reportar un mensaje ficticio, suspender la cuenta desde administración, enviar
una aclaración y reactivar la cuenta.

**Acceptance Scenarios**:

1. **Given** un mensaje ofensivo, **When** el destinatario lo reporta, **Then** aparece en la cola de
   reportes del administrador con el contexto de la conversación.
2. **Given** un usuario bloqueado, **When** intenta enviar mensajes a quien lo bloqueó, **Then** no
   puede hacerlo.
3. **Given** un administrador que suspende una cuenta con motivo, **When** se aplica, **Then** el
   perfil deja de aparecer, el usuario recibe el motivo y queda registrado en la bitácora.
4. **Given** un usuario suspendido, **When** envía una aclaración, **Then** el administrador puede
   mantener la suspensión o levantarla, con registro de la decisión.

---

### User Story 8 - Privacidad y control de la cuenta (Priority: P3)

Cada usuario consulta el aviso de privacidad y los términos, otorga consentimiento expreso para el uso
de documentos y biometría, descarga sus datos y puede eliminar su cuenta desde la app.

**Why this priority**: obligación legal antes del lanzamiento público; no bloquea el prototipo.

**Independent Test**: solicitar la eliminación de una cuenta de prestador ficticia y comprobar que el
perfil desaparece y los documentos se eliminan según la política de conservación.

**Acceptance Scenarios**:

1. **Given** un prestador que va a subir documentos, **When** llega a ese paso, **Then** debe aceptar
   explícitamente el consentimiento para datos sensibles antes de continuar.
2. **Given** un usuario, **When** solicita eliminar su cuenta y la confirma, **Then** su perfil deja
   de ser visible de inmediato y sus documentos se eliminan, conservando solo lo que la ley obliga
   (por ejemplo, registros de pagos).
3. **Given** un usuario, **When** solicita sus datos, **Then** recibe una copia de su información
   personal registrada.

---

### Edge Cases

- El prestador sube un documento ilegible, de otra persona o con la selfie que no coincide con la
  INE: el administrador lo rechaza con motivo y el perfil no se publica.
- La carta de no antecedentes tiene más de 12 meses: la insignia se retira y el prestador recibe un
  aviso para renovarla, 30 días antes de vencer.
- La pasarela confirma un pago duplicado o tardío para un acceso que ya estaba activo: no se duplica
  el acceso y el excedente se registra para devolución o crédito.
- El aviso de pago llega antes de que el cliente regrese a la app: el acceso ya aparece desbloqueado
  al volver.
- El cliente intenta pagar de nuevo a un prestador al que ya tiene acceso vigente: se le informa y no
  se cobra.
- El prestador es suspendido mientras un cliente tiene acceso pagado vigente: el cliente recibe un
  crédito por un contacto nuevo.
- El prestador elimina su cuenta con accesos vigentes: los clientes afectados reciben crédito.
- Intentos de evadir el filtro del chat (números con espacios, letras, emojis, "arroba"): se ocultan
  los patrones comunes y el prestador puede ser reportado.
- Fotos de portafolio con teléfonos o datos de contacto escritos: el administrador las rechaza al
  moderar.
- Un cliente con pase activo contacta a muchos prestadores en muy poco tiempo: el sistema limita los
  contactos diarios y marca el caso para revisión.
- Conexión inestable al subir video o fotos: la carga se puede reintentar sin repetir el formulario.
- Cancelación de renovación el mismo día del cobro: si el cobro aún no se procesó, no se realiza.

## Requirements *(mandatory)*

### Functional Requirements

**Cuentas y roles**

- **FR-001**: El sistema DEBE permitir registrarse con correo o teléfono, verificar con un código de
  un solo uso, iniciar sesión, cerrar sesión y recuperar la contraseña.
- **FR-002**: El sistema DEBE manejar los roles Cliente, Prestador y Administrador; una misma persona
  puede ser cliente y prestador con la misma cuenta.
- **FR-003**: Solo un administrador DEBE poder otorgar o retirar el rol de administrador a otra
  cuenta, sin compartir credenciales.
- **FR-004**: El sistema DEBE impedir que menores de 18 años publiquen un perfil o paguen accesos.

**Alta y verificación del prestador**

- **FR-005**: El sistema DEBE ofrecer un alta por pasos con guardado automático: datos personales
  (nombre completo, fecha de nacimiento, teléfono, dirección), oficios con años de experiencia, zona
  de cobertura, disponibilidad, tarifas de referencia, documentos, video y portafolio.
- **FR-006**: Los documentos obligatorios DEBEN ser INE por ambos lados, selfie y comprobante de
  domicilio; la carta de no antecedentes penales DEBE ser opcional.
- **FR-007**: El sistema DEBE pedir consentimiento expreso para datos sensibles y biométricos antes de
  la carga de documentos.
- **FR-008**: El video de presentación DEBE ser opcional, de máximo 90 segundos; el portafolio DEBE
  admitir hasta 20 fotos con descripción.
- **FR-009**: El perfil DEBE pasar por los estados Borrador → En revisión → Aprobado / Requiere
  corrección / Rechazado → Suspendido / Eliminado.
- **FR-010**: El administrador DEBE poder ver cada documento, aprobarlo, rechazarlo o pedir
  corrección con un motivo obligatorio, y aprobar o rechazar el perfil completo.
- **FR-011**: El sistema DEBE notificar al prestador cada cambio de estado con su motivo.
- **FR-012**: El administrador DEBE moderar video y fotos antes de que sean públicos.
- **FR-013**: El sistema DEBE avisar al prestador 30 días antes de que su carta de antecedentes
  cumpla 12 meses y retirar la insignia al vencer.
- **FR-014**: El alta del prestador DEBE ser gratuita durante el piloto; el sistema DEBE permitir
  activar más adelante una cuota de administración que se cobre solo al aprobar el perfil.

**Privacidad de documentos**

- **FR-015**: Los documentos y datos sensibles NUNCA DEBEN ser visibles para clientes, visitantes ni
  otros prestadores; solo para el propio prestador y los administradores.
- **FR-016**: Cada consulta de un documento por un administrador DEBE quedar registrada en una
  bitácora (quién, qué, cuándo, acción).
- **FR-017**: El perfil público DEBE mostrar solo nombre de pila e inicial del apellido, colonia o
  zona, oficios, experiencia, tarifas de referencia, disponibilidad, video y fotos aprobados,
  insignias, calificaciones y reseñas.

**Búsqueda**

- **FR-018**: Cualquier persona DEBE poder buscar prestadores aprobados por oficio y zona, y filtrar
  por calificación, insignias y tarifa de referencia.
- **FR-019**: Los resultados DEBEN excluir perfiles no aprobados, suspendidos o eliminados y
  ordenarse considerando calificación, número de reseñas y cercanía.

**Pagos y acceso**

- **FR-020**: El sistema DEBE ofrecer un contacto individual de $39 MXN (IVA incluido) que desbloquea
  el chat y el teléfono de un prestador durante 30 días.
- **FR-021**: El sistema DEBE ofrecer un pase mensual de $129 MXN (IVA incluido) con hasta 30 contactos
  nuevos durante 30 días, como pago único o con renovación automática opcional (solo con tarjeta).
- **FR-022**: Antes de cada pago, el sistema DEBE mostrar qué incluye, vigencia, regla de crédito y
  devolución, y el aviso "Pagas por acceso y contacto, no por el trabajo. No garantiza la
  contratación."
- **FR-023**: El sistema DEBE aceptar tarjeta, OXXO y SPEI; los pagos en efectivo o transferencia
  DEBEN quedar como "Pago pendiente" y desbloquearse solo al recibir la confirmación verificada de la
  pasarela.
- **FR-024**: El sistema DEBE manejar los estados de acceso Pendiente → Activo → Vencido, además de
  Expirado (pago no realizado) y Reembolsado.
- **FR-025**: El sistema NO DEBE cobrar dos veces un acceso vigente al mismo prestador.
- **FR-026**: Si el prestador no responde en 48 horas desde el primer mensaje del cliente en un
  acceso pagado, el sistema DEBE otorgar un crédito por un contacto nuevo; el cliente DEBE poder
  solicitar la devolución en dinero en lugar del crédito.
- **FR-027**: Al pagar un segundo contacto individual dentro de 30 días, el sistema DEBE ofrecer el
  pase descontando lo pagado en contactos individuales en ese periodo.
- **FR-028**: Con renovación automática, el sistema DEBE avisar 3 días antes del cobro y permitir
  cancelarla en una sola acción, conservando el acceso hasta el fin del periodo pagado.
- **FR-029**: El sistema DEBE limitar los contactos nuevos por día de un mismo cliente (10 por defecto)
  y marcar para revisión los patrones anómalos.
- **FR-030**: El sistema NO DEBE procesar, cobrar ni retener pagos del trabajo entre cliente y
  prestador, ni cobrar comisión sobre él.
- **FR-031**: El sistema DEBE emitir un comprobante de cada pago y permitir solicitar factura.

**Chat**

- **FR-032**: El sistema DEBE ofrecer chat en tiempo real con texto y fotos entre cliente y prestador
  con acceso activo, y notificar los mensajes nuevos.
- **FR-033**: El sistema DEBE ocultar teléfonos, correos y enlaces a otras apps de mensajería en el
  chat cuando no exista un acceso activo entre las partes.
- **FR-034**: Al vencer el acceso, la conversación DEBE quedar en solo lectura.

**Contratación**

- **FR-035**: El cliente DEBE poder registrar una contratación (descripción, fecha y precio acordado)
  que el prestador confirma o rechaza; DEBE poder marcarse como terminada o cancelada.

**Calificaciones**

- **FR-036**: Solo DEBE poder calificar quien tenga una contratación terminada; el cliente califica al
  prestador de 1 a 5 en calidad, precio justo, puntualidad/rapidez y trato, con comentario y hasta 5
  fotos opcionales; el prestador califica al cliente de 1 a 5 en puntualidad de pago, trato y claridad
  de lo solicitado.
- **FR-037**: Las calificaciones DEBEN ser doble ciega y publicarse cuando ambas partes califican o
  a los 14 días de terminada la contratación.
- **FR-038**: El prestador DEBE poder responder una vez a cada reseña recibida.
- **FR-039**: El perfil DEBE mostrar el promedio por eje y el número de reseñas.

**Reportes y moderación**

- **FR-040**: Los usuarios DEBEN poder reportar perfiles, mensajes y reseñas, y bloquear a otros
  usuarios.
- **FR-041**: El administrador DEBE poder ocultar contenido y suspender o reactivar cuentas con
  motivo obligatorio; el usuario suspendido DEBE poder enviar una aclaración.
- **FR-042**: Toda acción administrativa DEBE quedar en la bitácora.
- **FR-043**: Si un prestador es suspendido o elimina su cuenta, los clientes con acceso vigente DEBEN
  recibir un crédito.

**Privacidad y cuenta**

- **FR-044**: El sistema DEBE mostrar aviso de privacidad, términos y política de reembolsos, y
  registrar su aceptación con fecha y versión.
- **FR-045**: El usuario DEBE poder descargar sus datos y eliminar su cuenta desde la app; al eliminar,
  los documentos se borran y se conservan solo los registros que la ley obliga a guardar.

**Experiencia móvil**

- **FR-046**: La aplicación DEBE poder instalarse en la pantalla de inicio de Android y iPhone,
  funcionar en pantallas desde 360 px de ancho y permitir reintentar cargas interrumpidas.

### Key Entities *(include if feature involves data)*

- **Usuario**: persona con cuenta; correo o teléfono verificado, fecha de nacimiento, roles, estado
  (activo, suspendido, eliminado).
- **Perfil de prestador**: datos públicos y privados del prestador; oficios, experiencia, zona,
  disponibilidad, tarifas de referencia, estado de verificación, insignias, promedios.
- **Documento**: archivo privado de un prestador; tipo (INE frente, INE reverso, selfie, domicilio,
  antecedentes), estado, motivo, fecha de expedición o vencimiento, revisor.
- **Elemento de portafolio**: foto o video con descripción y estado de moderación.
- **Oficio / Categoría**: catálogo de servicios ofrecidos.
- **Acceso de contacto**: derecho de un cliente a contactar a un prestador; origen (individual o
  pase), estado, vigencia.
- **Pase mensual**: periodo de 30 días con contactos disponibles, renovación automática sí/no.
- **Pago**: cobro de un acceso o pase; monto, método, estado, referencia de la pasarela, comprobante.
- **Crédito**: contacto a favor del cliente con motivo (sin respuesta, suspensión, eliminación).
- **Conversación / Mensaje**: intercambio entre cliente y prestador ligado a un acceso.
- **Contratación**: acuerdo registrado; descripción, fecha, precio acordado, estado.
- **Calificación**: evaluación de una contratación por una de las partes; ejes, comentario, fotos,
  respuesta, estado de publicación.
- **Reporte**: queja sobre perfil, mensaje o reseña; estado y resolución.
- **Registro de bitácora**: acción administrativa o consulta de documento con autor y fecha.
- **Consentimiento**: aceptación de un documento legal con versión y fecha.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un prestador completa el alta con todos sus documentos en menos de 20 minutos desde el
  celular.
- **SC-002**: El 70% de los prestadores que inician el alta la envían a revisión.
- **SC-003**: El administrador revisa un perfil completo en menos de 5 minutos y el 90% de los
  perfiles se revisan en menos de 48 horas.
- **SC-004**: Un cliente encuentra un prestador de su zona y paga un contacto en menos de 3 minutos.
- **SC-005**: El 100% de los pagos confirmados desbloquean el acceso: los de tarjeta en menos de 1
  minuto y los de OXXO o SPEI en menos de 5 minutos después de su confirmación.
- **SC-006**: Cero casos de documentos o datos de contacto visibles para personas no autorizadas en las
  pruebas de seguridad.
- **SC-007**: Al menos el 80% de los prestadores contactados responden en menos de 48 horas.
- **SC-008**: Al menos el 40% de los contactos pagados registran una contratación y el 60% de las
  contrataciones terminadas reciben calificación.
- **SC-009**: Las quejas por cobros no reconocidos son menos del 1% de los pagos.
- **SC-010**: Durante el piloto, al menos 50 prestadores aprobados y 100 contactos pagados.

## Assumptions

- El piloto se lanza en una sola ciudad o zona metropolitana de México; la zona se define antes del
  lanzamiento.
- La verificación de identidad en el MVP es manual por un administrador; la verificación automática
  con un proveedor externo queda fuera del MVP.
- En el MVP ningún oficio exige la carta de no antecedentes; cualquier exigencia futura por oficio
  requiere validación legal.
- El cliente no comparte su teléfono automáticamente; el prestador lo ve solo si el cliente decide
  escribirlo en el chat.
- Los precios incluyen IVA; el responsable del proyecto factura las cuotas de la plataforma.
- La pasarela de pago soporta tarjeta, OXXO, SPEI y cobros recurrentes con tarjeta en modo de pruebas
  y producción.
- Los textos legales (aviso de privacidad, términos y política de reembolsos) los redacta o revisa un
  abogado; el sistema solo los muestra y registra su aceptación.
- Los registros de pagos se conservan el tiempo que exige la legislación fiscal aunque se elimine la
  cuenta.
- Las notificaciones se envían dentro de la app, por notificación push (si el usuario la permite) y por
  correo; SMS o WhatsApp quedan fuera del MVP salvo para el código de verificación.
- Fuera del MVP: apps en tiendas, mapa en tiempo real, verificación automática de identidad, planes
  destacados para prestadores y varias ciudades.
- Antes de manejar datos reales, todos los flujos se validan con un prototipo y datos ficticios.
