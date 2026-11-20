import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

/** Fila de carga para p-table. Uso: <tr app-skeleton-fila [columnas]="4"></tr> */
@Component({
    selector: 'tr[app-skeleton-fila]',
    imports: [Skeleton],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        @for (ancho of anchos(); track $index) {
            <td><p-skeleton [width]="ancho" height="1.25rem" /></td>
        }
    `
})
export class SkeletonFilaComponent {
    readonly columnas = input(4);

    // Anchos alternados para que no se vea como una rejilla uniforme.
    private readonly ciclo = ['70%', '55%', '85%', '45%'];
    protected readonly anchos = computed(() => Array.from({ length: this.columnas() }, (_, i) => this.ciclo[i % this.ciclo.length]));
}
