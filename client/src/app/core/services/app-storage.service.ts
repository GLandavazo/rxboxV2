import { TokensResponse } from '../dtos';

const ACCESS = 'rxbox.accessToken';
const REFRESH = 'rxbox.refreshToken';

/**
 * Lo único que vive en localStorage: los tokens, para sobrevivir a recargas.
 * Los datos del usuario NO se guardan aquí: se piden a /api/auth/sesion y viven en UserState.
 * Antes de producción abierta conviene mover el refresh token a una cookie httpOnly.
 */
export class AppStorageService {
    static setTokens(tokens: TokensResponse): void {
        localStorage.setItem(ACCESS, tokens.accessToken);
        localStorage.setItem(REFRESH, tokens.refreshToken);
    }

    static getAccessToken(): string | null {
        return localStorage.getItem(ACCESS);
    }

    static getRefreshToken(): string | null {
        return localStorage.getItem(REFRESH);
    }

    static clearTokens(): void {
        localStorage.removeItem(ACCESS);
        localStorage.removeItem(REFRESH);
    }
}
