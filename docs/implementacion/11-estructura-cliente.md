# 11 — Estructura del cliente (Sakai + organización de sicogu)

La plantilla Sakai (`client/`, Angular 21 + PrimeNG 21) con la organización de carpetas de sicogu. Las pantallas se diseñan aparte (Stitch); esto es la base sobre la que se montan.

```
client/src/
├── app.component.ts        <router-outlet> + <p-toast> + <p-confirmdialog> (una sola vez)
├── app.config.ts           providers: router, http, PrimeNG, MessageService, ConfirmationService
├── app.routes.ts           AppLayout (con menú) y PublicLayout (sin menú)
└── app/
    ├── core/                       lo que es único en la app y no se pinta
    │   ├── data/                   constantes: menu.data.ts (MENU + menuParaRol)
    │   ├── dtos/                   contratos del API, un archivo por módulo
    │   ├── guards/                 CanActivateFn
    │   ├── interceptors/           HttpInterceptorFn
    │   ├── models/                 modelos de UI propios (AppMenuItem)
    │   └── services/               servicios singleton: AvisosService, después AuthService y uno por módulo
    ├── layouts/
    │   ├── main-layout/            el layout de Sakai
    │   │   ├── app.layout.ts       topbar + sidebar + <router-outlet> + <app-loading [listener]="true">
    │   │   ├── components/         app.topbar, app.sidebar, app.menu, app.menuitem, app.footer,
    │   │   │                       app.configurator, breadcrumb, page-content (+ index.ts)
    │   │   └── service/            layout.service.ts (menú abierto/cerrado, tema oscuro)
    │   └── public-layout/          solo <router-outlet>: login, landing, errores, kiosco de check-in
    ├── pages/                      una carpeta por módulo; ahí van las pantallas de Stitch
    └── shared/                     reutilizable y sin estado de negocio
        ├── components/             input, select, input-code, loading, modal-content, skeleton, stepper
        ├── directives/             input-focus, input-uppercase, only-numbers, remove-host, role-control
        ├── functions/              dates, errors, helpers, objects, skeleton
        └── states/                 base.state, app.state, user.state
```

Todo se importa por su `index.ts`: `import { InputComponent, LoadingComponent } from '@/app/shared/components';`.

**Regla para decidir dónde va algo:** si tiene una sola instancia y habla con el API o con el router, va en `core/`. Si se pinta y se reusa en varias pantallas, va en `shared/components`. Si es una función pura sin `inject()`, va en `shared/functions`. Si solo la usa una pantalla, va en `pages/<modulo>/components`.

---

## 1. States: dónde usar uno

`BaseStateService<T>` es un store mínimo sobre un `signal`: `select(key)` devuelve un signal de solo lectura de una llave, `set(key, valor)` cambia una, `setState(parcial)` cambia varias y `resetState()` regresa al estado inicial.

```ts
type PersonasStateType = { filtro: string; pagina: number };

@Injectable({ providedIn: 'root' })
export class PersonasState extends BaseStateService<PersonasStateType> {
    readonly filtro = this.select('filtro');   // campo, no getter: crea un solo computed
    readonly pagina = this.select('pagina');
    constructor() { super({ filtro: '', pagina: 1 }); }
}
```

**Usa un state cuando el dato lo leen varios componentes que no son padre e hijo**, o cuando debe sobrevivir a la navegación:

| State | Qué guarda | Quién escribe | Quién lee |
|---|---|---|---|
| `AppState` | título de la página, overlay de carga | `PageContent` (título), cualquier servicio (`showLoading`/`hideLoading`) | topbar, `<app-loading [listener]="true">`, pestaña del navegador |
| `UserState` | sesión: email, box actual, boxes, rol | solo el AuthService (login, cambio de box, restaurar, logout) | menú, topbar, `appRoleControl`, guards de rol, pantallas que ocultan acciones |
| `<Modulo>State` (cuando haga falta) | filtros y página de un listado | la pantalla del listado | la misma pantalla al regresar del detalle |

Ejemplo del último caso: entras a Personas, buscas "ana" en la página 3, abres un detalle y regresas. Con un `PersonasState` en `root` el listado vuelve con "ana" y página 3. Si los filtros vivieran en el componente, se perderían al destruirse.

**No uses un state** para:
- **Datos de una sola pantalla** (lo que se está editando, si un diálogo está abierto): un `signal` en el componente.
- **Pasar datos de padre a hijo**: `input()` / `output()` / `model()`.
- **Respuestas del API "por si acaso"**: se piden al entrar a la pantalla. Los catálogos fijos sí se cachean, pero en su servicio con `shareReplay`, no en un state.

`provideIn: 'root'` hace que el state dure toda la sesión. Si debe morir con la pantalla, regístralo en el componente con `providers: [MiState]`.

