import { Component, computed, inject, input, resource, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CatalogosApi } from '../../../core/api/catalogos-api';
import {
  DecisionModeracion,
  DecisionPerfil,
  ElementoPortafolioPropio,
} from '../../../core/api/modelos';
import { calcularEdad } from '../../../shared/fechas/edad';
import { mensajeGeneral } from '../../../shared/formularios/errores';
import { MxnPipe } from '../../../shared/pipes/mxn';
import { Avisos } from '../../../shared/ui/avisos';
import { Boton } from '../../../shared/ui/boton';
import { Cargando } from '../../../shared/ui/cargando';
import { DIAS_SEMANA } from '../../alta-prestador/pasos';
import { DecisionConMotivo, DecisionTomada, OpcionDecision } from './decision-con-motivo';
import { DocumentoRevision } from './documento-revision';
import { RevisionesApi } from './revisiones-api';
import { valorDe } from '../../../shared/recursos';

const OBLIGATORIOS = ['IneFrente', 'IneReverso', 'Selfie', 'ComprobanteDomicilio'];

const OPCIONES_PORTAFOLIO: readonly OpcionDecision<DecisionModeracion>[] = [
  { decision: 'Aprobar', texto: 'Publicar', variante: 'primario', pideMotivo: false },
  { decision: 'Rechazar', texto: 'No publicar', variante: 'peligro', pideMotivo: true },
];

const MENSAJES_DECISION: Readonly<Record<DecisionPerfil, string>> = {
  Aprobar: 'Perfil aprobado y publicado.',
  RequiereCorreccion: 'Le pedimos al prestador que corrija los documentos señalados.',
  Rechazar: 'Perfil rechazado.',
};

/** Detalle de un perfil en revisión: datos, documentos, portafolio y decisión (FR-009 a FR-012). */
@Component({
  selector: 'app-detalle-revision',
  imports: [RouterLink, Boton, Cargando, DecisionConMotivo, DocumentoRevision, MxnPipe],
  templateUrl: './detalle-revision.html',
  styles: `
    .datos {
      display: grid;
      grid-template-columns: max-content 1fr;
      gap: var(--espacio-1) var(--espacio-3);
      margin: 0 0 var(--espacio-4);
    }
    .datos dt {
      font-weight: 600;
    }
    .datos dd {
      margin: 0;
    }
    section {
      margin-bottom: var(--espacio-6);
    }
    .portafolio {
      display: grid;
      gap: var(--espacio-3);
      padding: 0;
      list-style: none;
    }
    .portafolio li {
      display: grid;
      gap: var(--espacio-2);
      padding: var(--espacio-3);
      border: 1px solid #d5dbe1;
      border-radius: var(--radio);
    }
    .portafolio img {
      width: 100%;
      border-radius: var(--radio);
    }
    .decision-final {
      padding: var(--espacio-4);
      border: 2px solid var(--color-primario);
      border-radius: var(--radio);
    }
  `,
})
export class DetalleRevision {
  private readonly api = inject(RevisionesApi);
  private readonly catalogos = inject(CatalogosApi);
  private readonly router = inject(Router);
  private readonly avisos = inject(Avisos);

  /** Parámetro de la ruta `revisiones/:prestadorId`. */
  readonly prestadorId = input.required<string>();

  protected readonly perfil = resource({
    params: () => ({ id: this.prestadorId() }),
    loader: ({ params }) => this.api.perfil(params.id),
  });
  private readonly oficios = resource({ loader: () => this.catalogos.oficios() });
  private readonly municipios = resource({ loader: () => this.catalogos.municipios() });

  protected readonly opcionesPortafolio = OPCIONES_PORTAFOLIO;
  protected readonly dias = DIAS_SEMANA;
  protected readonly decidiendo = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly editable = computed(() => valorDe(this.perfil)?.estado === 'EnRevision');
  protected readonly edad = computed(() => {
    const fecha = valorDe(this.perfil)?.datosPersonales?.fechaNacimiento;
    return fecha ? calcularEdad(fecha) : null;
  });
  protected readonly nombresOficios = computed(() => {
    const catalogo = valorDe(this.oficios) ?? [];
    return new Map(catalogo.map((o) => [o.id, o.nombre]));
  });
  protected readonly nombresCobertura = computed(() => {
    const catalogo = valorDe(this.municipios) ?? [];
    const ids = valorDe(this.perfil)?.cobertura ?? [];
    return ids.map((id) => catalogo.find((m) => m.id === id)?.nombre ?? id).join(', ');
  });
  protected readonly opcionesPerfil = computed<readonly OpcionDecision<DecisionPerfil>[]>(() => {
    const documentos = valorDe(this.perfil)?.documentos ?? [];
    const aprobados = OBLIGATORIOS.every(
      (tipo) => documentos.find((d) => d.tipo === tipo)?.estado === 'Aprobado',
    );
    const conCorreccion = documentos.some((d) => d.estado === 'RequiereCorreccion');
    return [
      {
        decision: 'Aprobar',
        texto: 'Aprobar y publicar',
        variante: 'primario',
        pideMotivo: false,
        deshabilitada: !aprobados,
      },
      {
        decision: 'RequiereCorreccion',
        texto: 'Pedir correcciones',
        variante: 'secundario',
        pideMotivo: true,
        deshabilitada: !conCorreccion,
      },
      { decision: 'Rechazar', texto: 'Rechazar perfil', variante: 'peligro', pideMotivo: true },
    ];
  });

  protected nombreDia(dia: number): string {
    return this.dias.find((d) => d.dia === dia)?.nombre ?? '';
  }

  protected recargar(): void {
    this.perfil.reload();
  }

  protected async decidirPortafolio(
    elemento: ElementoPortafolioPropio,
    { decision, motivo }: DecisionTomada<DecisionModeracion>,
  ): Promise<void> {
    await this.ejecutar(() => this.api.decidirPortafolio(elemento.id, decision, motivo));
    this.perfil.reload();
  }

  protected async decidirPerfil({
    decision,
    motivo,
  }: DecisionTomada<DecisionPerfil>): Promise<void> {
    const hecho = await this.ejecutar(() =>
      this.api.decidirPerfil(this.prestadorId(), decision, motivo),
    );
    if (hecho) {
      this.avisos.mostrar('exito', MENSAJES_DECISION[decision]);
      await this.router.navigate(['/admin', 'revisiones']);
    }
  }

  private async ejecutar(accion: () => Promise<void>): Promise<boolean> {
    this.error.set(null);
    this.decidiendo.set(true);
    try {
      await accion();
      return true;
    } catch (error: unknown) {
      this.error.set(mensajeGeneral(error));
      return false;
    } finally {
      this.decidiendo.set(false);
    }
  }
}
