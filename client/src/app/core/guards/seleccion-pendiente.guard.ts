import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** /auth/seleccionar-box solo tiene sentido justo después de un login con varios boxes. */
export const seleccionPendienteGuard: CanActivateFn = () => !!inject(AuthService).seleccionPendiente() || inject(Router).createUrlTree(['/auth/login']);
