import { WritableSignal } from '@angular/core';

/**
 * Arreglo fijo de N elementos vacíos para alimentar [value] de un p-table mientras carga.
 * Guardarlo en una propiedad del componente; NO llamarlo desde la plantilla
 * (una función en la plantilla crea un arreglo nuevo en cada ciclo y p-table re-renderiza).
 */
export const filasSkeleton = (n = 5): undefined[] => Array.from({ length: n });

/** Enciende la bandera mientras dura la acción y la apaga siempre, aunque falle. */
export async function conSkeleton<T>(cargando: WritableSignal<boolean>, accion: () => Promise<T>): Promise<T> {
    cargando.set(true);
    try {
        return await accion();
    } finally {
        cargando.set(false);
    }
}