---

## 2. Functions (`shared/functions`): cuándo aplicar cada una

Funciones puras, sin `inject()`. Se pueden usar en componentes, servicios y guards.

| Archivo | Función | Úsala cuando |
|---|---|---|
| `dates.ts` | `toDateOnly(date)` | Mandas una fecha al API (`DateOnly` de .NET, `"yyyy-MM-dd"`). Nunca uses `toISOString()`: convierte a UTC y en Hermosillo cambia el día después de las 17:00. |
| | `fromDateOnly(texto)` | Recibes un `DateOnly` del API y lo vas a poner en un `p-datepicker`. |
| `errors.ts` | `mensajeDeError(error)` | Necesitas el texto de un error HTTP: junta los errores de validación (400) o toma `detail` (404/409). Lo usa el interceptor de errores; en una pantalla, cuando el error se muestra dentro del formulario (ej. el login). |
| | `ProblemDetails` | Tipar `error.error` de una respuesta del API. |
| `helpers.ts` | `toHttpParams(filtros)` | Armas los query params de un listado: omite vacíos y repite la llave en arreglos (`tipo=A&tipo=B`), como lo enlaza ASP.NET. |
| | `descargarBlob(blob, nombre)` | Descargas un archivo (reporte, Excel). Pídelo con `HttpClient` y `responseType: 'blob'` para que lleve el token; esta función solo dispara la descarga. |
| `objects.ts` | `IsNoE(valor)` | Validas "vacío" en general: null, undefined, texto en blanco o arreglo vacío. |
| | `removeNullProperties(obj)` | Limpias un objeto de filtros antes de mandarlo. |
| | `filterObjectFields(obj, llaves)` | Mandas en un PUT solo los campos editables de un formulario más grande. |
| | `matchArrays(a, b)` | Intersección de dos listas (ej. roles permitidos vs roles del usuario). |
| | `hasJsonStructure(texto)` | Validas si un texto es JSON antes de `JSON.parse`. |
| `skeleton.ts` | `filasSkeleton(n)` | Le das a un `p-table` N filas vacías mientras carga. Guárdalo en un campo, no lo llames en la plantilla. |
| | `conSkeleton(cargando, accion)` | Envuelves una carga: prende el signal `cargando`, ejecuta y lo apaga aunque falle. |

**Patrón de listado con skeleton:**

```ts
protected readonly cargando = signal(false);
protected readonly filas = filasSkeleton(5);

cargar() {
    return conSkeleton(this.cargando, async () =>
        this.personas.set(await firstValueFrom(this.service.listar(...))));
}
```

```html
<p-table [value]="cargando() ? filas : personas()">
    <ng-template #body let-p>
        @if (cargando()) { <tr app-skeleton-fila [columnas]="4"></tr> }
        @else { <tr>...</tr> }
    </ng-template>
</p-table>
```

---

## 3. Directivas (`shared/directives`)

| Directiva | Úsala en | Ejemplo |
|---|---|---|
| `appInputFocus` | El primer campo de un formulario o diálogo. | `<input pInputText formControlName="nombre" appInputFocus />` |
| `appInputUppercase` | Campos que se guardan en mayúsculas (RFC, placas, códigos con letras). Funciona con y sin formControl. | `<input pInputText formControlName="rfc" appInputUppercase />` |
| `appOnlyNumbers` | Teléfonos, código de atleta, cualquier campo de solo dígitos. También limpia lo pegado. Para montos o decimales usa `p-inputnumber`. | `<input pInputText formControlName="telefono" maxlength="10" inputmode="numeric" appOnlyNumbers />` |
| `appRoleControl` | Botones o secciones que solo ve un rol. Reacciona al cambio de box. Es solo UX: el backend valida el rol en cada endpoint. | `<p-button [appRoleControl]="['ADMIN']" label="Agregar" />` |
| `appRemoveHost` | Raro: cuando un componente debe quedar "plano" dentro de un `<ul>` o `<tr>`. Rompe el encapsulamiento; evítala si puedes usar un selector de atributo (`tr[app-…]`). | `<app-item appRemoveHost />` |

---

## 4. Componentes (`shared/components`)

