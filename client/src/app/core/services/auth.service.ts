import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AppState } from '../../shared/states/app.state';
import { BoxSesion, UserState } from '../../shared/states/user.state';
import { LoginResponse, SesionResponse, TokensResponse } from '../dtos';
import { AppStorageService } from './app-storage.service';
import { HttpService } from './http.service';

const URL = `${environment.apiUrl}/auth`;

/**
 * Sesión: login, selección y cambio de box, renovación de tokens y salida.
 * Es el único que escribe en UserState.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly http = inject(HttpService);
    private readonly router = inject(Router);
    private readonly user = inject(UserState);
    private readonly app = inject(AppState);

    /** Login con varios boxes: queda pendiente de elegir uno (solo en memoria; el selectionToken dura 5 min). */
    readonly seleccionPendiente = signal<{ selectionToken: string; boxes: BoxSesion[] } | null>(null);

    // Un solo refresh a la vez: el API rota el refresh token y un segundo intento con el viejo
    // se toma como robo y cierra todas las sesiones del usuario en ese box.
    private refreshEnCurso: Promise<boolean> | null = null;

    /** true mientras se cierra la sesión por refresh fallido; evita salidas repetidas. */
    saliendo = false;

    isAuthenticated(): boolean {
        return this.user.autenticado();
    }

    /** 'listo' entra directo; 'elegir-box' manda a /auth/seleccionar-box. */
    async login(email: string, password: string): Promise<'listo' | 'elegir-box'> {
        this.saliendo = false;
        const r = await this.http.POST<LoginResponse>(`${URL}/login`, { email, password }, { sinAlerta: true });

        if (r.requiereSeleccion) {
            this.seleccionPendiente.set({ selectionToken: r.selectionToken!, boxes: r.boxes! });
            return 'elegir-box';
        }
        await this.iniciar(r as TokensResponse);
        return 'listo';
    }

    async seleccionarBox(boxPublicId: string): Promise<void> {
        const pendiente = this.seleccionPendiente();
        if (!pendiente) throw new Error('No hay una selección de box pendiente.');

        const tokens = await this.http.POST<TokensResponse>(`${URL}/seleccionar-box`, { selectionToken: pendiente.selectionToken, boxPublicId }, { sinAlerta: true });
        this.seleccionPendiente.set(null);
        await this.iniciar(tokens);
    }

    async cambiarBox(boxPublicId: string): Promise<void> {
        const tokens = await this.http.POST<TokensResponse>(`${URL}/cambiar-box`, { boxPublicId });
        await this.iniciar(tokens);
    }

    /** App initializer: si hay tokens guardados, recupera la sesión antes de que corran los guards. */
    async restaurarSesion(): Promise<void> {
        if (!AppStorageService.getRefreshToken()) return;
        try {
            await this.cargarSesion();
        } catch {
            AppStorageService.clearTokens();
            this.user.resetState();
        }
    }

    /** Lo llama el error interceptor ante un 401. true = hay tokens nuevos. */
    refreshToken(): Promise<boolean> {
        this.refreshEnCurso ??= this.ejecutarRefresh().finally(() => (this.refreshEnCurso = null));
        return this.refreshEnCurso;
    }

    async logOut(): Promise<void> {
        const refreshToken = AppStorageService.getRefreshToken();
        const accessToken = AppStorageService.getAccessToken();
        if (refreshToken && accessToken) {
            try {
                // Revoca el refresh token en el API. El endpoint pide Bearer, así que pasa por el request
                // interceptor; el error interceptor no intenta refresh en /auth/logout y SIN_ALERTA calla el error.
                await this.http.POST(`${URL}/logout`, { refreshToken }, { sinAlerta: true });
            } catch {
                // Token vencido o API caída: igual se cierra la sesión local.
            }
        }
        this.limpiar();
        await this.router.navigate(['/auth/login']);
    }

    /** Cierre por refresh fallido (no hay con qué llamar al API). */
    expirarSesion(): void {
        if (this.saliendo) return;
        this.saliendo = true;
        this.limpiar();
        void this.router.navigate(['/auth/login'], { queryParams: { expirada: 1 } });
    }

    private async ejecutarRefresh(): Promise<boolean> {
        const refreshToken = AppStorageService.getRefreshToken();
        if (!refreshToken) return false;
        try {
            const tokens = await this.http.POST<TokensResponse>(`${URL}/refresh`, { refreshToken }, { skipInterceptor: true });
            AppStorageService.setTokens(tokens);
            return true;
        } catch {
            return false;
        }
    }

    private async iniciar(tokens: TokensResponse): Promise<void> {
        AppStorageService.setTokens(tokens);
        await this.cargarSesion();
    }

    private async cargarSesion(): Promise<void> {
        const sesion = await this.http.GET<SesionResponse>(`${URL}/sesion`, { sinAlerta: true });
        this.user.setState({ email: sesion.email, box: sesion.box, boxes: sesion.boxes });
    }

    private limpiar(): void {
        AppStorageService.clearTokens();
        this.user.resetState();
        this.seleccionPendiente.set(null);
        this.app.resetState();
    }
}
