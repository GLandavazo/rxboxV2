import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, computed, input, signal, viewChild } from '@angular/core';

/**
 * Envoltura de campo: etiqueta, asterisco si el control interno es `required` y mensaje de error.
 *
 *   <app-input label="Nombre" error="Requerido" [valid]="!form.controls.nombre.invalid || !form.controls.nombre.touched">
 *     <input pInputText formControlName="nombre" required />
 *   </app-input>
 * Con Validators.required (sin atributo) usa [obligatorio]="true" para el asterisco.
 */
@Component({
    selector: 'app-input',
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div class="flex flex-col gap-1">
            @if (label()) {
                <label class="text-xs font-semibold uppercase tracking-widest text-muted-color">
                    {{ label() }}
                    @if (required()) {
                        <span class="text-red-500">*</span>
                    }
                </label>
            }
            <div #contenido class="flex flex-col">
                <ng-content />
            </div>
            @if (!valid() && error()) {
                <small class="text-red-500">{{ error() }}</small>
            }
        </div>
    `
})
export class InputComponent {
    readonly label = input<string>();
    readonly error = input<string>();
    /** false muestra el error. Normalmente `!control.invalid || !control.touched`. */
    readonly valid = input(true);
    /** Fuerza el asterisco. Si no se indica, se detecta el atributo `required` del control interno. */
    readonly obligatorio = input<boolean>();

    private readonly detectado = signal(false);
    protected readonly required = computed(() => this.obligatorio() ?? this.detectado());
    private readonly contenido = viewChild.required<ElementRef<HTMLElement>>('contenido');

    constructor() {
        afterNextRender(() => {
            const el = this.contenido().nativeElement;
            this.detectado.set(!!el.querySelector('[required]'));
        });
    }
}
