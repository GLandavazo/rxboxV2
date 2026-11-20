import { Directive, ElementRef, OnInit, inject } from '@angular/core';

/**
 * Saca a los hijos del elemento host y lo elimina del DOM. Para componentes que deben
 * quedar "planos" dentro de una estructura estricta (ej. un <li> directo en un <ul>).
 * Úsala poco: rompe el encapsulamiento y no funciona con contenido que cambia.
 */
@Directive({ selector: '[appRemoveHost]' })
export class RemoveHostDirective implements OnInit {
    private readonly el = inject<ElementRef<HTMLElement>>(ElementRef);

    ngOnInit(): void {
        const host = this.el.nativeElement;
        const padre = host.parentElement;
        if (!padre) return;
        while (host.firstChild) padre.insertBefore(host.firstChild, host);
        padre.removeChild(host);
    }
}
