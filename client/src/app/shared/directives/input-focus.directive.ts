import { Directive, ElementRef, afterNextRender, inject } from '@angular/core';

/** Enfoca el elemento al aparecer. Para el primer campo de un formulario o diálogo. */
@Directive({ selector: '[appInputFocus]' })
export class InputFocusDirective {
    constructor() {
        const el = inject<ElementRef<HTMLElement>>(ElementRef);
        // Después del primer render: en un p-dialog el contenido aún se está montando.
        afterNextRender(() => setTimeout(() => el.nativeElement.focus(), 50));
    }
}
