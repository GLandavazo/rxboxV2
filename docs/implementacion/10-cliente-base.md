# Implementación 10 — Cliente base (front)

Requerimientos: `docs/requerimientos/10-cliente-base.md`.

Stack: **Angular 21** (standalone, signals, zoneless), **PrimeNG 21** con tema Aura, **Tailwind 4** con `tailwindcss-primeui`. Versiones: `@angular/cli` 21.2, `primeng` 21.1.10, `@primeuix/themes` 2.x, `tailwindcss` 4.3.

> **Por qué Angular 21 y no 22.** Desde la versión 22, PrimeNG ya no es MIT: pasa a la licencia comercial PrimeUI, que exige una llave (`providePrimeNG({ license: '…' })`) y sin ella muestra un aviso "Invalid PrimeUI License" en pantalla. Hay una licencia Community gratuita para equipos chicos, pero se renueva cada año y hay que registrarse. PrimeNG 21.1.10 sigue siendo MIT, es la misma línea que usaba el RxBox anterior y todo este código compila y corre igual en ambas versiones (probado). Angular 21 tiene soporte LTS hasta mayo de 2027; antes de esa fecha se decide si pasar a PrimeNG 22 con llave Community o cambiar de librería.

Estructura final de `cliente/src/app`:

```
app/
├── app.ts / app.config.ts / app.routes.ts
├── core/
│   ├── api.models.ts                  Pagina<T>, ProblemDetails
│   ├── http/error.interceptor.ts      toasts de error (RC-003)
│   ├── auth/
│   │   ├── auth.models.ts
│   │   ├── token.storage.ts
│   │   ├── auth.service.ts            login, selección, cambio de box, refresh, sesión
│   │   ├── auth.interceptor.ts        bearer + refresh automático (RC-002)
│   │   └── auth.guards.ts
│   └── catalogos/
│       ├── catalogo.models.ts
│       └── catalogos.service.ts       editables + fijos con caché
├── layout/app-layout.ts               menú, box actual, cambio de box, salir
└── features/
    ├── auth/login.page.ts, seleccionar-box.page.ts
    ├── inicio/inicio.page.ts
    ├── catalogos/catalogos.page.ts
    └── personas/
        ├── persona.models.ts, personas.service.ts
        ├── personas.page.ts           listado paginado en servidor
        ├── persona-form.dialog.ts     alta y edición
        └── persona-detalle.page.ts    datos, código y contactos
```

La app es **zoneless** (default desde Angular 21): todo estado que cambie después de un `await` va en un `signal`, si no, la vista no se actualiza.

Cada etapa termina con la app compilando (`ng serve`) y probada a mano contra la API en `http://localhost:5080`.

---

## Etapa 0 — Backend: sesión y pendientes

### 0.1 Pendientes de los módulos anteriores

Encontrados al revisar `server/`:

1. **`Core/DTOs/Responses/Personas/PersonaResponses.cs` tiene el namespace duplicado** (un `namespace RxBox.Core.DTOs.Responses.Personas` dentro de otro igual). Compila, pero el namespace real queda como `RxBox.Core.DTOs.Responses.Personas.RxBox.Core.DTOs.Responses.Personas`. Deja solo uno y ajusta los `using` que hayan tenido que usar el nombre largo.
2. **`CatalogoFijoService` no tiene `tipos-persona`** (paso 0.1 del módulo 02). Agrega `["tipos-persona"] = typeof(TipoPersona),`. El filtro de personas del front lo usa.

**Commit:** `fix(personas): namespace de responses y catálogo fijo tipos-persona`

### 0.2 RF-AUT-001: `GET /api/auth/sesion`

`Core/DTOs/Responses/Auth/AuthResponses.cs`, agrega:

```csharp
    public record SesionResponse(string Email, BoxOpcionResponse Box, IReadOnlyList<BoxOpcionResponse> Boxes);
```

`Core/Interfaces/IAuthService.cs`, agrega:

```csharp
        Task<SesionResponse> ObtenerSesionAsync(CancellationToken ct = default);
```

`Infrastructure/Auth/AuthService.cs`, agrega:

```csharp
        public async Task<SesionResponse> ObtenerSesionAsync(CancellationToken ct = default)
        {
            var usuarioId = currentUser.UsuarioId ?? throw new UnauthenticatedException("No hay sesión.");
            var boxId = currentUser.BoxId ?? throw new UnauthenticatedException("No hay sesión.");

            var email = await db.Usuarios.AsNoTracking()
                .Where(u => u.Id == usuarioId).Select(u => u.Email).SingleAsync(ct);

            var boxes = await BoxesActivos(usuarioId).Select(ADeUsuario).ToListAsync(ct);

            // Su acceso a este box se desactivó mientras el access token seguía vivo (CA-002).
            var actual = boxes.FirstOrDefault(b => b.BoxId == boxId)
                ?? throw new UnauthenticatedException("Ya no tienes acceso a este box.");

            return new SesionResponse(email, Opcion(actual), [.. boxes.Select(Opcion)]);
        }

        private static BoxOpcionResponse Opcion(BoxDeUsuario b) => new(b.PublicId, b.Nombre, b.Rol.ToString());
```

> Puedes reusar `Opcion` en `LoginAsync`, donde hoy se arma `new BoxOpcionResponse(b.PublicId, b.Nombre, b.Rol.ToString())` a mano.

`API/Controllers/AuthController.cs`, agrega (la clase ya tiene `[Authorize]`):

```csharp
        [HttpGet("sesion")]
        public async Task<IActionResult> Sesion(CancellationToken ct) =>
            Ok(await authService.ObtenerSesionAsync(ct));
```

Test en `AuthTests` (y `using System.Text.Json;` arriba):

```csharp
        [Fact]
        public async Task Sesion_regresa_el_box_actual_y_todos_los_boxes_del_usuario()
        {
            var boxA = await factory.CrearBoxAsync();
            var boxB = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(boxA, boxB);
            using var client = factory.CreateClient();

            var login = await (await client.PostAsJsonAsync("/api/auth/login",
                new { email = usuario.Email, password = RxBoxApiFactory.Password })).Content.ReadFromJsonAsync<JsonElement>();
            var tokens = await (await client.PostAsJsonAsync("/api/auth/seleccionar-box",
                new { selectionToken = login.GetProperty("selectionToken").GetString(), boxPublicId = boxB.PublicId }))
                .Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.GetProperty("accessToken").GetString());

            var sesion = await client.GetFromJsonAsync<JsonElement>("/api/auth/sesion");

            Assert.Equal(usuario.Email, sesion.GetProperty("email").GetString());
            Assert.Equal(boxB.PublicId.ToString(), sesion.GetProperty("box").GetProperty("publicId").GetString());
            Assert.Equal(2, sesion.GetProperty("boxes").GetArrayLength());
        }
```

Postman: agrega `GET {{baseUrl}}/api/auth/sesion` a la carpeta Auth (ya viene en la colección actualizada).

**Commit:** `feat(auth): consulta de sesión actual`

---

## Etapa 1 — Proyecto

### 1.1 Crear el proyecto

Desde la raíz `rxboxV2/` (crea la carpeta `cliente/`):

```bash
npx @angular/cli@21 new cliente --style=css --routing --ssr=false --skip-tests
cd cliente
npm install primeng@21 @primeuix/themes@2 primeicons tailwindcss @tailwindcss/postcss postcss tailwindcss-primeui
npx ng generate environments
```

Si el CLI pregunta por configuración de IA, elige "None".

### 1.2 Tailwind

`cliente/.postcssrc.json`

```json
{
  "plugins": {
    "@tailwindcss/postcss": {}
  }
}
```

`src/styles.css` (reemplaza su contenido)

```css
@import "primeicons/primeicons.css";
@import "tailwindcss";
@import "tailwindcss-primeui";

body {
  @apply bg-surface-50 text-surface-900 antialiased;
  font-family: Inter, system-ui, sans-serif;
}
```

`tailwindcss-primeui` hace que las clases de Tailwind usen los colores del tema (`bg-primary`, `text-muted-color`, `bg-surface-0`…), así el front y PrimeNG comparten paleta.

