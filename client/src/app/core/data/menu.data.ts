import { AppMenuItem } from '../models';
import { Rol } from '../../shared/states/user.state';

/**
 * Menú lateral. Fuente única para el sidebar (AppMenu) y el breadcrumb.
 * Cada módulo nuevo agrega aquí su entrada; `roles` limita quién la ve:
 *   { label: 'Catálogos', icon: 'pi pi-fw pi-list', routerLink: ['/catalogos'], roles: ['ADMIN'] }
 */
export const MENU: AppMenuItem[] = [
    {
        label: 'Inicio',
        items: [{ label: 'Inicio', icon: 'pi pi-fw pi-home', routerLink: ['/'] }]
    },
    // Demos de Sakai: quitar cuando ya no se consulten.
    {
        label: 'Plantilla (demo)',
        items: [
            { label: 'Form Layout', icon: 'pi pi-fw pi-id-card', routerLink: ['/uikit/formlayout'] },
            { label: 'Input', icon: 'pi pi-fw pi-check-square', routerLink: ['/uikit/input'] },
            { label: 'Button', icon: 'pi pi-fw pi-mobile', routerLink: ['/uikit/button'] },
            { label: 'Table', icon: 'pi pi-fw pi-table', routerLink: ['/uikit/table'] },
            { label: 'Overlay', icon: 'pi pi-fw pi-clone', routerLink: ['/uikit/overlay'] },
            { label: 'Message', icon: 'pi pi-fw pi-comment', routerLink: ['/uikit/message'] },
            { label: 'Crud', icon: 'pi pi-fw pi-pencil', routerLink: ['/pages/crud'] }
        ]
    }
];

/** Deja solo lo que el rol puede ver y quita grupos que quedan vacíos. */
export function menuParaRol(items: AppMenuItem[], rol: Rol | null): AppMenuItem[] {
    return items
        .filter((item) => !item.roles || (rol !== null && item.roles.includes(rol)))
        .map((item) => (item.items ? { ...item, items: menuParaRol(item.items, rol) } : item))
        .filter((item) => !item.items || item.items.length > 0);
}
