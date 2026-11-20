import { Injectable, computed, signal } from '@angular/core';
import { BaseStateService } from './base.state';

type AppStateType = {
    pageTitle: string;
};

/** Estado global de la UI: título de la página y overlay de carga (`<app-loading [listener]="true">`). */
@Injectable({ providedIn: 'root' })
export class AppState extends BaseStateService<AppStateType> {
    readonly pageTitle = this.select('pageTitle');

    // Contador y no booleano: con dos peticiones en paralelo, la primera que termine
    // no debe apagar el overlay mientras la otra sigue.
    private readonly pendientes = signal(0);
    readonly isLoading = computed(() => this.pendientes() > 0);

    constructor() {
        super({ pageTitle: '' });
    }

    showLoading(): void {
        this.pendientes.update((n) => n + 1);
    }

    hideLoading(): void {
        this.pendientes.update((n) => Math.max(0, n - 1));
    }
}
