import { HttpClient, HttpContext, HttpContextToken, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toHttpParams } from '../../shared/functions';

/** Salta todos los interceptores: sin token, sin refresh y sin aviso. Solo para refresh/logout. */
export const SIN_INTERCEPTOR = new HttpContextToken<boolean>(() => false);

/** Sin overlay global: para lecturas cuya región ya muestra su propio skeleton. */
export const SIN_OVERLAY = new HttpContextToken<boolean>(() => false);

/** Sin toast de error: la pantalla muestra el error en su lugar (ej. dentro del formulario de login). */
export const SIN_ALERTA = new HttpContextToken<boolean>(() => false);

type Metodo = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export interface HttpOpciones {
    /** Objeto de filtros; vacíos se omiten (toHttpParams). */
    params?: object | HttpParams;
    skipInterceptor?: boolean;
    sinOverlay?: boolean;
    sinAlerta?: boolean;
}

/**
 * Envoltura de HttpClient que regresa Promises, para usar con async/await en servicios y pantallas.
 * Cada módulo tiene su servicio en core/services que usa este:
 *
 *   @Injectable({ providedIn: 'root' })
 *   export class PersonasService {
 *     private readonly http = inject(HttpService);
 *     private readonly url = `${environment.apiUrl}/personas`;
 *     listar(f: PersonaFiltro) { return this.http.GET<Pagina<PersonaItem>>(this.url, { params: f, sinOverlay: true }); }
 *     crear(body: PersonaCreateRequest) { return this.http.POST<Persona>(this.url, body); }
 *   }
 */
@Injectable({ providedIn: 'root' })
export class HttpService {
    private readonly http = inject(HttpClient);

    GET<T>(url: string, opciones?: HttpOpciones): Promise<T> {
        return this.request<T>('GET', url, undefined, opciones);
    }

    POST<T>(url: string, body?: unknown, opciones?: HttpOpciones): Promise<T> {
        return this.request<T>('POST', url, body ?? {}, opciones);
    }

    PUT<T>(url: string, body: unknown, opciones?: HttpOpciones): Promise<T> {
        return this.request<T>('PUT', url, body, opciones);
    }

    PATCH<T>(url: string, body?: unknown, opciones?: HttpOpciones): Promise<T> {
        return this.request<T>('PATCH', url, body ?? {}, opciones);
    }

    DELETE<T = void>(url: string, opciones?: HttpOpciones): Promise<T> {
        return this.request<T>('DELETE', url, undefined, opciones);
    }

    /** Archivo binario con token; descárgalo con descargarBlob(). */
    GET_BLOB(url: string, opciones?: HttpOpciones): Promise<Blob> {
        return firstValueFrom(this.http.get(url, { responseType: 'blob', params: this.params(opciones), context: this.contexto(opciones) }));
    }

    private request<T>(metodo: Metodo, url: string, body?: unknown, opciones?: HttpOpciones): Promise<T> {
        return firstValueFrom(this.http.request<T>(metodo, url, { body, params: this.params(opciones), context: this.contexto(opciones) }));
    }

    private params(opciones?: HttpOpciones): HttpParams | undefined {
        const p = opciones?.params;
        if (!p) return undefined;
        return p instanceof HttpParams ? p : toHttpParams(p);
    }

    private contexto(opciones?: HttpOpciones): HttpContext {
        return new HttpContext()
            .set(SIN_INTERCEPTOR, !!opciones?.skipInterceptor)
            .set(SIN_OVERLAY, !!opciones?.sinOverlay)
            .set(SIN_ALERTA, !!opciones?.sinAlerta);
    }
}
