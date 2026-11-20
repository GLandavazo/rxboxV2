import { BoxSesion } from '../../shared/states/user.state';

export interface LoginRequest {
    email: string;
    password: string;
}

export interface TokensResponse {
    accessToken: string;
    refreshToken: string;
    accessExpiraEn: string;
}

/** Un box: trae tokens. Varios: trae boxes y selectionToken para elegir. */
export interface LoginResponse extends Partial<TokensResponse> {
    requiereSeleccion: boolean;
    boxes?: BoxSesion[];
    selectionToken?: string;
}

/** GET /api/auth/sesion */
export interface SesionResponse {
    email: string;
    box: BoxSesion;
    boxes: BoxSesion[];
}
