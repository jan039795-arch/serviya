import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CatalogosApi } from '../../core/api/catalogos-api';
import { GuardadoPaso, Municipio, Oficio, PerfilPropio } from '../../core/api/modelos';
import { mensajeGeneral } from '../../shared/formularios/errores';
import { AltaApi } from './alta-api';
import { TOTAL_PASOS } from './pasos';

const ESTADOS_EDITABLES = new Set(['Borrador', 'RequiereCorreccion']);

/**
 * Estado compartido del alta (perfil y catálogos). Se provee en la ruta `/alta`, así que vive
 * mientras el prestador está en el alta y se descarta al salir.
 */
@Injectable()
export class AltaEstado {
  private readonly api = inject(AltaApi);
  private readonly catalogos = inject(CatalogosApi);
  private readonly router = inject(Router);

  readonly perfil = signal<PerfilPropio | null>(null);
  readonly oficios = signal<readonly Oficio[]>([]);
  readonly municipios = signal<readonly Municipio[]>([]);
  readonly errorCarga = signal<string | null>(null);
  readonly errorGuardado = signal<string | null>(null);
  readonly guardando = signal(false);

  readonly editable = computed(() => ESTADOS_EDITABLES.has(this.perfil()?.estado ?? ''));
  /** Paso donde retomar: el siguiente al último guardado (US1, escenario 2). */
  readonly pasoParaRetomar = computed(() =>
    Math.min((this.perfil()?.pasoAlta ?? 0) + 1, TOTAL_PASOS),
  );

  async cargar(): Promise<void> {
    this.errorCarga.set(null);
    try {
      const [perfil, oficios, municipios] = await Promise.all([
        this.api.perfil(),
        this.catalogos.oficios(),
        this.catalogos.municipios(),
      ]);
      this.oficios.set(oficios);
      this.municipios.set(municipios);
      this.perfil.set(perfil);
    } catch (error: unknown) {
      this.errorCarga.set(mensajeGeneral(error));
    }
  }

  /** Guarda el paso y avanza al siguiente. Devuelve `false` si no se pudo guardar. */
  async guardarYAvanzar(cambios: GuardadoPaso): Promise<boolean> {
    const guardado = await this.guardar(cambios);
    if (guardado) {
      await this.irAPaso(Math.min(cambios.paso + 1, TOTAL_PASOS));
    }
    return guardado;
  }

  async guardar(cambios: GuardadoPaso): Promise<boolean> {
    this.errorGuardado.set(null);
    this.guardando.set(true);
    try {
      this.perfil.set(await this.api.guardar(cambios));
      return true;
    } catch (error: unknown) {
      this.errorGuardado.set(mensajeGeneral(error));
      return false;
    } finally {
      this.guardando.set(false);
    }
  }

  /** Aplica un cambio local tras una operación que no devuelve el perfil completo. */
  actualizar(cambio: (perfil: PerfilPropio) => PerfilPropio): void {
    this.perfil.update((perfil) => (perfil ? cambio(perfil) : perfil));
  }

  irAPaso(numero: number): Promise<boolean> {
    return this.router.navigate(['/alta', String(numero)]);
  }

  irAEstado(): Promise<boolean> {
    return this.router.navigate(['/alta', 'estado']);
  }
}
