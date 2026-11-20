import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { AppState } from '../../shared/states/app.state';
import { AppStorageService } from '../services/app-storage.service';
import { SIN_INTERCEPTOR, SIN_OVERLAY } from '../services/http.service';

/** Agrega el Bearer y prende el overlay global mientras dura la petición. */
export const requestInterceptor: HttpInterceptorFn = (request, next) => {
    if (request.context.get(SIN_INTERCEPTOR)) return next(request);

    const token = AppStorageService.getAccessToken();
    if (token) request = request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

    // Lecturas con skeleton propio: sin overlay, pero con token y manejo de errores.
    if (request.context.get(SIN_OVERLAY)) return next(request);

    const app = inject(AppState);
    app.showLoading();
    return next(request).pipe(finalize(() => app.hideLoading()));
};
