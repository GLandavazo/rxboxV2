import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AppState } from '../../states/app.state';

/**
 * Overlay de carga a pantalla completa.
 *   <app-loading [listener]="true" />    sigue a AppState.isLoading (va una vez en el layout)
 *   @if (cargando()) { <app-loading /> } siempre visible mientras exista
 */
@Component({
    selector: 'app-loading',
    imports: [ProgressSpinnerModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @if (visible()) {
            <div class="fixed inset-0 z-[1200] grid place-items-center bg-surface-0/60 dark:bg-surface-950/60" animate.enter="animate-fadein" animate.leave="animate-fadeout" role="status" aria-live="polite">
                <p-progress-spinner strokeWidth="4" ariaLabel="Cargando" />
            </div>
        }
    `
})
export class LoadingComponent {
    readonly listener = input(false);
    private readonly app = inject(AppState);
    protected readonly visible = computed(() => (this.listener() ? this.app.isLoading() : true));
}
