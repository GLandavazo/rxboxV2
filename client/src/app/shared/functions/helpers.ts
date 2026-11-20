import { HttpParams } from '@angular/common/http';
import { IsNoE } from './objects';

/**
 * Objeto de filtros → HttpParams. Omite valores vacíos y repite la llave para arreglos
 * (`tipo=A&tipo=B`), que es como ASP.NET enlaza `[FromQuery] List<T>`.
 *
 *   this.http.get(url, { params: toHttpParams({ buscar, tipo, pagina: 1 }) })
 */
export function toHttpParams(data: object): HttpParams {
    let params = new HttpParams();
    Object.entries(data).forEach(([key, value]) => {
        if (IsNoE(value)) return;
        if (Array.isArray(value)) {
            value.forEach((item) => (params = params.append(key, String(item))));
        } else {
            params = params.set(key, value instanceof Date ? value.toISOString() : String(value));
        }
    });
    return params;
}

/**
 * Descarga un Blob que ya llegó por HttpClient (así lleva el token del interceptor):
 *
 *   const blob = await firstValueFrom(this.http.get(url, { responseType: 'blob' }));
 *   descargarBlob(blob, 'reporte.xlsx');
 */
export function descargarBlob(blob: Blob, nombre: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = nombre;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 100);
}
