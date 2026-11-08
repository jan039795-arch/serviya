import { calcularEdad, diasDesde, esMayorDeEdad, hoyIso } from './edad';

const hoy = new Date(2026, 10, 8); // 8 de noviembre de 2026

describe('calcularEdad', () => {
  it('cuenta los años cumplidos', () => {
    expect(calcularEdad('1990-04-12', hoy)).toBe(36);
  });

  it('no suma el año si aún no es su cumpleaños', () => {
    expect(calcularEdad('2008-11-09', hoy)).toBe(17);
    expect(calcularEdad('2008-11-08', hoy)).toBe(18);
  });

  it('rechaza fechas inválidas', () => {
    expect(calcularEdad('2001-02-30', hoy)).toBeNull();
    expect(calcularEdad('08/11/2000', hoy)).toBeNull();
  });
});

describe('esMayorDeEdad', () => {
  it('exige 18 años cumplidos', () => {
    expect(esMayorDeEdad('2008-11-08', hoy)).toBe(true);
    expect(esMayorDeEdad('2008-11-09', hoy)).toBe(false);
    expect(esMayorDeEdad('', hoy)).toBe(false);
  });
});

describe('fechas auxiliares', () => {
  it('da la fecha de hoy y los días transcurridos', () => {
    expect(hoyIso(hoy)).toBe('2026-11-08');
    expect(diasDesde('2026-08-10', hoy)).toBe(90);
    expect(diasDesde('2026-11-10', hoy)).toBe(-2);
  });
});
