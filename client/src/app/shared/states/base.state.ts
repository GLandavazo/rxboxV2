import { Signal, WritableSignal, computed, signal } from '@angular/core';

/**
 * Store mínimo con signals. Un state por tema (app, usuario, una pantalla compleja);
 * se extiende y se registra como servicio `providedIn: 'root'` (o en el componente si su
 * vida debe ser la de la pantalla).
 *
 *   type MiStateType = { filtro: string; seleccion: Item | null };
 *   @Injectable({ providedIn: 'root' })
 *   export class MiState extends BaseStateService<MiStateType> {
 *     readonly filtro = this.select('filtro');          // campo, no getter: un solo computed
 *     constructor() { super({ filtro: '', seleccion: null }); }
 *   }
 */
export class BaseStateService<T extends object> {
    readonly state: WritableSignal<T>;

    constructor(protected readonly initialState: T) {
        this.state = signal(initialState);
    }

    /** Signal de solo lectura de una llave. Guárdalo en un campo; no lo llames en la plantilla. */
    select<K extends keyof T>(key: K): Signal<T[K]> {
        return computed(() => this.state()[key]);
    }

    set<K extends keyof T>(key: K, data: T[K]): void {
        this.state.update((actual) => ({ ...actual, [key]: data }));
    }

    setState(partialState: Partial<T>): void {
        this.state.update((actual) => ({ ...actual, ...partialState }));
    }

    resetState(): void {
        this.state.set(this.initialState);
    }
}
