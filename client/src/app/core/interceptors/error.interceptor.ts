import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, from, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { mensajeDeError } from '../../shared/functions';
import { AppStorageService } from '../services/app-storage.service';
import { AuthService } from '../services/auth.service';
import { AvisosService } from '../services/avisos.service';
import { SIN_ALERTA, SIN_INTERCEPTOR } from '../services/http.service';

// Un 401 en estas rutas es "credenciales incorrectas", no "token vencido".
const SIN_REFRESH = ['/auth/login', '/auth/seleccionar-box', '/auth/refresh', '/auth/logout'].map((r) => environment.apiUrl + r);

/**
 * - 401: renueva el token una vez y repite la petición; si no se puede, cierra la sesión.
 * - Otros errores: aviso con el mensaje del API, salvo SIN_ALERTA.
 * Siempre relanza el HttpErrorResponse original, para que la pantalla pueda leer el status.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
    if (request.context.get(SIN_INTERCEPTOR)) return next(request);

    const auth = inject(AuthService);
    const avisos = inject(AvisosService);

    return next(request).pipe(
        catchError((error: unknown) => {
            if (!(error instanceof HttpErrorResponse)) return throwError(() => error);

            if (error.status === 401 && !SIN_REFRESH.includes(request.url)) {
                return reintentarConTokenNuevo(request, next, auth, avisos);
            }
            avisar(request, error, avisos);
            return throwError(() => error);
        })
    );
};

function reintentarConTokenNuevo(request: HttpRequest<unknown>, next: HttpHandlerFn, auth: AuthService, avisos: AvisosService): Observable<never> | ReturnType<HttpHandlerFn> {
    return from(auth.refreshToken()).pipe(
        switchMap((renovado) => {
            if (!renovado) {
                auth.expirarSesion();
                return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Sesión expirada', url: request.url }));
            }
            const reintento = request.clone({ setHeaders: { Authorization: `Bearer ${AppStorageService.getAccessToken()}` } });
            // Un solo reintento: si vuelve a dar 401 se muestra el error, sin otro refresh.
            return next(reintento).pipe(
                catchError((error: unknown) => {
                    if (error instanceof HttpErrorResponse) avisar(request, error, avisos);
                    return throwError(() => error);
                })
            );
        })
    );
}

function avisar(request: HttpRequest<unknown>, error: HttpErrorResponse, avisos: AvisosService): void {
    if (request.context.get(SIN_ALERTA)) return;
    const severidad = error.status === 0 || error.status >= 500 ? 'error' : 'warn';
    avisos.toast(mensajeDeError(error), severidad, TITULOS[error.status] ?? 'Error');
}

const TITULOS: Record<number, string> = {
    0: 'Sin conexión',
    400: 'Revisa los datos',
    401: 'Sesión',
    403: 'Sin permiso',
    404: 'No encontrado',
    409: 'No se pudo completar'
};
