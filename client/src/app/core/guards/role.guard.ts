import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Rol, UserState } from '../../shared/states/user.state';

/**
 * Rol del box actual. Los roles van en la ruta:
 *   { path: 'catalogos', canActivate: [roleGuard], data: { roles: ['ADMIN'] }, loadComponent: ... }
 * Sin `data.roles` deja pasar. Es UX: el API valida el rol en cada endpoint.
 */
export const roleGuard: CanActivateFn = (route) => {
    const roles = route.data['roles'] as Rol[] | undefined;
    if (!roles?.length) return true;

    const rol = inject(UserState).rol();
    return (rol !== null && roles.includes(rol)) || inject(Router).createUrlTree(['/auth/access']);
};