### 1.3 Ambientes

`src/environments/environment.development.ts`

```ts
export const environment = {
  apiUrl: 'http://localhost:5080/api',
};
```

`src/environments/environment.ts` (producción; se ajusta al desplegar)

```ts
export const environment = {
  apiUrl: '/api',
};
```

La API ya permite CORS desde `http://localhost:4200` (`appsettings.Development.json`).

### 1.4 Tema

`src/app/tema.ts`. Primario esmeralda, como en `DESIGN.md`.

```ts
import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const RxBoxPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{emerald.50}', 100: '{emerald.100}', 200: '{emerald.200}', 300: '{emerald.300}',
      400: '{emerald.400}', 500: '{emerald.500}', 600: '{emerald.600}', 700: '{emerald.700}',
      800: '{emerald.800}', 900: '{emerald.900}', 950: '{emerald.950}',
    },
  },
});
```

### 1.5 Configuración

`src/app/app.config.ts`

```ts
import { ApplicationConfig, LOCALE_ID, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import { routes } from './app.routes';
import { RxBoxPreset } from './tema';
import { errorInterceptor } from './core/http/error.interceptor';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Orden importa: errorInterceptor es el más externo, así ve el error DESPUÉS de que
    // authInterceptor intentó renovar el token, y no muestra toasts de 401 recuperables.
    provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
    providePrimeNG({
      theme: {
        preset: RxBoxPreset,
        options: { darkModeSelector: '.app-dark', cssLayer: { name: 'primeng', order: 'theme, base, primeng' } },
      },
      translation: {
        accept: 'Sí', reject: 'No', emptyMessage: 'Sin resultados', emptyFilterMessage: 'Sin resultados',
        dayNamesMin: ['D', 'L', 'M', 'M', 'J', 'V', 'S'], firstDayOfWeek: 1,
      },
    }),
    MessageService,
    ConfirmationService,
    { provide: LOCALE_ID, useValue: 'es-MX' },
    // Restaura la sesión guardada antes de que corran los guards.
    provideAppInitializer(() => inject(AuthService).restaurar()),
  ],
};
```

`src/main.ts`, agrega antes de `bootstrapApplication`:

```ts
import { registerLocaleData } from '@angular/common';
import localeEsMx from '@angular/common/locales/es-MX';

registerLocaleData(localeEsMx);
```

`src/app/app.ts`

```ts
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastModule, ConfirmDialogModule],
  template: `
    <router-outlet />
    <p-toast position="top-right" />
    <p-confirmdialog />
  `,
})
export class App {}
```

Borra `app.html` y `app.css` si el CLI los generó.

### 1.6 Modelos comunes

`src/app/core/api.models.ts`

```ts
/** PaginaResponse<T> del backend. */
export interface Pagina<T> {
  items: T[];
  total: number;
  pagina: number;
  tamano: number;
  totalPaginas: number;
}

/** ProblemDetails / ValidationProblemDetails de ASP.NET. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
```

### 1.7 Errores (RC-003)

`src/app/core/http/error.interceptor.ts`

```ts
import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails } from '../api.models';

/** Marca una petición cuyo error muestra la propia pantalla (ej. login). */
export const SIN_TOAST = new HttpContextToken<boolean>(() => false);

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(MessageService);

  return next(req).pipe(
    catchError((error: unknown) => {
      // 401: lo resuelve authInterceptor (renueva o manda a login).
      if (error instanceof HttpErrorResponse && error.status !== 401 && !req.context.get(SIN_TOAST)) {
        toast.add({
          severity: error.status === 0 || error.status >= 500 ? 'error' : 'warn',
          summary: tituloDeError(error),
          detail: mensajeDeError(error),
          life: 6000,
        });
      }
      return throwError(() => error);
    }),
  );
};

export function mensajeDeError(error: HttpErrorResponse): string {
  if (error.status === 0) return 'No hay conexión con el servidor.';
  const problema = error.error as ProblemDetails | null;
  if (problema?.errors) return Object.values(problema.errors).flat().join(' ');
  return problema?.detail ?? problema?.title ?? 'Ocurrió un error inesperado.';
}

function tituloDeError(error: HttpErrorResponse): string {
  switch (error.status) {
    case 0: return 'Sin conexión';
    case 400: return 'Revisa los datos';
    case 403: return 'Sin permiso';
    case 404: return 'No encontrado';
    case 409: return 'No se pudo completar';
    default: return 'Error del servidor';
  }
}
```

**Commit:** `chore(cliente): proyecto Angular con PrimeNG y Tailwind`

---

## Etapa 2 — Autenticación

### 2.1 Modelos y almacenamiento

`src/app/core/auth/auth.models.ts`

```ts
export type Rol = 'ADMIN' | 'COACH';

export interface BoxOpcion {
  publicId: string;
  nombre: string;
  rol: Rol;
}

export interface Tokens {
  accessToken: string;
  refreshToken: string;
  accessExpiraEn: string;
}

export interface LoginResponse extends Partial<Tokens> {
  requiereSeleccion: boolean;
  boxes?: BoxOpcion[];
  selectionToken?: string;
}

export interface Sesion {
  email: string;
  box: BoxOpcion;
  boxes: BoxOpcion[];
}
```

`src/app/core/auth/token.storage.ts`

```ts
import { Injectable } from '@angular/core';
import { Tokens } from './auth.models';

const ACCESS = 'rxbox.access';
const REFRESH = 'rxbox.refresh';

/**
 * Tokens en localStorage para sobrevivir recargas. Suficiente para el piloto; antes de
 * producción abierta conviene mover el refresh token a una cookie httpOnly.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  get access(): string | null { return localStorage.getItem(ACCESS); }
  get refresh(): string | null { return localStorage.getItem(REFRESH); }

  guardar(tokens: Tokens): void {
    localStorage.setItem(ACCESS, tokens.accessToken);
    localStorage.setItem(REFRESH, tokens.refreshToken);
  }

  limpiar(): void {
    localStorage.removeItem(ACCESS);
    localStorage.removeItem(REFRESH);
  }
}
```

### 2.2 Servicio

`src/app/core/auth/auth.service.ts`

