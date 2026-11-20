import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface Step {
    id: string;
    title: string;
}

/**
 * Indicador de pasos para flujos tipo asistente (solo visual; la navegación la lleva la pantalla).
 *   <app-stepper [steps]="pasos" [currentStep]="paso()" />
 * Para pasos con contenido y navegación incluidos usa p-stepper de PrimeNG.
 */
@Component({
    selector: 'app-stepper',
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <ol class="flex items-start justify-between max-w-xl mx-auto mb-8 px-4">
            @for (step of steps(); track step.id; let i = $index; let first = $first) {
                <li class="relative flex flex-1 flex-col items-center">
                    @if (!first) {
                        <span class="absolute top-6 right-1/2 w-full h-0.5 -z-0" [class]="i <= currentStep() ? 'bg-primary' : 'bg-surface-300'"></span>
                    }
                    <span
                        class="relative z-10 grid place-items-center w-12 h-12 rounded-full border-2 font-bold transition-colors"
                        [class]="i < currentStep() ? 'bg-primary border-primary text-primary-contrast' : i === currentStep() ? 'bg-primary border-primary text-primary-contrast shadow-md' : 'bg-surface-0 border-surface-300 text-muted-color'"
                    >
                        @if (i < currentStep()) {
                            <i class="pi pi-check"></i>
                        } @else {
                            {{ i + 1 }}
                        }
                    </span>
                    <span class="mt-3 text-center text-sm" [class]="i <= currentStep() ? 'font-semibold' : 'text-muted-color'">
                        {{ step.title }}
                    </span>
                </li>
            }
        </ol>
    `
})
export class StepperComponent {
    readonly steps = input.required<Step[]>();
    /** Índice del paso actual, desde 0. */
    readonly currentStep = input.required<number>();
}
