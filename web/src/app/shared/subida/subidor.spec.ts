import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, toArray } from 'rxjs';
import { ACCIONES_PROTOTIPO, AccionesPrototipo } from '../../core/prototipo/acciones';
import { esUrlSegura, Subidor } from './subidor';

const archivo = new Blob(['ficticio'], { type: 'image/png' });

describe('Subidor', () => {
  it('sin prototipo rechaza URLs simuladas y que no sean https', async () => {
    const subidor = TestBed.inject(Subidor);

    await expect(
      firstValueFrom(subidor.subir('simulado:abc', archivo, 'image/png')),
    ).rejects.toThrow();
    await expect(
      firstValueFrom(subidor.subir('http://r2.ejemplo/abc', archivo, 'image/png')),
    ).rejects.toThrow();
  });

  it('en el prototipo simula la subida sin enviar el archivo', async () => {
    const acciones = { simularSubida: () => of(50, 100) } as unknown as AccionesPrototipo;
    TestBed.configureTestingModule({
      providers: [{ provide: ACCIONES_PROTOTIPO, useValue: acciones }],
    });

    const avance = await firstValueFrom(
      TestBed.inject(Subidor).subir('simulado:abc', archivo, 'image/png').pipe(toArray()),
    );

    expect(avance).toEqual([50, 100]);
  });

  it('solo considera seguras las URLs https', () => {
    expect(esUrlSegura('https://cuenta.r2.cloudflarestorage.com/obj')).toBe(true);
    expect(esUrlSegura('http://cuenta.r2.cloudflarestorage.com/obj')).toBe(false);
    expect(esUrlSegura('javascript:alert(1)')).toBe(false);
  });
});
