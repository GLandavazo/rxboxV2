import { Component, computed, inject } from '@angular/core';
import { MENU, menuParaRol } from '@/app/core/data';
import { UserState } from '@/app/shared/states';
import { AppMenuitem } from './app.menuitem';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [AppMenuitem],
    template: `<ul class="layout-menu">
        @for (item of model(); track item.label) {
            @if (!item.separator) {
                <li app-menuitem [item]="item" [root]="true"></li>
            } @else {
                <li class="menu-separator"></li>
            }
        }
    </ul> `
})
export class AppMenu {
    private readonly user = inject(UserState);

    /** Se recalcula al cambiar de box (otro rol, otro menú). */
    protected readonly model = computed(() => menuParaRol(MENU, this.user.rol()));
}
