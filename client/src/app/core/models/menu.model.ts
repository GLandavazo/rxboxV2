import { MenuItem } from 'primeng/api';
import { Rol } from '../../shared/states/user.state';

/** MenuItem de PrimeNG + roles que lo ven. Sin `roles` = todos. */
export interface AppMenuItem extends MenuItem {
    roles?: Rol[];
    items?: AppMenuItem[];
}
