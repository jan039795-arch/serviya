import sesiones from '../../../../mocks/sesiones.json';
import { esMayorDeEdad } from '../../../shared/fechas/edad';
import { Rol, Sesion } from '../../api/modelos';
import { borrarLocal, guardarLocal, leerLocal } from '../almacen-local';

/**
 * Usuarios del prototipo: los de prueba de `sesiones.json` más los que se registran.
 * Nunca se guardan contraseñas: el prototipo acepta cualquiera de 8 caracteres o más.
 */
export interface UsuarioSimulado {
  readonly id: string;
  readonly email: string;
  readonly nombreCompleto: string;
  readonly fechaNacimiento: string;
  readonly roles: readonly Rol[];
  readonly verificado: boolean;
}

export type PerfilDePrueba = keyof typeof sesiones;

/** Código fijo del prototipo para verificar el correo y recuperar la contraseña. */
export const CODIGO_SIMULADO = '123456';

const CLAVE_USUARIOS = 'usuarios';
const CLAVE_SESION = 'sesion';

const DE_PRUEBA: readonly UsuarioSimulado[] = Object.values(sesiones).map((sesion) => ({
  id: sesion.id,
  email: sesion.email,
  nombreCompleto: sesion.nombreCompleto,
  fechaNacimiento: '1990-01-15',
  roles: sesion.roles as Rol[],
  verificado: true,
}));

export function esPerfilDePrueba(valor: unknown): valor is PerfilDePrueba {
  return typeof valor === 'string' && Object.hasOwn(sesiones, valor);
}

export function correoDePrueba(perfil: PerfilDePrueba): string {
  return sesiones[perfil].email;
}

function registrados(): readonly UsuarioSimulado[] {
  return leerLocal<UsuarioSimulado[]>(CLAVE_USUARIOS) ?? [];
}

export function todosLosUsuarios(): readonly UsuarioSimulado[] {
  const propios = registrados();
  const correosPropios = new Set(propios.map((u) => u.email));
  return [...DE_PRUEBA.filter((u) => !correosPropios.has(u.email)), ...propios];
}

export function buscarPorCorreo(email: string): UsuarioSimulado | undefined {
  const normalizado = email.trim().toLowerCase();
  return todosLosUsuarios().find((u) => u.email.toLowerCase() === normalizado);
}

export function buscarPorId(id: string): UsuarioSimulado | undefined {
  return todosLosUsuarios().find((u) => u.id === id);
}

/** Agrega o reemplaza un usuario registrado (o una copia modificada de uno de prueba). */
export function guardarUsuario(usuario: UsuarioSimulado): void {
  const otros = registrados().filter((u) => u.id !== usuario.id);
  guardarLocal(CLAVE_USUARIOS, [...otros, usuario]);
}

export function aSesion(usuario: UsuarioSimulado): Sesion {
  return {
    id: usuario.id,
    nombreCompleto: usuario.nombreCompleto,
    email: usuario.email,
    roles: usuario.roles,
    estado: 'Activo',
    esMayorDeEdad: esMayorDeEdad(usuario.fechaNacimiento),
  };
}

export function usuarioActual(): UsuarioSimulado | null {
  const email = leerLocal<string>(CLAVE_SESION);
  return email ? (buscarPorCorreo(email) ?? null) : null;
}

export function iniciarSesion(email: string): void {
  guardarLocal(CLAVE_SESION, email.trim().toLowerCase());
}

export function cerrarSesion(): void {
  borrarLocal(CLAVE_SESION);
}

export function nuevoId(): string {
  return (
    globalThis.crypto?.randomUUID?.() ?? `id-${Date.now()}-${Math.random().toString(16).slice(2)}`
  );
}
