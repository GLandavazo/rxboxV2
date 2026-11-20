import { TableLazyLoadEvent } from 'primeng/table';

/** PaginaResponse<T> del API. */
export interface Pagina<T> {
    items: T[];
    total: number;
    pagina: number;
    tamano: number;
    totalPaginas: number;
}

/** Lo que reciben los listados paginados del API (PaginacionRequest). */
export interface Paginacion {
    pagina: number;
    tamano: number;
}

/**
 * Evento de un p-table [lazy] → página y tamaño para el API.
 *   (onLazyLoad)="filtro = { ...filtro, ...paginacionDesdeLazy($event) }; cargar()"
 */
export function paginacionDesdeLazy(e: TableLazyLoadEvent, tamanoPorDefecto = 20): Paginacion {
    const tamano = e.rows ?? tamanoPorDefecto;
    return { pagina: Math.floor((e.first ?? 0) / tamano) + 1, tamano };
}
