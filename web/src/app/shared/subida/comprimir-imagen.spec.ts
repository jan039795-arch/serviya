import {
  calcularDimensiones,
  DURACION_MAXIMA_VIDEO_S,
  TAMANO_MAXIMO_VIDEO_BYTES,
  validarVideo,
} from './comprimir-imagen';

describe('calcularDimensiones', () => {
  it('reduce una foto horizontal a 1600 px de ancho', () => {
    expect(calcularDimensiones(4000, 3000)).toEqual({ ancho: 1600, alto: 1200 });
  });

  it('reduce una foto vertical a 1600 px de alto', () => {
    expect(calcularDimensiones(3024, 4032)).toEqual({ ancho: 1200, alto: 1600 });
  });

  it('no amplía una foto chica', () => {
    expect(calcularDimensiones(800, 600)).toEqual({ ancho: 800, alto: 600 });
  });

  it('respeta el límite exacto', () => {
    expect(calcularDimensiones(1600, 1600)).toEqual({ ancho: 1600, alto: 1600 });
  });
});

describe('validarVideo', () => {
  it('acepta un video de 90 s y menos de 60 MB', () => {
    expect(validarVideo(DURACION_MAXIMA_VIDEO_S, TAMANO_MAXIMO_VIDEO_BYTES)).toBeNull();
  });

  it('rechaza un video de más de 90 s', () => {
    expect(validarVideo(90.5, 1000)).toContain('90 segundos');
  });

  it('rechaza un video de más de 60 MB', () => {
    expect(validarVideo(30, TAMANO_MAXIMO_VIDEO_BYTES + 1)).toContain('60 MB');
  });

  it('rechaza un video sin duración legible', () => {
    expect(validarVideo(Number.NaN, 1000)).toContain('duración');
  });
});