| Componente | Úsalo cuando | Ejemplo |
|---|---|---|
| `app-input` | Envuelves cualquier control de formulario para tener etiqueta en el estilo del diseño (mayúsculas pequeñas), asterisco de obligatorio y mensaje de error. | `<app-input label="Nombre" error="Obligatorio" [valid]="!f.nombre.invalid \|\| !f.nombre.touched"><input pInputText formControlName="nombre" required /></app-input>` |
| `app-select` | Lista de objetos con búsqueda y limpiar, enlazada con `[(value)]` (sin Reactive Forms). Con `formControlName` usa `p-select` directo. | `<app-select [items]="metodos()" descriptionField="nombre" [(value)]="metodo" />` |
| `app-input-code` | Capturar un código por dígitos: el check-in con código de atleta. Acepta pegar el código entero y tiene `limpiar()`. | `<app-input-code [longitud]="6" (codigo)="checkin($event)" />` |
| `app-loading` | Overlay de carga a pantalla completa. Con `[listener]="true"` sigue a `AppState.isLoading`; ya está en el layout, así que en pantallas basta `appState.showLoading()`. Sin listener se muestra mientras exista: `@if (procesando()) { <app-loading /> }`. | |
| `app-modal-content` | El componente que abres con `DialogService` (diálogos dinámicos): cuerpo, pie con `<div footer>`, `close()` y `closeSuccess()`. | `ref.onClose.subscribe(ok => ok && this.cargar())` |
| `app-skeleton` | Placeholder de una región mientras carga: `texto`, `tarjeta` o `lista`. | `<app-skeleton variante="tarjeta" alto="10rem" />` |
| `tr[app-skeleton-fila]` | Filas de carga dentro de un `p-table`. | ver el patrón de la sección 2 |
| `app-stepper` | Indicador visual de pasos en un flujo de varias pantallas (ej. alta de atleta → membresía → pago). Solo pinta; la navegación la lleva la pantalla. Para pasos con contenido usa `p-stepper`. | `<app-stepper [steps]="pasos" [currentStep]="paso()" />` |

### Avisos (en `core/services`, no en `functions`)

`AvisosService` reemplaza a `cAlert`, `cConfirm` y `toast` de sicogu. Es un servicio y no una función porque necesita `MessageService` y `ConfirmationService` de PrimeNG.

```ts
private readonly avisos = inject(AvisosService);

this.avisos.toast('Persona guardada');                   // success por defecto
this.avisos.toast('Revisa el teléfono', 'warn');
if (await this.avisos.confirmar('¿Desactivar a Ana?', { titulo: 'Desactivar', aceptar: 'Desactivar', peligro: true })) { ... }
```

---

## 5. Layout

- **`AppLayout`** (`layouts/main-layout`) es el de Sakai, con estos cambios:
  - **Topbar:** "RxBox" en lugar de "SAKAI"; muestra box, email y rol desde `UserState`, y el botón Salir.
  - **Menú:** sale de `core/data/menu.data.ts` y se filtra por rol con `menuParaRol()`. Se recalcula al cambiar de box.
  - **Carga:** `<app-loading [listener]="true">` va una sola vez, en el layout.
- **`PageContent`** es el marco de cada pantalla, igual que en sicogu: migas, título, subtítulo y botones de acción. También pone el título en `AppState` y en la pestaña del navegador.

  ```html
  <app-page-content pageTitle="Personas" subtitle="Atletas y coaches del box">
      <p-button acciones label="Agregar" icon="pi pi-plus" />
      ...pantalla de Stitch...
  </app-page-content>
  ```

- **`Breadcrumb`** arma las migas desde `MENU`. Para pantallas que no están en el menú (un detalle), se agrega `data: { breadcrumb: 'Detalle' }` en la ruta.
- **`PublicLayout`** es para todo lo que va sin menú: `/auth/*`, `/landing`, `/notfound` y, más adelante, el kiosco de check-in.

---

## 6. Qué no se trajo de sicogu y por qué

| De sicogu | Decisión |
|---|---|
| `functions/animations.ts` (`@angular/animations`) | Ese paquete está deprecado y la plantilla no lo tiene. Se usa `animate.enter` / `animate.leave` de Angular con las clases `animate-fadein` / `animate-fadeout` (ver `app-loading`). |
| `functions/dialogs.ts` (SweetAlert + toastr por jQuery) | Reemplazado por `AvisosService` con PrimeNG. |
| `helpers.loadScripts`, `getSiteUrl` | Eran para cargar el JS del tema anterior con jQuery. Sakai no lo necesita. |
| `helpers.downloadHandled` (XHR) | No mandaba el token. Reemplazado por `descargarBlob` + `HttpClient`. |
| `helpers.extractErrorMessage` | Estaba hecho para FastAPI. Reemplazado por `mensajeDeError` para ProblemDetails de ASP.NET. |
| `convertToHttpParams` | Ahora es `toHttpParams`: omite vacíos y usa `key=` en vez de `key[]=` para arreglos, que es lo que entiende ASP.NET. |
| `permission-control.directive` | RxBox no tiene permisos finos, solo rol por box. Basta `appRoleControl`. |
| `pipes/page-texts` | Dependía de `AppTexts` de sicogu. |
| States de negocio de sicogu (`periodo-fiscal`, `factores`…) | Son de sicogu. |
| `UserState` de sicogu | Reescrito para RxBox: box actual, boxes y rol en lugar de roles globales de SIGA. |
| `input` y `modal-content` con jQuery y clases de Bootstrap | Reescritos con Tailwind (las clases de la plantilla) y sin jQuery. |
| `select` con `p-dropdown` | Ahora usa `p-select`; `p-dropdown` ya no existe en PrimeNG 21. |