```ts
import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, firstValueFrom, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SIN_TOAST } from '../http/error.interceptor';
import { BoxOpcion, LoginResponse, Sesion, Tokens } from './auth.models';
import { TokenStorage } from './token.storage';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokens = inject(TokenStorage);
  private readonly router = inject(Router);
  private readonly url = `${environment.apiUrl}/auth`;
  private readonly sinToast = { context: new HttpContext().set(SIN_TOAST, true) };

  readonly sesion = signal<Sesion | null>(null);
  readonly esAdmin = computed(() => this.sesion()?.box.rol === 'ADMIN');

  /** Login con varios boxes: queda pendiente de elegir uno. */
  readonly seleccionPendiente = signal<{ selectionToken: string; boxes: BoxOpcion[] } | null>(null);

  private refrescando$: Observable<Tokens> | null = null;

  get tieneSesionGuardada(): boolean { return !!this.tokens.refresh; }
  get accessToken(): string | null { return this.tokens.access; }

  async login(email: string, password: string): Promise<'listo' | 'elegir-box'> {
    const r = await firstValueFrom(
      this.http.post<LoginResponse>(`${this.url}/login`, { email, password }, this.sinToast));

    if (r.requiereSeleccion) {
      this.seleccionPendiente.set({ selectionToken: r.selectionToken!, boxes: r.boxes! });
      return 'elegir-box';
    }
    await this.iniciar(r as Tokens);
    return 'listo';
  }

  async seleccionarBox(boxPublicId: string): Promise<void> {
    const pendiente = this.seleccionPendiente();
    if (!pendiente) throw new Error('No hay una selección de box pendiente.');

    const tokens = await firstValueFrom(this.http.post<Tokens>(`${this.url}/seleccionar-box`,
      { selectionToken: pendiente.selectionToken, boxPublicId }, this.sinToast));
    this.seleccionPendiente.set(null);
    await this.iniciar(tokens);
  }

  async cambiarBox(boxPublicId: string): Promise<void> {
    const tokens = await firstValueFrom(this.http.post<Tokens>(`${this.url}/cambiar-box`, { boxPublicId }));
    await this.iniciar(tokens);
  }

  /** Al arrancar la app: si hay tokens guardados, recupera la sesión. */
  async restaurar(): Promise<void> {
    if (!this.tieneSesionGuardada) return;
    try {
      await this.cargarSesion();
    } catch {
      this.limpiarLocal();
    }
  }

  /** Una sola renovación aunque fallen varias peticiones a la vez (RC-002). */
  refrescar(): Observable<Tokens> {
    this.refrescando$ ??= this.http
      .post<Tokens>(`${this.url}/refresh`, { refreshToken: this.tokens.refresh }, this.sinToast)
      .pipe(
        tap((t) => this.tokens.guardar(t)),
        finalize(() => (this.refrescando$ = null)),
        shareReplay(1),
      );
    return this.refrescando$;
  }

  async logout(): Promise<void> {
    const refreshToken = this.tokens.refresh;
    if (refreshToken) {
      await firstValueFrom(this.http.post(`${this.url}/logout`, { refreshToken }, this.sinToast)).catch(() => undefined);
    }
    this.limpiarLocal();
    await this.router.navigate(['/login']);
  }

  /** Cuando el refresh ya falló: no hay con qué llamar a la API. */
  expirarSesion(): void {
    this.limpiarLocal();
    void this.router.navigate(['/login']);
  }

  private async iniciar(tokens: Tokens): Promise<void> {
    this.tokens.guardar(tokens);
    await this.cargarSesion();
  }

  private async cargarSesion(): Promise<void> {
    this.sesion.set(await firstValueFrom(this.http.get<Sesion>(`${this.url}/sesion`)));
  }

  private limpiarLocal(): void {
    this.tokens.limpiar();
    this.sesion.set(null);
    this.seleccionPendiente.set(null);
  }
}
```

### 2.3 Interceptor y guards

`src/app/core/auth/auth.interceptor.ts`

```ts
import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

// Endpoints que no llevan access token.
const PUBLICOS = ['/auth/login', '/auth/refresh', '/auth/seleccionar-box'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  if (!req.url.startsWith(environment.apiUrl) || PUBLICOS.some((ruta) => req.url.endsWith(ruta))) {
    return next(req);
  }

  return next(conToken(req, auth.accessToken)).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !auth.tieneSesionGuardada) {
        return throwError(() => error);
      }

      // Access token vencido: renueva una vez y repite la petición original.
      return auth.refrescar().pipe(
        catchError((errorRefresh) => {
          auth.expirarSesion();
          return throwError(() => errorRefresh);
        }),
        switchMap((tokens) => next(conToken(req, tokens.accessToken))),
      );
    }),
  );
};

function conToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}
```

`src/app/core/auth/auth.guards.ts`

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** La sesión ya se restauró en el app initializer. */
export const authGuard: CanActivateFn = () =>
  inject(AuthService).sesion() ? true : inject(Router).createUrlTree(['/login']);

export const adminGuard: CanActivateFn = () =>
  inject(AuthService).esAdmin() ? true : inject(Router).createUrlTree(['/']);

export const invitadoGuard: CanActivateFn = () =>
  inject(AuthService).sesion() ? inject(Router).createUrlTree(['/']) : true;
```

### 2.4 Pantallas

`src/app/features/auth/login.page.ts`

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../core/auth/auth.service';
import { mensajeDeError } from '../../core/http/error.interceptor';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, MessageModule, PasswordModule],
  template: `
    <main class="min-h-screen grid place-items-center p-4">
      <form [formGroup]="form" (ngSubmit)="entrar()"
            class="w-full max-w-sm bg-surface-0 border border-surface-200 rounded-xl p-8 flex flex-col gap-4">
        <h1 class="text-2xl font-semibold">RxBox</h1>
        <p class="text-muted-color -mt-2">Inicia sesión para administrar tu box.</p>

        @if (error()) { <p-message severity="error">{{ error() }}</p-message> }

        <label class="flex flex-col gap-1">
          <span class="text-xs font-semibold uppercase tracking-widest text-muted-color">Email</span>
          <input pInputText type="email" formControlName="email" autocomplete="username" />
        </label>

        <label class="flex flex-col gap-1">
          <span class="text-xs font-semibold uppercase tracking-widest text-muted-color">Contraseña</span>
          <p-password formControlName="password" [feedback]="false" [toggleMask]="true" fluid
                      autocomplete="current-password" />
        </label>

        <p-button type="submit" label="Entrar" [loading]="cargando()" styleClass="w-full" />
      </form>
    </main>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected async entrar(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.cargando.set(true);
    this.error.set(null);
    try {
      const { email, password } = this.form.getRawValue();
      const resultado = await this.auth.login(email, password);
      await this.router.navigate([resultado === 'elegir-box' ? '/seleccionar-box' : '/']);
    } catch (e) {
      this.error.set(e instanceof HttpErrorResponse ? mensajeDeError(e) : 'Ocurrió un error inesperado.');
    } finally {
      this.cargando.set(false);
    }
  }
}
```

`src/app/features/auth/seleccionar-box.page.ts`

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { AuthService } from '../../core/auth/auth.service';
import { mensajeDeError } from '../../core/http/error.interceptor';

@Component({
  selector: 'app-seleccionar-box',
  imports: [ButtonModule, MessageModule],
  template: `
    <main class="min-h-screen grid place-items-center p-4">
      <section class="w-full max-w-sm bg-surface-0 border border-surface-200 rounded-xl p-8 flex flex-col gap-3">
        <h1 class="text-xl font-semibold">¿A qué box entras?</h1>
        @if (error()) { <p-message severity="error">{{ error() }}</p-message> }

        @for (box of auth.seleccionPendiente()?.boxes; track box.publicId) {
          <button type="button" (click)="elegir(box.publicId)" [disabled]="eligiendo()"
                  class="text-left border border-surface-200 rounded-lg p-4 hover:border-primary hover:bg-primary/5 transition">
            <span class="block font-medium">{{ box.nombre }}</span>
            <span class="text-xs text-muted-color">{{ box.rol === 'ADMIN' ? 'Administrador' : 'Coach' }}</span>
          </button>
        }

        <p-button label="Volver" [text]="true" (onClick)="volver()" />
      </section>
    </main>
  `,
})
export class SeleccionarBoxPage implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly eligiendo = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    // Recarga de página o entrada directa: el selectionToken solo vive en memoria.
    if (!this.auth.seleccionPendiente()) void this.router.navigate(['/login']);
  }

  protected async elegir(boxPublicId: string): Promise<void> {
    this.eligiendo.set(true);
    try {
      await this.auth.seleccionarBox(boxPublicId);
      await this.router.navigate(['/']);
    } catch (e) {
      // El selectionToken dura 5 minutos.
      this.error.set(e instanceof HttpErrorResponse ? mensajeDeError(e) : 'Ocurrió un error inesperado.');
    } finally {
      this.eligiendo.set(false);
    }
  }

  protected volver(): void {
    this.auth.seleccionPendiente.set(null);
    void this.router.navigate(['/login']);
  }
}
```

### 2.5 Rutas (versión de esta etapa)

`src/app/app.routes.ts`

```ts
import { Routes } from '@angular/router';
import { adminGuard, authGuard, invitadoGuard } from './core/auth/auth.guards';

export const routes: Routes = [
  { path: 'login', canActivate: [invitadoGuard], loadComponent: () => import('./features/auth/login.page').then((m) => m.LoginPage) },
  { path: 'seleccionar-box', loadComponent: () => import('./features/auth/seleccionar-box.page').then((m) => m.SeleccionarBoxPage) },
  {
    path: '',
    // Lazy también: así PrimeNG del layout no infla el bundle inicial que carga el login.
    loadComponent: () => import('./layout/app-layout').then((m) => m.AppLayout),
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./features/inicio/inicio.page').then((m) => m.InicioPage) },
      // Etapa 4: catalogos · Etapa 5: personas
    ],
  },
  { path: '**', redirectTo: '' },
];
```

