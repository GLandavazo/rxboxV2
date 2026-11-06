# RxBox API (.NET 10)

Backend de RxBox: membresías, pagos y asistencia para boxes de CrossFit. Multi-tenant
por box, con autenticación JWT propia. El esquema y las reglas de autenticación están
en [`../docs/esquema-v2.md`](../docs/esquema-v2.md).

## Estructura

```
server/
├── RxBox.slnx
├── API/                     # RxBox.API: controllers, Program.cs, appsettings
│   ├── API.http             # peticiones de ejemplo para probar los endpoints
│   ├── Controllers/         # HealthController, AuthController
│   ├── Extensions/          # ServiceExtensions (DI), StartupExtensions (migrate)
│   └── Middleware/          # GlobalExceptionHandler
├── Core/                    # RxBox.Core: sin dependencias de infraestructura
│   ├── DTOs/                # Requests y Responses (nunca exponen el Id interno)
│   ├── Entities/            # BaseEntity, AuditableEntity, ITenantEntity, IActivable, IAuditable
│   ├── Enums/               # enums de sistema (se guardan como text + CHECK)
│   ├── Exceptions/          # BusinessException y derivadas
│   ├── Interfaces/          # ICurrentUserService, IAuthService, Repositories/
│   └── Options/             # JwtOptions
├── Infrastructure/          # RxBox.Infrastructure
│   ├── Auth/                # AuthService, JwtTokenService, CurrentUserService
│   ├── Data/                # AppDbContext, Configurations/, interceptor de box y auditoría
│   └── Repositories/        # GenericRepository<T>, UnitOfWork
└── Tests/                   # RxBox.Tests: xUnit + Postgres con Testcontainers
```

### Logging (Serilog)

Consola + archivo rotado diario en `logs/` con `CompactJsonFormatter`. Ajusta niveles
en la sección `Serilog` de `appsettings.json` / `appsettings.Development.json`.

## Requisitos

- .NET SDK 10
- `dotnet tool install --global dotnet-ef`
- PostgreSQL corriendo
- Docker, para los tests

## Primer arranque

```bash
# 1. Ajusta la cadena de conexión en:
#    API/appsettings.Development.json  →  ConnectionStrings:Postgres

# 2. Desde server/
cd API
dotnet run
```

Queda escuchando en:

- Health:  <http://localhost:5080/health> → `{ "status": "ok", ... }`
- Swagger: <http://localhost:5080/swagger> (solo Development)

### Configuración

Las variables de entorno con prefijo `RXBOX__` sobrescriben appsettings. Fuera de
Development es obligatorio definir el secreto del JWT (mínimo 32 bytes); sin él la API
no arranca:

```bash
RXBOX__Jwt__Secret=...
RXBOX__ConnectionStrings__Postgres=...
```

## Autenticación

JWT propio HS256. Access 15 min; refresh 30 días, guardado como hash SHA-256 y con
rotación. Claims: `sub` (usuarios.id), `box_id`, `rol`.

| Endpoint | Auth | Descripción |
|---|---|---|
| `POST /api/auth/login` | no | Un box activo: tokens. Varios: `requiereSeleccion`, `boxes`, `selectionToken` (5 min, sin box). |
| `POST /api/auth/seleccionar-box` | no | `selectionToken` + `boxPublicId` → tokens de ese box. |
| `POST /api/auth/cambiar-box` | sí | `boxPublicId` → tokens nuevos sin pedir contraseña. |
| `POST /api/auth/refresh` | no | Rota el refresh. Reusar uno ya rotado revoca la sesión del usuario en ese box. |
| `POST /api/auth/logout` | sí | Revoca el refresh token. |

## Multi-tenant

- Una entidad de box implementa `ITenantEntity`. `AppDbContext` le aplica un query
  filter por `BoxId` del usuario actual; sin box actual no regresa filas.
- El interceptor asigna `BoxId` al insertar si la entidad no lo trae. También llena la
  auditoría (`CreadoPor`/`EditadoPor`) con el `UsuarioId` actual.
- `usuario_boxes` y `refresh_tokens` llevan `box_id` pero no son `ITenantEntity`: se
  consultan en login, antes de que exista box actual.

## Tests

Requieren Docker: cada corrida levanta su propio Postgres con Testcontainers.

```bash
dotnet test
```

## Probar los endpoints

Colección Postman en [`../docs/postman/`](../docs/postman/), o abre
[`API/API.http`](API/API.http) en VS Code / Rider. Todo endpoint nuevo o modificado se
refleja en la colección.

## Convenciones

- Reglas de negocio → lanzar una `BusinessException` (`ConflictException`,
  `NotFoundException`, `DependencyException`, `UnauthenticatedException`). Nunca
  `InvalidOperationException` suelta.
- CRUD genérico vía `uow.Repository<T>()`; repo específico solo si necesitas
  `Include` / proyección / SQL crudo o encapsular una regla.
- Rutas y DTOs usan `PublicId`; el `Id` int no sale de la API (`GetByPublicIdAsync`).
- `[Authorize]` a nivel de clase por default; `[AllowAnonymous]` como excepción.
- Soft-delete en `IActivable` → siempre `PATCH .../{publicId}/toggle`.
- CORS y URLs siempre desde `appsettings` / variables de entorno.
