import { ChangeDetectionStrategy, Component, ElementRef, computed, input, output, viewChildren } from '@angular/core';

/**
 * Captura de código por dígitos (código de atleta en el check-in).
 *   <app-input-code [longitud]="6" (codigo)="validar($event)" />
 * Emite el código completo, o '' mientras falten dígitos. Acepta pegar el código entero.
 */
@Component({
    selector: 'app-input-code',
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div class="flex justify-center gap-2 sm:gap-3 my-3">
            @for (i of posiciones(); track i) {
                <input
                    #digito
                    type="text"
                    inputmode="numeric"
                    autocomplete="one-time-code"
                    maxlength="1"
                    [attr.aria-label]="'Dígito ' + (i + 1)"
                    class="w-11 h-14 sm:w-14 sm:h-16 text-center text-2xl font-semibold rounded-lg border border-surface-300 bg-surface-0 focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/30"
                    (input)="alEscribir(i)"
                    (keydown)="alTecla($event, i)"
                    (paste)="alPegar($event)"
                />
            }
        </div>
    `
})
export class InputCodeComponent {
    readonly longitud = input(6);
    readonly codigo = output<string>();

    protected readonly posiciones = computed(() => Array.from({ length: this.longitud() }, (_, i) => i));
    private readonly digitos = viewChildren<ElementRef<HTMLInputElement>>('digito');

    /** Limpia y enfoca el primer dígito (ej. después de un check-in exitoso). */
    limpiar(): void {
        this.inputs().forEach((input) => (input.value = ''));
        this.enfocar(0);
        this.emitir();
    }

    protected alEscribir(i: number): void {
        const input = this.inputs()[i];
        input.value = input.value.replace(/\D/g, '').slice(-1);
        if (input.value) this.enfocar(i + 1);
        this.emitir();
    }

    protected alTecla(e: KeyboardEvent, i: number): void {
        if (e.key === 'Backspace' && !this.inputs()[i].value) this.enfocar(i - 1);
        else if (e.key === 'ArrowLeft') this.enfocar(i - 1);
        else if (e.key === 'ArrowRight') this.enfocar(i + 1);
    }

    protected alPegar(e: ClipboardEvent): void {
        e.preventDefault();
        const texto = (e.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, this.longitud());
        this.inputs().forEach((input, i) => (input.value = texto[i] ?? ''));
        this.enfocar(Math.min(texto.length, this.longitud() - 1));
        this.emitir();
    }

    private inputs(): HTMLInputElement[] {
        return this.digitos().map((d) => d.nativeElement);
    }

    private enfocar(i: number): void {
        this.inputs()[i]?.focus();
    }

    private emitir(): void {
        const valores = this.inputs().map((input) => input.value);
        this.codigo.emit(valores.includes('') ? '' : valores.join(''));
    }
}
