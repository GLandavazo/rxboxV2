import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserState } from '../../shared/states/user.state';

/** Para /auth/login: si ya hay sesión, no tiene caso mostrarlo. */
export const isAuthenticatedGuard: CanActivateFn = () => (inject(UserState).autenticado() ? inject(Router).createUrlTree(['/']) : true);
