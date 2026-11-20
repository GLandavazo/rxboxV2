import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserState } from '../../shared/states/user.state';

/**
 * Hay sesión. La sesión ya se restauró en el app initializer, así que basta leer UserState.
 * Sin sesión manda al login recordando a dónde quería ir.
 */
export const authGuard: CanActivateFn = (_route, state) =>
    inject(UserState).autenticado() || inject(Router).createUrlTree(['/auth/login'], { queryParams: { volver: state.url } });
