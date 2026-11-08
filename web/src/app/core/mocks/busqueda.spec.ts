import municipios from '../../../mocks/municipios.json';
import prestadores from '../../../mocks/prestadores.json';
import { Pagina, TarjetaPrestador } from '../api/modelos';
import { aPerfilPublico, aTarjeta, directorio } from './estado/directorio';
import { puntajeBayesiano } from './estado/ranking';
import { rutasBusqueda } from './handlers/busqueda';
import { coincidir, RespuestaSimulada } from './simulacion';

/** Campos que nunca deben salir en una respuesta pública (FR-015, FR-017, SC-006). */
const PRIVADOS = [
  'telefono',
  'telefonoFicticio',
  'whatsapp',
  'email',
  'correo',
  'direccion',
  'fechaNacimiento',
  'nombreCompleto',
  'documentos',
  'puntoBase',
  'datosPersonales',
];

function llaves(valor: unknown): string[] {
  if (Array.isArray(valor)) {
    return valor.flatMap(llaves);
  }
  if (valor && typeof valor === 'object') {
    return Object.entries(valor).flatMap(([llave, hijo]) => [llave, ...llaves(hijo)]);
  }
  return [];
}

function pedir(ruta: string, consulta = ''): RespuestaSimulada {
  for (const definicion of rutasBusqueda) {
    const parametros = coincidir(definicion.patron, ruta);
    if (parametros && definicion.metodo === 'GET') {
      return definicion.manejar({
        metodo: 'GET',
        ruta,
        parametros,
        consulta: new URLSearchParams(consulta),
        cuerpo: null,
      });
    }
  }
  throw new Error(`Sin ruta para ${ruta}`);
}

function nombres(consulta: string): string[] {
  const pagina = pedir('/prestadores', consulta).cuerpo as Pagina<TarjetaPrestador>;
  return pagina.items.map((t) => t.nombrePublico);
}

const [valleAlto, sanMiguel] = municipios;
const hector = prestadores.find((p) => p.nombrePublico === 'Héctor N.');
const ivan = prestadores.find((p) => p.nombrePublico === 'Iván C.');

beforeEach(() => localStorage.clear());

describe('puntajeBayesiano', () => {
  it('una sola reseña de 5 no supera a 20 reseñas de 4.8', () => {
    const unaPerfecta = puntajeBayesiano(5, 1, 4.5);
    const muchasAltas = puntajeBayesiano(4.8, 20, 4.5);

    expect(muchasAltas).toBeGreaterThan(unaPerfecta);
  });

  it('sin reseñas vale el promedio global', () => {
    expect(puntajeBayesiano(0, 0, 4.5)).toBe(4.5);
  });
});

describe('DTOs públicos', () => {
  it('la tarjeta y el perfil nunca incluyen datos privados', () => {
    for (const prestador of directorio()) {
      const publicas = [...llaves(aTarjeta(prestador)), ...llaves(aPerfilPublico(prestador))];

      expect(publicas.filter((llave) => PRIVADOS.includes(llave))).toEqual([]);
    }
  });

  it('ninguna respuesta de búsqueda contiene un teléfono', () => {
    const texto = JSON.stringify(pedir('/prestadores', 'tamano=50').cuerpo);

    expect(texto).not.toMatch(/\+52|\b55\s?\d{4}\s?\d{4}\b/);
  });
});

describe('búsqueda simulada', () => {
  it('devuelve solo aprobados del oficio que cubren el municipio', () => {
    const encontrados = nombres(`oficio=fontanero&municipio=${valleAlto.id}`);

    expect(encontrados).toContain('Juan P.');
    expect(encontrados).toContain('Jorge E.');
    expect(encontrados).not.toContain('Iván C.');
    expect(encontrados).not.toContain('Pedro S.');
  });

  it('ordena con el promedio bayesiano: una sola reseña perfecta no queda primero', () => {
    // Sin municipio no cuenta la cercanía; solo calificación y número de reseñas.
    const encontrados = nombres('oficio=fontanero');

    expect(encontrados[0]).toBe('Juan P.');
    expect(encontrados.indexOf('Pedro S.')).toBeGreaterThan(0);
  });

  it('la cercanía favorece a quien tiene su base en el municipio buscado', () => {
    expect(nombres(`oficio=fontanero&municipio=${sanMiguel.id}`)[0]).toBe('Pedro S.');
  });

  it('filtra por insignia, calificación mínima y tarifa máxima', () => {
    expect(nombres('oficio=fontanero&insignia=AntecedentesPresentados')).toEqual(['Juan P.']);
    expect(nombres('calificacionMin=4.8&tamano=50')).not.toContain('Carlos R.');
    expect(nombres('oficio=fontanero&tarifaMax=30000')).toEqual(['Pedro S.']);
  });

  it('un oficio que no existe no devuelve resultados', () => {
    expect(nombres('oficio=astronauta')).toEqual([]);
  });

  it('los perfiles en revisión o suspendidos responden 404 por enlace directo', () => {
    expect(pedir(`/prestadores/${hector?.id}`).status).toBe(404);
    expect(pedir(`/prestadores/${ivan?.id}`).status).toBe(404);
    expect(pedir(`/prestadores/${prestadores[0].id}`).status).toBe(200);
  });
});
