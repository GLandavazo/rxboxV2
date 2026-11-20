import { Injectable, computed } from '@angular/core';
import { BaseStateService } from './base.state';

export type Rol = 'ADMIN' | 'COACH';

export interface BoxSesion {
    publicId: string;
    nombre: string;
    rol: Rol;
}

type UserStateType = {
    email: string;
    box: BoxSesion | null;
    boxes: BoxSesion[];
};

/**
 * Sesión del usuario: lo que regresa `GET /api/auth/sesion`.
 * Lo llena el AuthService al iniciar sesión, cambiar de box o restaurar la sesión,
 * y lo limpia al salir (`resetState()`). El resto de la app solo lee.
 */
@Injectable({ providedIn: 'root' })
export class UserState extends BaseStateService<UserStateType> {
    readonly email = this.select('email');
    readonly box = this.select('box');
    readonly boxes = this.select('boxes');

    readonly rol = computed(() => this.state().box?.rol ?? null);
    readonly esAdmin = computed(() => this.rol() === 'ADMIN');
    readonly autenticado = computed(() => this.state().box !== null);

    constructor() {
        super({ email: '', box: null, boxes: [] });
    }
}