`AppLayout` e `InicioPage` llegan en la etapa 3; para probar esta etapa sola, comenta esa ruta y deja una página temporal.

> `ng build` avisa que el bundle inicial pasa de 500 kB (el tema de PrimeNG y los toasts van en él). Es un aviso, no un error. Si molesta, sube el `maximumWarning` de `initial` a `1mb` en `angular.json`.

### 2.6 Prueba manual

1. Usuario del seed (`admin@rxbox.local`) → entra directo.
2. Password incorrecto → mensaje "Email o contraseña incorrectos." dentro del formulario, sin toast.
3. En DevTools → Application → Local Storage, cambia el valor de `rxbox.access` por basura y navega: la petición falla con 401, se renueva sola y sigue funcionando.
4. Cambia también `rxbox.refresh` por basura: te manda a `/login`.

**Commit:** `feat(cliente): login, selección de box y renovación de sesión`

---

## Etapa 3 — Layout

### 3.1 Layout

`src/app/layout/app-layout.ts`

```ts
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { BoxOpcion } from '../core/auth/auth.models';
import { AuthService } from '../core/auth/auth.service';

interface ItemMenu { etiqueta: string; icono: string; ruta: string; soloAdmin?: boolean; }

const MENU: ItemMenu[] = [
  { etiqueta: 'Inicio', icono: 'pi pi-home', ruta: '/' },
  { etiqueta: 'Personas', icono: 'pi pi-users', ruta: '/personas' },
  { etiqueta: 'Catálogos', icono: 'pi pi-list', ruta: '/catalogos', soloAdmin: true },
];

@Component({
  selector: 'app-layout',
  imports: [FormsModule, RouterOutlet, RouterLink, RouterLinkActive, ButtonModule, SelectModule],
  template: `
    <div class="min-h-screen flex">
      <aside class="w-60 shrink-0 bg-surface-0 border-r border-surface-200 p-4 hidden md:flex flex-col gap-1">
        <span class="text-xl font-bold text-primary px-3 py-2">RxBox</span>
        @for (item of menu(); track item.ruta) {
          <a [routerLink]="item.ruta" routerLinkActive="bg-primary/10 text-primary font-medium"
             [routerLinkActiveOptions]="{ exact: item.ruta === '/' }"
             class="flex items-center gap-3 rounded-lg px-3 py-2 hover:bg-surface-100">
            <i [class]="item.icono"></i> {{ item.etiqueta }}
          </a>
        }
      </aside>

      <div class="flex-1 flex flex-col min-w-0">
        <header class="h-16 bg-surface-0 border-b border-surface-200 px-4 flex items-center gap-3">
          @if (sesion(); as s) {
            @if (s.boxes.length > 1) {
              <p-select [options]="s.boxes" optionLabel="nombre" [ngModel]="boxActual()"
                        (onChange)="cambiarBox($event.value)" [disabled]="cambiando()" />
            } @else {
              <span class="font-medium">{{ s.box.nombre }}</span>
            }
            <span class="text-xs text-muted-color uppercase tracking-widest">
              {{ s.box.rol === 'ADMIN' ? 'Administrador' : 'Coach' }}
            </span>
            <span class="ml-auto text-sm text-muted-color hidden sm:block">{{ s.email }}</span>
            <p-button icon="pi pi-sign-out" [text]="true" [rounded]="true" ariaLabel="Salir" (onClick)="auth.logout()" />
          }
        </header>

        <!-- Menú móvil -->
        <nav class="md:hidden flex gap-1 overflow-x-auto bg-surface-0 border-b border-surface-200 px-2">
          @for (item of menu(); track item.ruta) {
            <a [routerLink]="item.ruta" routerLinkActive="text-primary border-primary"
               [routerLinkActiveOptions]="{ exact: item.ruta === '/' }"
               class="px-3 py-2 border-b-2 border-transparent whitespace-nowrap">{{ item.etiqueta }}</a>
          }
        </nav>

        <main class="flex-1 p-4 md:p-6">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class AppLayout {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly sesion = this.auth.sesion;
  protected readonly menu = computed(() => MENU.filter((i) => !i.soloAdmin || this.auth.esAdmin()));
  // Misma referencia que en s.boxes para que p-select la marque como seleccionada.
  protected readonly boxActual = computed(() => {
    const s = this.sesion();
    return s?.boxes.find((b) => b.publicId === s.box.publicId) ?? null;
  });
  protected readonly cambiando = signal(false);

  protected async cambiarBox(box: BoxOpcion): Promise<void> {
    if (!box || box.publicId === this.sesion()?.box.publicId) return;
    this.cambiando.set(true);
    try {
      await this.auth.cambiarBox(box.publicId);
      // Regresa a inicio: las pantallas abiertas tenían datos del box anterior.
      await this.router.navigateByUrl('/');
    } finally {
      this.cambiando.set(false);
    }
  }
}
```

### 3.2 Inicio

`src/app/features/inicio/inicio.page.ts`

```ts
import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-inicio',
  template: `
    <h1 class="text-2xl font-semibold mb-2">Hola</h1>
    <p class="text-muted-color">Estás en {{ auth.sesion()?.box?.nombre }}. Las métricas llegan con el módulo 09.</p>
  `,
})
export class InicioPage {
  protected readonly auth = inject(AuthService);
}
```

### 3.3 Prueba manual

Con un usuario de dos boxes (crea un segundo `usuario_box` en la BD local o usa los tests como guía): el selector del encabezado cambia de box sin pedir contraseña y el nombre del box se actualiza. Con un Coach, el menú no muestra Catálogos y `/catalogos` regresa a inicio.

**Commit:** `feat(cliente): layout con cambio de box`

---

## Etapa 4 — Catálogos

### 4.1 Servicio

`src/app/core/catalogos/catalogo.models.ts`

```ts
export type TipoCatalogo = 'CATEGORIA_MEMBRESIA' | 'METODO_PAGO';

export interface Catalogo {
  publicId: string;
  tipo: TipoCatalogo;
  nombre: string;
  orden: number;
  activo: boolean;
}

/** Valor de un catálogo fijo (enum del backend). */
export interface CatalogoFijo {
  valor: string;
  descripcion: string;
}
```

`src/app/core/catalogos/catalogos.service.ts`

```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Catalogo, CatalogoFijo, TipoCatalogo } from './catalogo.models';

@Injectable({ providedIn: 'root' })
export class CatalogosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/catalogos`;
  private readonly fijos = new Map<string, Observable<CatalogoFijo[]>>();

  listar(tipo: TipoCatalogo, soloActivos = true): Observable<Catalogo[]> {
    return this.http.get<Catalogo[]>(this.url, { params: { tipo, soloActivos } });
  }

  crear(body: { tipo: TipoCatalogo; nombre: string; orden: number | null }): Observable<Catalogo> {
    return this.http.post<Catalogo>(this.url, body);
  }

  editar(publicId: string, body: { nombre: string; orden: number | null }): Observable<Catalogo> {
    return this.http.put<Catalogo>(`${this.url}/${publicId}`, body);
  }

  cambiarEstado(publicId: string): Observable<Catalogo> {
    return this.http.patch<Catalogo>(`${this.url}/${publicId}/toggle`, {});
  }

  /** Catálogo fijo con caché: los enums no cambian mientras la app está abierta (RC-006). */
  fijo(nombre: string): Observable<CatalogoFijo[]> {
    let valores = this.fijos.get(nombre);
    if (!valores) {
      valores = this.http.get<CatalogoFijo[]>(`${environment.apiUrl}/catalogos-fijos/${nombre}`).pipe(shareReplay(1));
      this.fijos.set(nombre, valores);
    }
    return valores;
  }
}
```

### 4.2 Pantalla

`src/app/features/catalogos/catalogos.page.ts`. Pestañas por tipo (llenadas desde el catálogo fijo `tipos-catalogo`), tabla con todos los elementos y diálogo para agregar o editar.

