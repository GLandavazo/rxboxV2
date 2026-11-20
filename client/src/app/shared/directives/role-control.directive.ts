import { Directive, ElementRef, Renderer2, effect, inject, input } from '@angular/core';
import { Rol, UserState } from '../states/user.state';

/**
 * Oculta el elemento si el rol del box actual no está en la lista.
 *   <p-button appRoleControl [appRoleControl]="['ADMIN']" label="Agregar" />
 * Reacciona al cambio de box. Es solo UX: el backend valida el rol en cada endpoint.
 */
@Directive({ selector: '[appRoleControl]' })
export class RoleControlDirective {
    readonly appRoleControl = input.required<Rol[]>();

    private readonly el = inject(ElementRef);
    private readonly renderer = inject(Renderer2);
    private readonly user = inject(UserState);

    constructor() {
        effect(() => {
            const roles = this.appRoleControl();
            const rol = this.user.rol();
            const permitido = roles.length === 0 || (rol !== null && roles.includes(rol));
            this.renderer.setStyle(this.el.nativeElement, 'display', permitido ? '' : 'none');
        });
    }
}
