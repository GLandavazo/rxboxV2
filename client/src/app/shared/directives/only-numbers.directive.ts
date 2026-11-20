import { Directive, ElementRef, HostListener, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

/**
 * Solo dígitos: teléfonos, código de atleta. Filtra también lo que se pega.
 * Para montos o decimales usa p-inputnumber, no esta directiva.
 */
@Directive({ selector: '[appOnlyNumbers]' })
export class OnlyNumbersDirective {
    private readonly el = inject<ElementRef<HTMLInputElement>>(ElementRef);
    private readonly control = inject(NgControl, { optional: true });

    @HostListener('input')
    onInput(): void {
        const input = this.el.nativeElement;
        const limpio = input.value.replace(/\D/g, '');
        if (limpio === input.value) return;
        input.value = limpio;
        this.control?.control?.setValue(limpio);
    }
}
