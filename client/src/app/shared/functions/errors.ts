import { HttpErrorResponse } from '@angular/common/http';

/** ProblemDetails / ValidationProblemDetails de ASP.NET. */
export interface ProblemDetails {
    title?: string;
    detail?: string;
    status?: number;
    errors?: Record<string, string[]>;
}

/**
 * Mensaje legible de un error HTTP del API. Primero los errores de validación (400),
 * luego `detail` (409/404 de BusinessException), luego `title`.
 */
export function mensajeDeError(error: unknown, porDefecto = 'Ocurrió un error inesperado.'): string {
    if (!(error instanceof HttpErrorResponse)) return error instanceof Error ? error.message : porDefecto;
    if (error.status === 0) return 'No hay conexión con el servidor.';

    const problema = error.error as ProblemDetails | null;
    if (problema?.errors) return Object.values(problema.errors).flat().join(' ');
    return problema?.detail ?? problema?.title ?? porDefecto;
}
