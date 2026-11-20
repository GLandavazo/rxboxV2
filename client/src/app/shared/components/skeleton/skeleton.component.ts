import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

export type SkeletonVariante = 'texto' | 'tarjeta' | 'lista';

/**
 * Placeholder de carga para cualquier región.
 *   <app-skeleton />                                   3 líneas de texto
 *   <app-skeleton variante="texto" [lineas]="5" />
 *   <app-skeleton variante="tarjeta" alto="10rem" />   encabezado + bloque
 *   <app-skeleton variante="lista" [lineas]="4" />     avatar + 2 líneas por ítem
 */
@Component({
    selector: 'app-skeleton',
    imports: [Skeleton],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div role="status" aria-busy="true" class="flex flex-col gap-2">
            <span class="sr-only">Cargando…</span>
            @switch (variante()) {
                @case ('tarjeta') {
                    <div class="flex items-center gap-3 mb-2">
                        <p-skeleton shape="circle" size="3rem" />
                        <div class="flex-1 flex flex-col gap-2">
                            <p-skeleton width="60%" />
                            <p-skeleton width="35%" height=".75rem" />
                        </div>
                    </div>
                    <p-skeleton width="100%" [height]="alto()" />
                }
                @case ('lista') {
                    @for (item of items(); track $index) {
                        <div class="flex items-center gap-3">
                            <p-skeleton shape="circle" size="2.5rem" />
                            <div class="flex-1 flex flex-col gap-2">
                                <p-skeleton width="100%" />
                                <p-skeleton width="75%" />
                            </div>
                        </div>
                    }
                }
                @default {
                    @for (ancho of anchos(); track $index) {
                        <p-skeleton [width]="ancho" />
                    }
                }
            }
        </div>
    `
})
export class SkeletonComponent {
    readonly variante = input<SkeletonVariante>('texto');
    readonly lineas = input(3);
    readonly alto = input('8rem');

    protected readonly items = computed(() => Array.from({ length: this.lineas() }));
    // La última línea más corta, como un párrafo real.
    protected readonly anchos = computed(() => Array.from({ length: this.lineas() }, (_, i) => (this.lineas() > 1 && i === this.lineas() - 1 ? '60%' : '100%')));
}
