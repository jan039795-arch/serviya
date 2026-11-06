<!--
Sync Impact Report
- Versión: 1.0.0 → 1.0.1 (PATCH: "Angular 20" pasa a "Angular estable vigente, hoy 22"; sin cambio
  de principios)
- Historial: (plantilla) → 1.0.0 creación inicial
- Principios definidos (nuevos):
  I. Privacidad y datos sensibles primero
  II. Intermediario solo de contacto
  III. Confianza verificable y sin discriminación
  IV. Mobile-first y accesible
  V. Simplicidad para un solo desarrollador
  VI. Pruebas en lo crítico
- Secciones añadidas: Restricciones técnicas y legales; Flujo de desarrollo
- Secciones eliminadas: ninguna
- Plantillas: plan/spec/tasks leen la constitución en tiempo de ejecución; sin cambios.
- TODO diferidos: ninguno.
-->

# Constitución de ServiYa

## Core Principles

### I. Privacidad y datos sensibles primero

- La INE, la selfie, el domicilio completo y la carta de no antecedentes penales DEBEN guardarse
  en almacenamiento privado; NUNCA se exponen en perfiles públicos, URLs públicas ni respuestas
  de API para roles distintos de administrador.
- El acceso a documentos DEBE hacerse mediante enlaces temporales firmados y quedar registrado
  en una bitácora inmutable (quién, qué documento, cuándo, para qué acción).
- En público el prestador solo muestra nombre de pila e inicial, colonia/zona, oficios,
  portafolio moderado, insignias y calificaciones.
- El tratamiento DEBE cumplir la LFPDPPP vigente: aviso de privacidad, consentimiento expreso
  para datos sensibles y biométricos, y ejercicio de derechos ARCO (incluida la eliminación
  de cuenta desde la propia app).
- Los datos personales NUNCA viajan en parámetros de URL ni en logs.

**Razón**: la plataforma custodia identificaciones de personas que entran a hogares; una fuga
destruiría la confianza y genera responsabilidad legal.

### II. Intermediario solo de contacto

- La plataforma NO cobra comisión sobre el trabajo, NO recibe ni retiene el pago del servicio y
  NO fija precios; el cliente y el prestador acuerdan y pagan directamente.
- Solo se cobra por acceso al contacto (contacto individual y pase/suscripción mensual) y, en su
  caso, por administración del perfil del prestador.
- Cada cobro DEBE mostrar antes de pagar: qué incluye, vigencia, regla de crédito/devolución y el
  aviso "pagas por acceso y contacto; no garantiza la contratación".
- Toda renovación automática DEBE ser opcional, avisarse con al menos 3 días de anticipación y
  poder cancelarse en un clic desde la cuenta.
- Los datos de contacto del prestador permanecen ocultos hasta confirmar el pago; el chat DEBE
  ocultar teléfonos, correos y enlaces de mensajería antes de ese momento.

**Razón**: mantiene un modelo legal y operativo simple y evita las quejas típicas por cobros
recurrentes inesperados.

### III. Confianza verificable y sin discriminación

- Ningún perfil de prestador se publica sin aprobación de un administrador humano; cada
  aprobación, rechazo o solicitud de corrección DEBE registrar motivo y autor.
- Solo puede calificar al prestador un cliente que pagó su contacto y registró la contratación;
  el prestador también califica al cliente; las calificaciones son doble ciega.
- Ejes de calificación al prestador: calidad, precio justo, puntualidad/rapidez y trato.
- La carta de no antecedentes penales es voluntaria y otorga una insignia; solo puede exigirse en
  oficios específicos con validación legal documentada.
- Toda suspensión DEBE registrar motivo y permitir al afectado presentar aclaración.
- Prestadores y clientes DEBEN ser mayores de 18 años.

**Razón**: la reputación solo vale si no puede fabricarse, y las reglas de verificación no
deben excluir injustamente a nadie.

### IV. Mobile-first y accesible

