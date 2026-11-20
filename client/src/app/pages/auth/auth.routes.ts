import { Routes } from '@angular/router';
import { isAuthenticatedGuard, seleccionPendienteGuard } from '@/app/core/guards';
import { Access } from './access';
import { Error } from './error';
import { Login } from './login';
import { SeleccionarBox } from './seleccionar-box';

export default [
    { path: 'access', component: Access },
    { path: 'error', component: Error },
    { path: 'login', component: Login, canActivate: [isAuthenticatedGuard] },
    { path: 'seleccionar-box', component: SeleccionarBox, canActivate: [seleccionPendienteGuard] },
    { path: '', pathMatch: 'full', redirectTo: 'login' }
] as Routes;