```ts
import { Component, OnInit, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { firstValueFrom } from 'rxjs';
import { Catalogo, TipoCatalogo } from '../../core/catalogos/catalogo.models';
import { CatalogosService } from '../../core/catalogos/catalogos.service';

@Component({
  selector: 'app-catalogos',
  imports: [FormsModule, ReactiveFormsModule, ButtonModule, DialogModule, InputNumberModule, InputTextModule,
            SelectButtonModule, TableModule, TagModule],
  template: `
    <div class="flex flex-wrap items-center gap-3 mb-4">
      <h1 class="text-2xl font-semibold">Catálogos</h1>
      <p-button class="ml-auto" label="Agregar" icon="pi pi-plus" (onClick)="abrir(null)" />
    </div>

    <p-selectbutton [options]="tipos()" optionLabel="descripcion" optionValue="valor"
                    [ngModel]="tipo()" (ngModelChange)="cambiarTipo($event)" [allowEmpty]="false" class="mb-4" />

    <div class="bg-surface-0 border border-surface-200 rounded-xl">
      <p-table [value]="items()" [loading]="cargando()">
        <ng-template #header>
          <tr><th class="w-24">Orden</th><th>Nombre</th><th class="w-32">Estado</th><th class="w-32"></th></tr>
        </ng-template>
        <ng-template #body let-c>
          <tr>
            <td>{{ c.orden }}</td>
            <td>{{ c.nombre }}</td>
            <td><p-tag [value]="c.activo ? 'Activo' : 'Inactivo'" [severity]="c.activo ? 'success' : 'secondary'" /></td>
            <td class="text-right whitespace-nowrap">
              <p-button icon="pi pi-pencil" [text]="true" [rounded]="true" ariaLabel="Editar" (onClick)="abrir(c)" />
              <p-button [icon]="c.activo ? 'pi pi-ban' : 'pi pi-check'" [text]="true" [rounded]="true"
                        [severity]="c.activo ? 'danger' : 'success'" [ariaLabel]="c.activo ? 'Desactivar' : 'Activar'"
                        (onClick)="cambiarEstado(c)" />
            </td>
          </tr>
        </ng-template>
        <ng-template #emptymessage><tr><td colspan="4">Sin elementos.</td></tr></ng-template>
      </p-table>
    </div>

    <p-dialog [header]="editando() ? 'Editar' : 'Agregar'" [(visible)]="dialogo" [modal]="true" [style]="{ width: '24rem' }">
      <form [formGroup]="form" (ngSubmit)="guardar()" class="flex flex-col gap-4 pt-2">
        <label class="flex flex-col gap-1">
          <span class="text-xs font-semibold uppercase tracking-widest text-muted-color">Nombre</span>
          <input pInputText formControlName="nombre" maxlength="60" />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-xs font-semibold uppercase tracking-widest text-muted-color">Orden (opcional)</span>
          <p-inputnumber formControlName="orden" [min]="0" [useGrouping]="false" fluid />
        </label>
        <div class="flex justify-end gap-2">
          <p-button label="Cancelar" [text]="true" (onClick)="dialogo.set(false)" />
          <p-button type="submit" label="Guardar" [loading]="guardando()" [disabled]="form.invalid" />
        </div>
      </form>
    </p-dialog>
  `,
})
export class CatalogosPage implements OnInit {
  private readonly service = inject(CatalogosService);
  private readonly confirm = inject(ConfirmationService);

  protected readonly tipos = toSignal(this.service.fijo('tipos-catalogo'), { initialValue: [] });
  protected readonly tipo = signal<TipoCatalogo>('METODO_PAGO');
  protected readonly items = signal<Catalogo[]>([]);
  protected readonly cargando = signal(false);
  protected readonly guardando = signal(false);
  protected readonly editando = signal<Catalogo | null>(null);
  // Signals, no campos simples: la app es zoneless y estos cambian después de un await.
  protected readonly dialogo = signal(false);

  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly form = this.fb.group({
    nombre: ['', [Validators.required, Validators.maxLength(60)]],
    orden: this.fb.control<number | null>(null),
  });

  ngOnInit(): void { void this.cargar(); }

  protected cambiarTipo(tipo: TipoCatalogo): void {
    this.tipo.set(tipo);
    void this.cargar();
  }

  protected abrir(catalogo: Catalogo | null): void {
    this.editando.set(catalogo);
    this.form.reset({ nombre: catalogo?.nombre ?? '', orden: catalogo?.orden ?? null });
    this.dialogo.set(true);
  }

  protected async guardar(): Promise<void> {
    const { nombre, orden } = this.form.getRawValue();
    const actual = this.editando();
    this.guardando.set(true);
    try {
      await firstValueFrom(actual
        ? this.service.editar(actual.publicId, { nombre, orden })
        : this.service.crear({ tipo: this.tipo(), nombre, orden }));
      this.dialogo.set(false);
      await this.cargar();
    } catch {
      // El interceptor ya mostró el mensaje (ej. 409 nombre repetido).
    } finally {
      this.guardando.set(false);
    }
  }

  protected cambiarEstado(c: Catalogo): void {
    const ejecutar = async () => {
      try {
        await firstValueFrom(this.service.cambiarEstado(c.publicId));
        await this.cargar();
      } catch { /* toast del interceptor (ej. 409 último activo) */ }
    };
    if (!c.activo) { void ejecutar(); return; }

    this.confirm.confirm({
      header: 'Desactivar',
      message: `"${c.nombre}" ya no aparecerá en las listas. Los registros que ya lo usan no cambian.`,
      acceptButtonProps: { label: 'Desactivar', severity: 'danger' },
      rejectButtonProps: { label: 'Cancelar', text: true },
      accept: ejecutar,
    });
  }

  private async cargar(): Promise<void> {
    this.cargando.set(true);
    try {
      // Mantenimiento: activos e inactivos (RF-CAT-001 RN-001).
      this.items.set(await firstValueFrom(this.service.listar(this.tipo(), false)));
    } finally {
      this.cargando.set(false);
    }
  }
}
```

Agrega la ruta dentro de `children` en `app.routes.ts`:

```ts
      { path: 'catalogos', canActivate: [adminGuard], loadComponent: () => import('./features/catalogos/catalogos.page').then((m) => m.CatalogosPage) },
```

### 4.3 Prueba manual

- Pestañas "Método de pago" y "Categoría de membresía" (vienen del backend, no escritas en el front).
- Agregar "Mercado Pago" → aparece. Repetirlo → toast amarillo "Ya existe 'Mercado Pago'…".
- Desactivar el único método activo → toast "Debe quedar al menos un elemento activo…".

**Commit:** `feat(cliente): mantenimiento de catálogos`

---

## Etapa 5 — Personas

### 5.1 Modelos y servicio

`src/app/features/personas/persona.models.ts`

```ts
export type TipoPersona = 'ATLETA' | 'COACH';

export interface ContactoEmergencia {
  publicId: string;
  nombre: string;
  parentesco: string | null;
  telefono: string;
}

export interface PersonaItem {
  publicId: string;
  codigo: string;
  nombre: string;
  apellidos: string;
  email: string | null;
  telefono: string | null;
  esAtleta: boolean;
  esCoach: boolean;
  activo: boolean;
}

export interface Persona extends PersonaItem {
  fechaCreacion: string;
  contactos: ContactoEmergencia[];
}

export interface PersonaFiltro {
  buscar?: string;
  tipo?: TipoPersona | null;
  soloActivos: boolean;
  pagina: number;
  tamano: number;
}

export interface PersonaDatos {
  nombre: string;
  apellidos: string;
  email: string | null;
  telefono: string | null;
  esAtleta: boolean;
  esCoach: boolean;
}

export interface ContactoDatos {
  nombre: string;
  parentesco: string | null;
  telefono: string;
}
```

`src/app/features/personas/personas.service.ts`

