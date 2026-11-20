import { Directive, ElementRef, HostListener, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

/** Convierte a mayúsculas mientras se escribe, conservando la posición del cursor. */
@Directive({ selector: '[appInputUppercase]' })
export class InputUppercaseDirective {
    private readonly el = inject<ElementRef<HTMLInputElement>>(ElementRef);
    private readonly control = inject(NgControl, { optional: true });

    @HostListener('input')
    onInput(): void {
        const input = this.el.nativeElement;
        const { selectionStart, selectionEnd } = input;
        const valor = input.value.toUpperCase();
        if (this.control?.control) this.control.control.setValue(valor, { emitEvent: false });
        input.value = valor;
        input.setSelectionRange(selectionStart, selectionEnd);
    }
}
