import { haceCuanto } from './relativa';

const ahora = new Date('2026-11-08T12:00:00Z');

describe('haceCuanto', () => {
  it('describe en español cuánto tiempo pasó', () => {
    expect(haceCuanto('2026-11-08T11:59:30Z', ahora)).toBe('hace un momento');
    expect(haceCuanto('2026-11-08T11:15:00Z', ahora)).toBe('hace 45 minutos');
    expect(haceCuanto('2026-11-08T09:00:00Z', ahora)).toBe('hace 3 horas');
    expect(haceCuanto('2026-11-07T12:00:00Z', ahora)).toBe('ayer');
    expect(haceCuanto('2026-11-05T12:00:00Z', ahora)).toBe('hace 3 días');
  });
});