```ts
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Pagina } from '../../core/api.models';
import { ContactoDatos, ContactoEmergencia, Persona, PersonaDatos, PersonaFiltro, PersonaItem } from './persona.models';

@Injectable({ providedIn: 'root' })
export class PersonasService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/personas`;

  listar(f: PersonaFiltro): Observable<Pagina<PersonaItem>> {
    let params = new HttpParams()
      .set('soloActivos', f.soloActivos)
      .set('pagina', f.pagina)
      .set('tamano', f.tamano);
    if (f.buscar?.trim()) params = params.set('buscar', f.buscar.trim());
    if (f.tipo) params = params.set('tipo', f.tipo);
    return this.http.get<Pagina<PersonaItem>>(this.url, { params });
  }

  obtener(publicId: string): Observable<Persona> {
    return this.http.get<Persona>(`${this.url}/${publicId}`);
  }

  crear(body: PersonaDatos & { contactos: ContactoDatos[] }): Observable<Persona> {
    return this.http.post<Persona>(this.url, body);
  }

  editar(publicId: string, body: PersonaDatos): Observable<Persona> {
    return this.http.put<Persona>(`${this.url}/${publicId}`, body);
  }

  cambiarEstado(publicId: string): Observable<Persona> {
    return this.http.patch<Persona>(`${this.url}/${publicId}/toggle`, {});
  }

  agregarContacto(publicId: string, body: ContactoDatos): Observable<ContactoEmergencia> {
    return this.http.post<ContactoEmergencia>(`${this.url}/${publicId}/contactos`, body);
  }

  editarContacto(publicId: string, contactoId: string, body: ContactoDatos): Observable<ContactoEmergencia> {
    return this.http.put<ContactoEmergencia>(`${this.url}/${publicId}/contactos/${contactoId}`, body);
  }

  eliminarContacto(publicId: string, contactoId: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${publicId}/contactos/${contactoId}`);
  }
}
```

### 5.2 Formulario de alta y edición

`src/app/features/personas/persona-form.dialog.ts`. En alta permite hasta 3 contactos; en edición los contactos se manejan desde el detalle.

```ts
import { Component, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { firstValueFrom } from 'rxjs';
import { Persona, PersonaDatos } from './persona.models';
import { PersonasService } from './personas.service';

const TELEFONO = /^\d{10}$/;
const MAX_CONTACTOS = 3;

type ContactoForm = FormGroup<{
  nombre: FormControl<string>;
  parentesco: FormControl<string>;
  telefono: FormControl<string>;
}>;

/** RF-PER-002 CA-002: atleta, coach o ambos. */
function rolRequerido(grupo: AbstractControl): ValidationErrors | null {
  const { esAtleta, esCoach } = grupo.value as { esAtleta: boolean; esCoach: boolean };
  return esAtleta || esCoach ? null : { sinRol: true };
}

/** RC-005: vacío → null. */
const opcional = (valor: string) => valor.trim() || null;

@Component({
  selector: 'app-persona-form',
  imports: [ReactiveFormsModule, ButtonModule, CheckboxModule, DialogModule, InputTextModule, MessageModule],
  template: `
    <p-dialog [header]="persona() ? 'Editar persona' : 'Agregar persona'" [(visible)]="visible"
              [modal]="true" [style]="{ width: '36rem' }" [breakpoints]="{ '640px': '95vw' }">
      <form [formGroup]="form" (ngSubmit)="guardar()" class="flex flex-col gap-4 pt-2">
        <div class="grid sm:grid-cols-2 gap-4">
          <label class="flex flex-col gap-1">
            <span class="etiqueta">Nombre</span>
            <input pInputText formControlName="nombre" maxlength="80" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="etiqueta">Apellidos</span>
            <input pInputText formControlName="apellidos" maxlength="120" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="etiqueta">Email (opcional)</span>
            <input pInputText type="email" formControlName="email" maxlength="150" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="etiqueta">Teléfono (opcional)</span>
            <input pInputText formControlName="telefono" inputmode="numeric" maxlength="10" placeholder="10 dígitos" />
          </label>
        </div>

        <div class="flex gap-6">
          <label class="flex items-center gap-2"><p-checkbox formControlName="esAtleta" [binary]="true" /> Atleta</label>
          <label class="flex items-center gap-2"><p-checkbox formControlName="esCoach" [binary]="true" /> Coach</label>
        </div>
        @if (form.hasError('sinRol')) {
          <p-message severity="warn" size="small">Marca si es atleta, coach o ambos.</p-message>
        }

        @if (!persona()) {
          <section class="flex flex-col gap-3" formArrayName="contactos">
            <div class="flex items-center">
              <span class="etiqueta">Contactos de emergencia</span>
              @if (contactos.length < maxContactos) {
                <p-button class="ml-auto" label="Agregar contacto" icon="pi pi-plus" [text]="true" size="small"
                          (onClick)="agregarContacto()" />
              }
            </div>
            @for (contacto of contactos.controls; track contacto; let i = $index) {
              <div [formGroupName]="i" class="grid grid-cols-[1fr_8rem_9rem_auto] gap-2 items-center">
                <input pInputText formControlName="nombre" placeholder="Nombre" maxlength="120" />
                <input pInputText formControlName="parentesco" placeholder="Parentesco" maxlength="40" />
                <input pInputText formControlName="telefono" placeholder="Teléfono" inputmode="numeric" maxlength="10" />
                <p-button icon="pi pi-times" [text]="true" severity="secondary" ariaLabel="Quitar"
                          (onClick)="contactos.removeAt(i)" />
              </div>
            }
          </section>
        }

        <div class="flex justify-end gap-2">
          <p-button label="Cancelar" [text]="true" (onClick)="visible.set(false)" />
          <p-button type="submit" label="Guardar" [loading]="guardando()" [disabled]="form.invalid" />
        </div>
      </form>
    </p-dialog>
  `,
  styles: `.etiqueta { font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.1em; color: var(--p-text-muted-color); }`,
})
export class PersonaFormDialog {
  private readonly service = inject(PersonasService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly visible = model(false);
  /** null = alta. */
  readonly persona = input<Persona | null>(null);
  readonly guardada = output<Persona>();

  protected readonly guardando = signal(false);
  protected readonly maxContactos = MAX_CONTACTOS;

  protected readonly form = this.fb.group({
    nombre: ['', [Validators.required, Validators.maxLength(80)]],
    apellidos: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    telefono: ['', Validators.pattern(TELEFONO)],
    esAtleta: [true],
    esCoach: [false],
    contactos: this.fb.array<ContactoForm>([]),
  }, { validators: rolRequerido });

  protected get contactos() { return this.form.controls.contactos; }

  constructor() {
    // Cada vez que se abre, carga la persona a editar o limpia para un alta.
    effect(() => {
      if (!this.visible()) return;
      const p = this.persona();
      untracked(() => {
        this.contactos.clear();
        this.form.reset({
          nombre: p?.nombre ?? '', apellidos: p?.apellidos ?? '',
          email: p?.email ?? '', telefono: p?.telefono ?? '',
          esAtleta: p?.esAtleta ?? true, esCoach: p?.esCoach ?? false,
        });
      });
    });
  }

  protected agregarContacto(): void {
    this.contactos.push(this.fb.group({
      nombre: ['', [Validators.required, Validators.maxLength(120)]],
      parentesco: ['', Validators.maxLength(40)],
      telefono: ['', [Validators.required, Validators.pattern(TELEFONO)]],
    }));
  }

  protected async guardar(): Promise<void> {
    const v = this.form.getRawValue();
    const datos: PersonaDatos = {
      nombre: v.nombre, apellidos: v.apellidos,
      email: opcional(v.email), telefono: opcional(v.telefono),
      esAtleta: v.esAtleta, esCoach: v.esCoach,
    };
    const actual = this.persona();

    this.guardando.set(true);
    try {
      const guardada = await firstValueFrom(actual
        ? this.service.editar(actual.publicId, datos)
        : this.service.crear({
            ...datos,
            contactos: v.contactos.map((c) => ({ nombre: c.nombre, parentesco: opcional(c.parentesco), telefono: c.telefono })),
          }));
      this.guardada.emit(guardada);
      this.visible.set(false);
    } catch {
      // toast del interceptor
    } finally {
      this.guardando.set(false);
    }
  }
}
```

### 5.3 Listado

`src/app/features/personas/personas.page.ts`. Paginación, búsqueda y filtros en el servidor.

```ts
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { Subject, debounceTime, distinctUntilChanged, firstValueFrom } from 'rxjs';
import { Pagina } from '../../core/api.models';
import { AuthService } from '../../core/auth/auth.service';
import { CatalogosService } from '../../core/catalogos/catalogos.service';
import { PersonaFormDialog } from './persona-form.dialog';
import { PersonaItem, TipoPersona } from './persona.models';
import { PersonasService } from './personas.service';

@Component({
  selector: 'app-personas',
  imports: [FormsModule, ButtonModule, CheckboxModule, IconFieldModule, InputIconModule, InputTextModule,
            SelectModule, TableModule, TagModule, PersonaFormDialog],
  template: `
    <div class="flex flex-wrap items-center gap-3 mb-4">
      <h1 class="text-2xl font-semibold">Personas</h1>
      @if (auth.esAdmin()) {
        <p-button class="ml-auto" label="Agregar" icon="pi pi-plus" (onClick)="formulario.set(true)" />
      }
    </div>

    <div class="flex flex-wrap items-center gap-3 mb-4">
      <p-iconfield class="w-full sm:w-80">
        <p-inputicon class="pi pi-search" />
        <input pInputText class="w-full" placeholder="Nombre, código, email o teléfono"
               [ngModel]="buscar()" (ngModelChange)="busqueda$.next($event)" />
      </p-iconfield>
      <p-select [options]="tipos()" optionLabel="descripcion" optionValue="valor" placeholder="Todos"
                [showClear]="true" [ngModel]="tipo()" (ngModelChange)="tipo.set($event); recargar()" class="w-40" />
      <label class="flex items-center gap-2 text-sm">
        <p-checkbox [binary]="true" [ngModel]="!soloActivos()" (ngModelChange)="soloActivos.set(!$event); recargar()" />
        Incluir inactivos
      </label>
    </div>

    <div class="bg-surface-0 border border-surface-200 rounded-xl">
      <p-table [value]="pagina()?.items ?? []" [lazy]="true" (onLazyLoad)="alPaginar($event)"
               [paginator]="true" [rows]="tamano" [rowsPerPageOptions]="[20, 50, 100]" [first]="primero()"
               [totalRecords]="pagina()?.total ?? 0" [loading]="cargando()" [scrollable]="true">
        <ng-template #header>
          <tr><th>Código</th><th>Nombre</th><th>Teléfono</th><th>Rol</th><th>Estado</th><th></th></tr>
        </ng-template>
        <ng-template #body let-p>
          <tr>
            <td class="font-mono">{{ p.codigo }}</td>
            <td style="min-width: 12rem">{{ p.nombre }} {{ p.apellidos }}</td>
            <td>{{ p.telefono ?? '—' }}</td>
            <td class="whitespace-nowrap">
              @if (p.esAtleta) { <p-tag value="Atleta" severity="info" class="mr-1" /> }
              @if (p.esCoach) { <p-tag value="Coach" severity="contrast" /> }
            </td>
            <td><p-tag [value]="p.activo ? 'Activo' : 'Inactivo'" [severity]="p.activo ? 'success' : 'secondary'" /></td>
            <td class="text-right whitespace-nowrap">
              <p-button icon="pi pi-eye" [text]="true" [rounded]="true" ariaLabel="Ver" (onClick)="ver(p)" />
              @if (auth.esAdmin()) {
                <p-button [icon]="p.activo ? 'pi pi-ban' : 'pi pi-check'" [text]="true" [rounded]="true"
                          [severity]="p.activo ? 'danger' : 'success'" [ariaLabel]="p.activo ? 'Desactivar' : 'Activar'"
                          (onClick)="cambiarEstado(p)" />
              }
            </td>
          </tr>
        </ng-template>
        <ng-template #emptymessage><tr><td colspan="6">Sin personas con esos filtros.</td></tr></ng-template>
      </p-table>
    </div>

    <app-persona-form [(visible)]="formulario" (guardada)="ver($event)" />
  `,
})
export class PersonasPage {
  protected readonly auth = inject(AuthService);
  private readonly service = inject(PersonasService);
  private readonly confirm = inject(ConfirmationService);
  private readonly router = inject(Router);

  protected readonly tipos = toSignal(inject(CatalogosService).fijo('tipos-persona'), { initialValue: [] });
  protected readonly pagina = signal<Pagina<PersonaItem> | null>(null);
  protected readonly cargando = signal(false);
  protected readonly buscar = signal('');
  protected readonly tipo = signal<TipoPersona | null>(null);
  protected readonly soloActivos = signal(true);
  protected readonly primero = signal(0);
  protected readonly formulario = signal(false);
  protected tamano = 20;

  protected readonly busqueda$ = new Subject<string>();

  constructor() {
    this.busqueda$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(inject(DestroyRef)))
      .subscribe((texto) => {
        this.buscar.set(texto);
        this.recargar();
      });
  }

  /** p-table lo llama al iniciar y al cambiar de página o de tamaño. */
  protected alPaginar(e: TableLazyLoadEvent): void {
    this.primero.set(e.first ?? 0);
    this.tamano = e.rows ?? this.tamano;
    void this.cargar();
  }

  /** Filtro nuevo: vuelve a la primera página. */
  protected recargar(): void {
    this.primero.set(0);
    void this.cargar();
  }

  protected ver(p: { publicId: string }): void {
    void this.router.navigate(['/personas', p.publicId]);
  }

  protected cambiarEstado(p: PersonaItem): void {
    const ejecutar = async () => {
      try {
        await firstValueFrom(this.service.cambiarEstado(p.publicId));
        await this.cargar();
      } catch { /* toast (ej. 409: no puedes desactivarte a ti mismo) */ }
    };
    if (!p.activo) { void ejecutar(); return; }

    this.confirm.confirm({
      header: 'Desactivar persona',
      message: `¿Desactivar a ${p.nombre} ${p.apellidos}? Si tiene acceso al panel, también lo perderá en este box.`,
      acceptButtonProps: { label: 'Desactivar', severity: 'danger' },
      rejectButtonProps: { label: 'Cancelar', text: true },
      accept: ejecutar,
    });
  }

  private async cargar(): Promise<void> {
    this.cargando.set(true);
    try {
      this.pagina.set(await firstValueFrom(this.service.listar({
        buscar: this.buscar(), tipo: this.tipo(), soloActivos: this.soloActivos(),
        pagina: Math.floor(this.primero() / this.tamano) + 1, tamano: this.tamano,
      })));
    } catch {
      // toast
    } finally {
      this.cargando.set(false);
    }
  }
}
```

### 5.4 Detalle con contactos

`src/app/features/personas/persona-detalle.page.ts`. El `publicId` llega como input gracias a `withComponentInputBinding()`.

```ts
import { DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { PersonaFormDialog } from './persona-form.dialog';
import { ContactoEmergencia, Persona } from './persona.models';
import { PersonasService } from './personas.service';

@Component({
  selector: 'app-persona-detalle',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, ButtonModule, DialogModule, InputTextModule,
            TableModule, TagModule, PersonaFormDialog],
  template: `
    <a routerLink="/personas" class="text-sm text-muted-color hover:text-primary"><i class="pi pi-arrow-left"></i> Personas</a>

    @if (persona(); as p) {
      <section class="bg-surface-0 border border-surface-200 rounded-xl p-6 mt-3 flex flex-wrap gap-6">
        <div class="flex-1 min-w-60">
          <h1 class="text-2xl font-semibold">{{ p.nombre }} {{ p.apellidos }}</h1>
          <div class="flex gap-1 mt-2">
            @if (p.esAtleta) { <p-tag value="Atleta" severity="info" /> }
            @if (p.esCoach) { <p-tag value="Coach" severity="contrast" /> }
            <p-tag [value]="p.activo ? 'Activo' : 'Inactivo'" [severity]="p.activo ? 'success' : 'secondary'" />
          </div>
          <dl class="grid grid-cols-[8rem_1fr] gap-y-2 mt-4 text-sm">
            <dt class="text-muted-color">Email</dt><dd>{{ p.email ?? '—' }}</dd>
            <dt class="text-muted-color">Teléfono</dt><dd>{{ p.telefono ?? '—' }}</dd>
            <dt class="text-muted-color">Alta</dt><dd>{{ p.fechaCreacion | date: 'longDate' }}</dd>
          </dl>
        </div>
        <!-- El código es lo que el atleta teclea en el check-in. -->
        <div class="text-center">
          <span class="block text-xs font-semibold uppercase tracking-widest text-muted-color">Código</span>
          <span class="block font-mono text-4xl font-bold tracking-widest text-primary">{{ p.codigo }}</span>
        </div>
        @if (auth.esAdmin()) {
          <div class="w-full flex justify-end">
            <p-button label="Editar" icon="pi pi-pencil" [outlined]="true" (onClick)="formulario.set(true)" />
          </div>
        }
      </section>

      <section class="bg-surface-0 border border-surface-200 rounded-xl mt-4">
        <div class="flex items-center p-4">
          <h2 class="font-semibold">Contactos de emergencia</h2>
          @if (auth.esAdmin() && p.contactos.length < 3) {
            <p-button class="ml-auto" label="Agregar" icon="pi pi-plus" [text]="true" (onClick)="abrirContacto(null)" />
          }
        </div>
        <p-table [value]="p.contactos">
          <ng-template #header><tr><th>Nombre</th><th>Parentesco</th><th>Teléfono</th><th></th></tr></ng-template>
          <ng-template #body let-c>
            <tr>
              <td>{{ c.nombre }}</td>
              <td>{{ c.parentesco ?? '—' }}</td>
              <td><a [href]="'tel:' + c.telefono" class="text-primary">{{ c.telefono }}</a></td>
              <td class="text-right whitespace-nowrap">
                @if (auth.esAdmin()) {
                  <p-button icon="pi pi-pencil" [text]="true" [rounded]="true" ariaLabel="Editar" (onClick)="abrirContacto(c)" />
                  <p-button icon="pi pi-trash" [text]="true" [rounded]="true" severity="danger" ariaLabel="Eliminar"
                            (onClick)="eliminarContacto(c)" />
                }
              </td>
            </tr>
          </ng-template>
          <ng-template #emptymessage><tr><td colspan="4">Sin contactos de emergencia.</td></tr></ng-template>
        </p-table>
      </section>

      <app-persona-form [(visible)]="formulario" [persona]="p" (guardada)="persona.set($event)" />
    }

    <p-dialog [header]="contactoEditando() ? 'Editar contacto' : 'Agregar contacto'" [(visible)]="dialogoContacto"
              [modal]="true" [style]="{ width: '26rem' }">
      <form [formGroup]="contactoForm" (ngSubmit)="guardarContacto()" class="flex flex-col gap-3 pt-2">
        <input pInputText formControlName="nombre" placeholder="Nombre" maxlength="120" />
        <input pInputText formControlName="parentesco" placeholder="Parentesco (opcional)" maxlength="40" />
        <input pInputText formControlName="telefono" placeholder="Teléfono (10 dígitos)" inputmode="numeric" maxlength="10" />
        <div class="flex justify-end gap-2">
          <p-button label="Cancelar" [text]="true" (onClick)="dialogoContacto.set(false)" />
          <p-button type="submit" label="Guardar" [loading]="guardando()" [disabled]="contactoForm.invalid" />
        </div>
      </form>
    </p-dialog>
  `,
})
export class PersonaDetallePage {
  protected readonly auth = inject(AuthService);
  private readonly service = inject(PersonasService);
  private readonly confirm = inject(ConfirmationService);

  /** Viene de la ruta personas/:publicId. */
  readonly publicId = input.required<string>();

  protected readonly persona = signal<Persona | null>(null);
  protected readonly formulario = signal(false);
  protected readonly guardando = signal(false);
  protected readonly contactoEditando = signal<ContactoEmergencia | null>(null);
  protected readonly dialogoContacto = signal(false);

  protected readonly contactoForm = inject(NonNullableFormBuilder).group({
    nombre: ['', [Validators.required, Validators.maxLength(120)]],
    parentesco: ['', Validators.maxLength(40)],
    telefono: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
  });

  constructor() {
    effect(() => void this.cargar(this.publicId()));
  }

  protected abrirContacto(c: ContactoEmergencia | null): void {
    this.contactoEditando.set(c);
    this.contactoForm.reset({ nombre: c?.nombre ?? '', parentesco: c?.parentesco ?? '', telefono: c?.telefono ?? '' });
    this.dialogoContacto.set(true);
  }

  protected async guardarContacto(): Promise<void> {
    const v = this.contactoForm.getRawValue();
    const datos = { nombre: v.nombre, parentesco: v.parentesco.trim() || null, telefono: v.telefono };
    const actual = this.contactoEditando();
    this.guardando.set(true);
    try {
      await firstValueFrom(actual
        ? this.service.editarContacto(this.publicId(), actual.publicId, datos)
        : this.service.agregarContacto(this.publicId(), datos));
      this.dialogoContacto.set(false);
      await this.cargar(this.publicId());
    } catch { /* toast (ej. 409 máximo 3) */ }
    finally { this.guardando.set(false); }
  }

  protected eliminarContacto(c: ContactoEmergencia): void {
    this.confirm.confirm({
      header: 'Eliminar contacto',
      message: `¿Eliminar a ${c.nombre} de los contactos de emergencia?`,
      acceptButtonProps: { label: 'Eliminar', severity: 'danger' },
      rejectButtonProps: { label: 'Cancelar', text: true },
      accept: async () => {
        try {
          await firstValueFrom(this.service.eliminarContacto(this.publicId(), c.publicId));
          await this.cargar(this.publicId());
        } catch { /* toast */ }
      },
    });
  }

  private async cargar(publicId: string): Promise<void> {
    try {
      this.persona.set(await firstValueFrom(this.service.obtener(publicId)));
    } catch {
      this.persona.set(null);   // 404: toast del interceptor
    }
  }
}
```

### 5.5 Rutas

En `children` de `app.routes.ts`:

```ts
      { path: 'personas', loadComponent: () => import('./features/personas/personas.page').then((m) => m.PersonasPage) },
      { path: 'personas/:publicId', loadComponent: () => import('./features/personas/persona-detalle.page').then((m) => m.PersonaDetallePage) },
```

### 5.6 Prueba manual

Como Admin:
- Agregar "Ana López" con un contacto → abre su detalle con código de 6 dígitos.
- Sin marcar atleta ni coach → aviso en el formulario y Guardar deshabilitado.
- Buscar "ana l" y por código; filtrar Coaches; "Incluir inactivos".
- Editar desde el detalle → los datos se actualizan sin recargar.
- Contactos: agregar hasta 3 (el botón desaparece), editar, eliminar con confirmación.
- Desactivar tu propia persona → toast "No puedes desactivarte a ti mismo."

Como Coach: ve listado y detalle (incluidos los contactos), sin botones de agregar, editar ni desactivar.

**Commit:** `feat(cliente): personas y contactos de emergencia`

---

## Checklist del módulo

- [ ] Etapa 0: namespace de `PersonaResponses`, `tipos-persona`, `GET /api/auth/sesion` con su test
- [ ] Etapa 1: proyecto en `cliente/`, tema esmeralda, Tailwind, interceptor de errores
- [ ] Etapa 2: login, selección de box, renovación automática y expiración de sesión
- [ ] Etapa 3: layout con box actual, cambio de box y menú según rol
- [ ] Etapa 4: catálogos con pestañas desde catálogos fijos
- [ ] Etapa 5: personas con paginación en servidor, alta con contactos, detalle y desactivar
- [ ] `ng build` sin errores (el aviso de tamaño del bundle inicial es esperado)
- [ ] Probado como Admin y como Coach
