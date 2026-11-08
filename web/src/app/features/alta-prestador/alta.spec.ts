import { PerfilPropio } from '../../core/api/modelos';
import { faltantesParaRevision } from '../../core/mocks/handlers/prestadores';
import { perfilNuevo } from '../../core/mocks/estado/perfiles';
import { digitosTelefono } from './pasos/datos-personales';
import { puntosDeRevision } from './pasos/revision-envio';

const completo: PerfilPropio = {
  ...perfilNuevo('p1'),
  datosPersonales: {
    nombreCompleto: 'Pablo Ficticio Ruiz',
    fechaNacimiento: '1988-02-03',
    telefono: '+525512340000',
    direccion: 'Calle Inventada 1',
    coloniaBase: 'Centro',
  },
  oficios: [
    {
      oficioId: 'o1',
      aniosExperiencia: 7,
      tarifaMinCentavos: 30000,
      tarifaMaxCentavos: 60000,
      unidadTarifa: 'Servicio',
    },
  ],
  cobertura: ['m1'],
  consentimientoSensibleVersion: 'v1',
  documentos: (['IneFrente', 'IneReverso', 'Selfie', 'ComprobanteDomicilio'] as const).map(
    (tipo) => ({ id: tipo, tipo, estado: 'Cargado' as const }),
  ),
};

describe('faltantesParaRevision (simulación de la API)', () => {
  it('no reporta faltantes con un perfil completo', () => {
    expect(faltantesParaRevision(completo)).toEqual([]);
  });

  it('reporta los documentos obligatorios que faltan o deben corregirse', () => {
    const perfil: PerfilPropio = {
      ...completo,
      documentos: [
        { id: 'd1', tipo: 'IneFrente', estado: 'RequiereCorreccion', motivo: 'Borrosa' },
      ],
    };

    expect(faltantesParaRevision(perfil)).toEqual([
      'INE (frente)',
      'INE (reverso)',
      'Selfie',
      'Comprobante de domicilio',
    ]);
  });

  it('pide tarifas y consentimiento', () => {
    const perfil: PerfilPropio = {
      ...completo,
      oficios: [{ ...completo.oficios[0], tarifaMinCentavos: 0 }],
      consentimientoSensibleVersion: undefined,
    };

    expect(faltantesParaRevision(perfil)).toEqual([
      'Tarifas de referencia de cada oficio',
      'Consentimiento para datos sensibles',
    ]);
  });
});

describe('puntosDeRevision', () => {
  it('marca como listos los obligatorios de un perfil completo', () => {
    const pendientes = puntosDeRevision(completo).filter((p) => !p.listo && !p.opcional);

    expect(pendientes).toEqual([]);
  });

  it('la carta de antecedentes es opcional', () => {
    const carta = puntosDeRevision(completo).find((p) => p.texto.startsWith('Carta'));

    expect(carta).toMatchObject({ listo: false, opcional: true, paso: 8 });
  });
});

describe('digitosTelefono', () => {
  it('deja solo los 10 dígitos', () => {
    expect(digitosTelefono('55 1234-5678')).toBe('5512345678');
    expect(digitosTelefono('+52 55 1234 5678')).toBe('5512345678');
    expect(digitosTelefono('(55) 1234 567')).toBe('551234567');
  });
});