---

## 7. Core: HTTP, sesión, interceptores y guards

Ya implementado en la plantilla. Sustituye las etapas 1 a 3 de `10-cliente-base.md` (que asumía un proyecto Angular nuevo).

```
src/environments/environment(.development).ts   apiUrl: '/api' (prod) | 'http://localhost:5080/api' (dev, por fileReplacements)
core/dtos/auth.dto.ts           LoginRequest, LoginResponse, TokensResponse, SesionResponse
core/dtos/commons.dto.ts        Pagina<T>, Paginacion, paginacionDesdeLazy(evento p-table)
core/services/http.service.ts   GET / POST / PUT / PATCH / DELETE / GET_BLOB → Promise<T>
core/services/app-storage.service.ts   solo los dos tokens en localStorage
core/services/auth.service.ts   login, seleccionarBox, cambiarBox, restaurarSesion, refreshToken, logOut, expirarSesion
core/interceptors/request.interceptor.ts   Bearer + overlay global
core/interceptors/error.interceptor.ts     401 → refresh una vez y reintenta; otros → toast
core/guards/                    authGuard, isAuthenticatedGuard, roleGuard, seleccionPendienteGuard
pages/auth/login.ts, seleccionar-box.ts   lógica conectada; la maqueta la reemplaza Stitch
```

### HttpService: opciones por petición

| Opción | Efecto | Cuándo |
|---|---|---|
| `params` | objeto de filtros → query string (omite vacíos) | listados |
| `sinOverlay` | no prende el overlay global | lecturas con skeleton propio (`conSkeleton`) |
| `sinAlerta` | el error interceptor no muestra toast | la pantalla pinta el error en su lugar (login) |
| `skipInterceptor` | ni token, ni refresh, ni aviso | solo `/auth/refresh` |

Cada módulo tiene su servicio en `core/services` que usa `HttpService` (ejemplo en el comentario de `http.service.ts`). Las pantallas no inyectan `HttpClient`.

### Flujo de sesión

1. **Arranque:** `provideAppInitializer` llama `restaurarSesion()`. Si hay refresh token, pide `GET /auth/sesion` y llena `UserState` antes de la primera navegación; por eso los guards solo leen `UserState`.
2. **Login:** un box → tokens → `/auth/sesion` → `/` (o `?volver=`). Varios boxes → `seleccionPendiente` (solo en memoria) → `/auth/seleccionar-box`.
3. **Token vencido:** el error interceptor llama `refreshToken()`. Es *single-flight*: si cinco peticiones reciben 401 a la vez, sale un solo refresh, porque el API rota el refresh token y reusar el viejo revoca todas las sesiones.
4. **Refresh fallido:** `expirarSesion()` limpia y manda a `/auth/login?expirada=1`.
5. **Cambiar box (topbar):** `POST /auth/cambiar-box` → tokens nuevos → `/auth/sesion` → `/`.
6. **Salir:** `POST /auth/logout` con Bearer (el endpoint lo exige) y `sinAlerta`; aunque falle, se limpia la sesión local.

### Rutas

- `AppLayout` lleva `canActivate: [authGuard]` y `canActivateChild: [roleGuard]`. Para limitar una pantalla: `data: { roles: ['ADMIN'] }`; sin rol permitido manda a `/auth/access`.
- `/auth/login` lleva `isAuthenticatedGuard`; `/auth/seleccionar-box`, `seleccionPendienteGuard`.

### Verificado (API simulada con Playwright)

Sin sesión → login con `volver`; contraseña mala → error en el formulario sin toast; login con 2 boxes → selección → topbar con selector; recarga → sesión restaurada; `/auth/login` con sesión → `/`; cambiar box con access vencido → `refresh` + reintento (un solo refresh); salir → logout con Bearer y tokens borrados; tokens revocados al recargar → `/auth/login?expirada=1`.

**Requiere del backend:** `GET /api/auth/sesion` (RF-AUT-001, etapa 0 del doc 10). Sin él, el login entra pero falla al cargar la sesión.

---

## 8. Siguiente paso

Pantallas con Stitch sobre esta estructura: inicio, catálogos y personas (etapas 4 en adelante de `10-cliente-base.md`), cada una con su servicio en `core/services` y su ruta en `app.routes.ts`.
