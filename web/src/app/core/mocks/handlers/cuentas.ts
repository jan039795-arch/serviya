import { Rol, SolicitudRegistro } from '../../api/modelos';
import { asegurarPerfil } from '../estado/perfiles';
import {
  aSesion,
  buscarPorCorreo,
  CODIGO_SIMULADO,
  cerrarSesion,
  correoDePrueba,
  esPerfilDePrueba,
  guardarUsuario,
  iniciarSesion,
  nuevoId,
  usuarioActual,
} from '../estado/usuarios';
import { creado, ok, problema, ruta, RutaSimulada, sinContenido } from '../simulacion';

const LARGO_MINIMO_CONTRASENA = 8;

function texto(cuerpo: unknown, campo: string): string {
  const valor = (cuerpo as Record<string, unknown> | null)?.[campo];
  return typeof valor === 'string' ? valor.trim() : '';
}

function validarRegistro(cuerpo: Partial<SolicitudRegistro>): Record<string, string[]> {
  const errores: Record<string, string[]> = {};
  if (!/^\S+@\S+\.\S+$/.test(cuerpo.email ?? '')) {
    errores['email'] = ['Escribe un correo válido.'];
  }
  if ((cuerpo.password ?? '').length < LARGO_MINIMO_CONTRASENA) {
    errores['password'] = ['La contraseña debe tener al menos 8 caracteres.'];
  }
  const nombre = (cuerpo.nombreCompleto ?? '').trim();
  if (nombre.length < 3 || nombre.length > 120) {
    errores['nombreCompleto'] = ['Escribe tu nombre completo (de 3 a 120 caracteres).'];
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(cuerpo.fechaNacimiento ?? '')) {
    errores['fechaNacimiento'] = ['Escribe tu fecha de nacimiento.'];
  }
  if ((cuerpo.aceptaciones ?? []).length < 2) {
    errores['aceptaciones'] = ['Acepta el aviso de privacidad y los términos.'];
  }
  return errores;
}

/** Rutas de Cuentas del prototipo (contracts/api.md, sección Cuentas). */
export const rutasCuentas: readonly RutaSimulada[] = [
  ruta('cuentas', 'POST', '/cuentas/registro', ({ cuerpo }) => {
    const solicitud = (cuerpo ?? {}) as Partial<SolicitudRegistro>;
    const errores = validarRegistro(solicitud);
    if (Object.keys(errores).length > 0) {
      return problema(400, 'Revisa los datos marcados.', { errors: errores });
    }
    if (buscarPorCorreo(solicitud.email ?? '')) {
      return problema(409, 'Ya existe una cuenta con ese correo.', {
        detail: 'Ya existe una cuenta con ese correo. Entra o recupera tu contraseña.',
      });
    }
    const roles: Rol[] = solicitud.rol === 'Prestador' ? ['Cliente', 'Prestador'] : ['Cliente'];
    const id = nuevoId();
    guardarUsuario({
      id,
      email: (solicitud.email ?? '').trim().toLowerCase(),
      nombreCompleto: (solicitud.nombreCompleto ?? '').trim(),
      fechaNacimiento: solicitud.fechaNacimiento ?? '',
      roles,
      verificado: false,
    });
    if (roles.includes('Prestador')) {
      asegurarPerfil(id, nuevoId());
    }
    return creado();
  }),

  ruta('cuentas', 'POST', '/cuentas/verificar-correo', ({ cuerpo }) => {
    const usuario = buscarPorCorreo(texto(cuerpo, 'email'));
    if (!usuario || texto(cuerpo, 'codigo') !== CODIGO_SIMULADO) {
      return problema(400, 'El código no es correcto o ya venció.', {
        detail: 'El código no es correcto o ya venció. Revisa tu correo o pide uno nuevo.',
      });
    }
    guardarUsuario({ ...usuario, verificado: true });
    iniciarSesion(usuario.email);
    return sinContenido();
  }),

  ruta('cuentas', 'POST', '/cuentas/reenviar-codigo', () => sinContenido()),

  ruta('cuentas', 'POST', '/cuentas/login', ({ cuerpo }) => {
    const usuario = buscarPorCorreo(texto(cuerpo, 'email'));
    const contrasena = (cuerpo as { password?: unknown } | null)?.password;
    if (!usuario || typeof contrasena !== 'string' || contrasena.length < LARGO_MINIMO_CONTRASENA) {
      return problema(400, 'Correo o contraseña incorrectos.', {
        detail: 'Correo o contraseña incorrectos.',
      });
    }
    if (!usuario.verificado) {
      return problema(403, 'Verifica tu correo.', {
        type: '/errores/correo-sin-verificar',
        detail: 'Primero verifica tu correo con el código que te enviamos.',
      });
    }
    iniciarSesion(usuario.email);
    return ok(aSesion(usuario));
  }),

  ruta('cuentas', 'POST', '/cuentas/logout', () => {
    cerrarSesion();
    return sinContenido();
  }),

  // Siempre 204, exista o no la cuenta, para no revelar qué correos están registrados.
  ruta('cuentas', 'POST', '/cuentas/recuperar', () => sinContenido()),

  ruta('cuentas', 'POST', '/cuentas/restablecer', ({ cuerpo }) => {
    const usuario = buscarPorCorreo(texto(cuerpo, 'email'));
    if (!usuario || texto(cuerpo, 'codigo') !== CODIGO_SIMULADO) {
      return problema(400, 'El código no es correcto o ya venció.', {
        detail: 'El código no es correcto o ya venció. Pide uno nuevo.',
      });
    }
    if (texto(cuerpo, 'nuevaPassword').length < LARGO_MINIMO_CONTRASENA) {
      return problema(400, 'Revisa los datos marcados.', {
        errors: { nuevaPassword: ['La contraseña debe tener al menos 8 caracteres.'] },
      });
    }
    return sinContenido();
  }),

  // La API real emite aquí la cookie XSRF-TOKEN; el prototipo no la necesita.
  ruta('cuentas', 'GET', '/cuentas/xsrf', () => sinContenido()),

  ruta('cuentas', 'GET', '/cuentas/yo', () => {
    const usuario = usuarioActual();
    return usuario ? ok(aSesion(usuario)) : problema(401, 'No has iniciado sesión.');
  }),

  ruta('cuentas', 'POST', '/cuentas/yo/roles/prestador', () => {
    const usuario = usuarioActual();
    if (!usuario) {
      return problema(401, 'No has iniciado sesión.');
    }
    if (!usuario.roles.includes('Prestador')) {
      guardarUsuario({ ...usuario, roles: [...usuario.roles, 'Prestador'] });
    }
    asegurarPerfil(usuario.id, nuevoId());
    return sinContenido();
  }),

  // Solo del prototipo: cambia el perfil desde el selector de rol.
  ruta('cuentas', 'POST', '/dev/simular-sesion', ({ cuerpo }) => {
    const perfil = (cuerpo as { perfil?: unknown } | null)?.perfil;
    if (perfil === 'visitante') {
      cerrarSesion();
      return sinContenido();
    }
    if (!esPerfilDePrueba(perfil)) {
      return problema(400, 'Perfil de prueba desconocido.');
    }
    iniciarSesion(correoDePrueba(perfil));
    return sinContenido();
  }),
];