- Toda pantalla se diseña primero para celular (ancho mínimo 360 px) y DEBE funcionar en Chrome
  Android y Safari iOS recientes; la app es instalable como PWA.
- Los textos usan lenguaje simple en español de México, sin tecnicismos.
- Los flujos largos (alta de prestador, carga de documentos) DEBEN poder interrumpirse y
  retomarse sin perder lo capturado.
- Las imágenes y videos se comprimen antes de subirse y las cargas se pueden reintentar.
- Se cumplen pautas básicas de accesibilidad (contraste, etiquetas, tamaño táctil ≥ 44 px).

**Razón**: muchos prestadores y clientes solo usan celular, con datos móviles limitados y poca
experiencia digital.

### V. Simplicidad para un solo desarrollador

- Arquitectura: monolito modular. Frontend Angular estable vigente, hoy 22 (PWA + SSR para perfiles
  públicos), backend
  ASP.NET Core 10 Web API con EF Core y SignalR, base de datos PostgreSQL.
- Módulos del backend con límites claros: Cuentas, Prestadores, Documentos, Búsqueda, Pagos,
  Chat, Contrataciones, Calificaciones, Reportes y Administración.
- Se PROHÍBEN microservicios, colas de mensajes dedicadas u orquestadores mientras no exista una
  necesidad medida; cada dependencia nueva DEBE justificarse en el plan.
- Se prefieren capacidades incluidas en .NET y Angular sobre librerías de terceros.

**Razón**: una sola persona debe poder entender, desplegar y depurar todo el sistema.

### VI. Pruebas en lo crítico

- Pagos (incluidos webhooks y pagos pendientes de OXXO/SPEI), permisos por rol, desbloqueo de
  contacto y acceso a documentos DEBEN tener pruebas automatizadas antes de llegar a producción.
- El ocultamiento de datos de contacto en chat y perfiles DEBE probarse con casos de evasión.
- Antes de manejar documentos o dinero reales, cada flujo se valida con un prototipo y datos
  ficticios; las pruebas NUNCA usan documentos reales.
- Pagos se prueban con el modo de pruebas de Mercado Pago.

**Razón**: los errores en cobros y en exposición de datos son los más costosos y difíciles de
revertir.

## Restricciones técnicas y legales

- Pasarela de pago: Mercado Pago (tarjeta, OXXO, SPEI); los pagos asíncronos manejan el estado
  "pendiente" y el desbloqueo se hace solo al confirmar por webhook verificado.
- Almacenamiento de archivos: compatible con S3 (Cloudflare R2) en buckets privados.
- Secretos y llaves fuera del repositorio (variables de entorno o gestor de secretos).
- Comunicación solo por HTTPS; contraseñas con el hash de ASP.NET Identity.
- Respaldos automáticos diarios de la base de datos.
- Textos legales (aviso de privacidad, términos, política de reembolsos) revisados por un abogado
  antes del lanzamiento público.
- Documentación del proyecto en español.

## Flujo de desarrollo

- Cada funcionalidad sigue Spec Kit: especificación → plan → tareas → implementación.
- Entregas pequeñas y utilizables; cada etapa termina con algo probable en un celular.
- Orden de construcción: prototipo con datos ficticios → cuentas → documentos y administración →
  búsqueda → pagos → chat y contrataciones → calificaciones y reportes → legal → piloto.
- Ningún cambio se integra si fallan las pruebas de los módulos críticos (Principio VI).

## Governance

- Esta constitución prevalece sobre cualquier otra práctica del proyecto; los planes DEBEN incluir
  una verificación de cumplimiento y justificar cualquier excepción.
- Las enmiendas se documentan en este archivo con su Sync Impact Report y la decisión del
  responsable del proyecto.
- Versionado semántico: MAJOR al eliminar o redefinir principios; MINOR al agregar principios o
  ampliar guías; PATCH en aclaraciones y redacción.
- Se revisa el cumplimiento al cerrar cada etapa y antes del lanzamiento del piloto.

**Version**: 1.0.1 | **Ratified**: 2026-11-05 | **Last Amended**: 2026-11-05
