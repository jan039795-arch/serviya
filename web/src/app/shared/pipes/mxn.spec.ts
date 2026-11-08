import { formatearMxn, MxnPipe } from './mxn';

describe('mxn', () => {
  it('convierte los precios de la plataforma', () => {
    expect(formatearMxn(3900)).toBe('$39.00 MXN');
    expect(formatearMxn(12900)).toBe('$129.00 MXN');
  });

  it('separa los miles y conserva los centavos', () => {
    expect(formatearMxn(150000)).toBe('$1,500.00 MXN');
    expect(formatearMxn(9050)).toBe('$90.50 MXN');
  });

  it('devuelve texto vacío sin monto', () => {
    const pipe = new MxnPipe();

    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
    expect(pipe.transform(Number.NaN)).toBe('');
  });
});
