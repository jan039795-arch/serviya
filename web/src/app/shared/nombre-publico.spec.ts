import { nombrePublico } from './nombre-publico';

describe('nombrePublico', () => {
  it('usa el nombre de pila y la inicial del primer apellido', () => {
    expect(nombrePublico('Juan Pérez López')).toBe('Juan P.');
    expect(nombrePublico('Ana Ruiz')).toBe('Ana R.');
  });

  it('con dos nombres toma la penúltima palabra como apellido', () => {
    expect(nombrePublico('María José Hernández López')).toBe('María H.');
  });

  it('limpia espacios y respeta acentos en la inicial', () => {
    expect(nombrePublico('  luis   órtiz  ')).toBe('luis Ó.');
  });

  it('con una sola palabra la devuelve tal cual', () => {
    expect(nombrePublico('Lupita')).toBe('Lupita');
    expect(nombrePublico('')).toBe('');
  });
});
