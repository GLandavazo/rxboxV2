# Implementación 02 — Personas y contactos de emergencia

Requerimientos: `docs/requerimientos/02-personas.md` (RF-PER-001 a 004, RF-CEM-001 a 003).

**Requisito:** módulo 00 terminado. Este módulo usa `PaginacionRequest`, `PaginaResponse<T>`, `ToPaginaAsync` y `factory.CrearClienteAsync`.

Archivos:

```
server/
├── API/
│   ├── Extensions/ServiceExtensions.cs                         (registro)
│   └── Controllers/PersonasController.cs                       (nuevo)
├── Core/
│   ├── Enums/EnumsSistema.cs                                   (+ TipoPersona)
│   ├── DTOs/Requests/Personas/PersonaRequests.cs               (nuevo)
│   ├── DTOs/Responses/Personas/PersonaResponses.cs             (nuevo)
│   └── Interfaces/IPersonaService.cs                           (nuevo)
├── Infrastructure/Services/
│   ├── Comun/Texto.cs                                          (nuevo)
│   ├── Catalogos/CatalogoFijoService.cs                        (+ tipos-persona)
│   └── Personas/PersonaService.cs                              (nuevo)
└── Tests/
    ├── RxBoxApiFactory.cs                                      (+ CrearCoachAsync)
    └── PersonaTests.cs                                         (nuevo)
```

Rutas:

| Método | Ruta | Rol |
|---|---|---|
| GET | `/api/personas?buscar=&tipo=&soloActivos=&pagina=&tamano=` | Admin, Coach |
| GET | `/api/personas/{publicId}` | Admin, Coach |
| POST | `/api/personas` | Admin |
| PUT | `/api/personas/{publicId}` | Admin |
| PATCH | `/api/personas/{publicId}/toggle` | Admin |
| POST | `/api/personas/{publicId}/contactos` | Admin |
| PUT | `/api/personas/{publicId}/contactos/{contactoPublicId}` | Admin |
| DELETE | `/api/personas/{publicId}/contactos/{contactoPublicId}` | Admin |

---

## Paso 0 — Piezas comunes

### 0.1 Enum del filtro

Al final de `Core/Enums/EnumsSistema.cs`:

```csharp
    /// <summary>Filtro del listado de personas. No se guarda: viene de es_atleta / es_coach.</summary>
    public enum TipoPersona
    {
        ATLETA,
        COACH
    }
```

En `CatalogoFijoService`, agrega `["tipos-persona"] = typeof(TipoPersona),`.

### 0.2 Limpieza de texto

`Infrastructure/Services/Comun/Texto.cs`

```csharp
namespace RxBox.Infrastructure.Services.Comun
{
    public static class Texto
    {
        /// <summary>"  Ana   María " → "Ana María"</summary>
        public static string Normalizar(string valor) =>
            string.Join(' ', valor.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        /// <summary>null, "" o "   " → null; si no, el valor sin espacios en los extremos.</summary>
        public static string? Opcional(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
```

> `CatalogoService` tiene su propio `Normalizar` privado. Puedes cambiarlo por `Texto.Normalizar` cuando quieras; no es parte de este módulo.

### 0.3 Usuario coach para tests

Agrega a `Tests/RxBoxApiFactory.cs`:

```csharp
        /// <summary>Usuario con rol COACH en el box.</summary>
        public async Task<Usuario> CrearCoachAsync(Box box)
        {
            var usuario = await CrearUsuarioAsync(box);
            await EnDbAsync(db => db.UsuarioBoxes
                .Single(ub => ub.UsuarioId == usuario.Id && ub.BoxId == box.Id)
                .Rol = RolUsuario.COACH);
            return usuario;
        }
```

---

## Etapa 1 — Listado y detalle (RF-PER-001)

### 1.1 DTOs

`Core/DTOs/Requests/Personas/PersonaRequests.cs` (se completa en las etapas 2, 3 y 5)

```csharp
using System.ComponentModel.DataAnnotations;
using RxBox.Core.DTOs.Requests.Comun;
using RxBox.Core.Enums;

namespace RxBox.Core.DTOs.Requests.Personas
{
    public class PersonaFiltroRequest : PaginacionRequest
    {
        /// <summary>Texto en nombre completo, código, email o teléfono.</summary>
        [StringLength(100)]
        public string? Buscar { get; set; }

        public TipoPersona? Tipo { get; set; }
        public bool SoloActivos { get; set; } = true;
    }
}
```

`Core/DTOs/Responses/Personas/PersonaResponses.cs`

```csharp
namespace RxBox.Core.DTOs.Responses.Personas
{
    public record ContactoEmergenciaResponse(Guid PublicId, string Nombre, string? Parentesco, string Telefono);

    public record PersonaListItemResponse(
        Guid PublicId, string Codigo, string Nombre, string Apellidos,
        string? Email, string? Telefono, bool EsAtleta, bool EsCoach, bool Activo);

    public record PersonaResponse(
        Guid PublicId, string Codigo, string Nombre, string Apellidos,
        string? Email, string? Telefono, bool EsAtleta, bool EsCoach, bool Activo,
        DateTimeOffset FechaCreacion,
        IReadOnlyList<ContactoEmergenciaResponse> Contactos);
}
```

### 1.2 Interfaz

`Core/Interfaces/IPersonaService.cs`

```csharp
using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.DTOs.Responses.Comun;
using RxBox.Core.DTOs.Responses.Personas;

namespace RxBox.Core.Interfaces
{
    public interface IPersonaService
    {
        Task<PaginaResponse<PersonaListItemResponse>> ListarAsync(PersonaFiltroRequest filtro, CancellationToken ct = default);
        Task<PersonaResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default);
    }
}
```

### 1.3 Servicio

`Infrastructure/Services/Personas/PersonaService.cs`. El constructor ya trae `AppDbContext` y `ICurrentUserService`, que se usan en la etapa 4.

```csharp
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.DTOs.Responses.Comun;
using RxBox.Core.DTOs.Responses.Personas;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Core.Interfaces.Repositories;
using RxBox.Infrastructure.Data;
using RxBox.Infrastructure.Services.Comun;

namespace RxBox.Infrastructure.Services.Personas
{
    /// <summary>
    /// Personas por medio del UnitOfWork. AppDbContext se usa solo para usuario_boxes y
    /// refresh_tokens, que no son BaseEntity; es la misma instancia que usa el UnitOfWork.
    /// </summary>
    public class PersonaService(IUnitOfWork uow, AppDbContext db, ICurrentUserService currentUser) : IPersonaService
    {
        private const int MaxContactos = 3;

        private IGenericRepository<Persona> Personas => uow.Repository<Persona>();
        private IGenericRepository<ContactoEmergencia> Contactos => uow.Repository<ContactoEmergencia>();

        public async Task<PaginaResponse<PersonaListItemResponse>> ListarAsync(
            PersonaFiltroRequest f, CancellationToken ct = default)
        {
            var query = Personas.AsQueryable().AsNoTracking();

            if (f.SoloActivos) query = query.Where(p => p.Activo);

            if (f.Tipo == TipoPersona.ATLETA) query = query.Where(p => p.EsAtleta);
            else if (f.Tipo == TipoPersona.COACH) query = query.Where(p => p.EsCoach);

            if (!string.IsNullOrWhiteSpace(f.Buscar))
            {
                var patron = $"%{Texto.Normalizar(f.Buscar)}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.Nombre + " " + p.Apellidos, patron)
                    || EF.Functions.ILike(p.Codigo, patron)
                    || (p.Email != null && EF.Functions.ILike(p.Email, patron))
                    || (p.Telefono != null && EF.Functions.ILike(p.Telefono, patron)));
            }

            return await query
                .OrderBy(p => p.Apellidos).ThenBy(p => p.Nombre).ThenBy(p => p.Id)   // Id: orden estable entre páginas
                .Select(p => new PersonaListItemResponse(
                    p.PublicId, p.Codigo, p.Nombre, p.Apellidos, p.Email, p.Telefono, p.EsAtleta, p.EsCoach, p.Activo))
                .ToPaginaAsync(f, ct);
        }

        public async Task<PersonaResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default)
        {
            var persona = await ObtenerEntidadAsync(publicId, ct);

            var contactos = await Contactos.AsQueryable().AsNoTracking()
                .Where(c => c.PersonaId == persona.Id)
                .OrderBy(c => c.Id)
                .Select(c => new ContactoEmergenciaResponse(c.PublicId, c.Nombre, c.Parentesco, c.Telefono))
                .ToListAsync(ct);

            return new PersonaResponse(
                persona.PublicId, persona.Codigo, persona.Nombre, persona.Apellidos,
                persona.Email, persona.Telefono, persona.EsAtleta, persona.EsCoach, persona.Activo,
                persona.FechaCreacion, contactos);
        }

        // Query filter de tenant: un publicId de otro box también da 404.
        private async Task<Persona> ObtenerEntidadAsync(Guid publicId, CancellationToken ct) =>
            await Personas.GetByPublicIdAsync(publicId, ct)
                ?? throw new NotFoundException("Persona", publicId);
    }
}
```

`EF.Functions.ILike` viene de Npgsql (namespace `Microsoft.EntityFrameworkCore`). `p.Nombre + " " + p.Apellidos` se traduce a `nombre || ' ' || apellidos`, así "ana l" encuentra a "Ana López" (CA-001).

### 1.4 Registro

En `AddAppServices` (y `using RxBox.Infrastructure.Services.Personas;`):

```csharp
            services.AddScoped<IPersonaService, PersonaService>();
```

### 1.5 Controller

`API/Controllers/PersonasController.cs` (se completa en las siguientes etapas)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/personas")]
    [Authorize]
    public class PersonasController(IPersonaService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] PersonaFiltroRequest filtro, CancellationToken ct) =>
            Ok(await service.ListarAsync(filtro, ct));

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Obtener(Guid publicId, CancellationToken ct) =>
            Ok(await service.ObtenerAsync(publicId, ct));
    }
}
```

### 1.6 Tests

`Tests/PersonaTests.cs` (las siguientes etapas agregan más):

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Infrastructure.Data;

namespace RxBox.Tests
{
    [Collection(ApiCollection.Nombre)]
    public class PersonaTests(RxBoxApiFactory factory)
    {
        /// <summary>Box nuevo con un admin como usuario actual.</summary>
        private sealed class Contexto(IServiceScope scope, Box box, Usuario admin) : IDisposable
        {
            public Box Box { get; } = box;
            public Usuario Admin { get; } = admin;
            public IPersonaService Servicio { get; } = scope.ServiceProvider.GetRequiredService<IPersonaService>();
            public AppDbContext Db { get; } = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            public void Dispose() => scope.Dispose();
        }

        private async Task<Contexto> ContextoAsync()
        {
            var box = await factory.CrearBoxAsync();
            var admin = await factory.CrearUsuarioAsync(box);
            return new Contexto(factory.ScopeDeUsuario(admin.Id, box), box, admin);
        }

        [Fact]
        public async Task Detalle_de_otro_box_es_no_encontrado()
        {
            var deOtroBox = await factory.CrearPersonaAsync(await factory.CrearBoxAsync());
            using var ctx = await ContextoAsync();

            await Assert.ThrowsAsync<NotFoundException>(() => ctx.Servicio.ObtenerAsync(deOtroBox.PublicId));
        }

        [Fact]
        public async Task Coach_puede_consultar_el_listado()
        {
            var coach = await factory.CrearCoachAsync(await factory.CrearBoxAsync());
            using var client = await factory.CrearClienteAsync(coach);

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/personas")).StatusCode);
        }
    }
}
```

El test de búsqueda y paginación va en la etapa 2, porque necesita crear personas con el servicio.

**Commit:** `feat(personas): listado y detalle`

---

## Etapa 2 — Agregar (RF-PER-002)

### 2.1 DTOs

Agrega a `PersonaRequests.cs`:

```csharp
    internal static class ReglasPersona
    {
        public const string Telefono = @"^\d{10}$";
        public const string TelefonoMensaje = "El teléfono debe tener 10 dígitos, sin espacios ni guiones.";

        public static IEnumerable<ValidationResult> ValidarRol(bool esAtleta, bool esCoach)
        {
            if (!esAtleta && !esCoach)
                yield return new ValidationResult(
                    "La persona debe ser atleta, coach o ambos.", ["EsAtleta", "EsCoach"]);
        }
    }

    public record ContactoEmergenciaRequest(
        [Required, StringLength(120)] string Nombre,
        [StringLength(40)] string? Parentesco,
        [Required, RegularExpression(ReglasPersona.Telefono, ErrorMessage = ReglasPersona.TelefonoMensaje)] string Telefono);

    public record PersonaCreateRequest(
        [Required, StringLength(80)] string Nombre,
        [Required, StringLength(120)] string Apellidos,
        [EmailAddress, StringLength(150)] string? Email,
        [RegularExpression(ReglasPersona.Telefono, ErrorMessage = ReglasPersona.TelefonoMensaje)] string? Telefono,
        bool EsAtleta = true,
        bool EsCoach = false,
        [MaxLength(3, ErrorMessage = "Máximo 3 contactos de emergencia.")] IReadOnlyList<ContactoEmergenciaRequest>? Contactos = null)
        : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            ReglasPersona.ValidarRol(EsAtleta, EsCoach);
    }
```

Cómo responde 400 cada caso, sin código en el servicio:
- `[Required]` rechaza `null`, `""` y `"   "`.
- `[RegularExpression]` ignora `null`, así el teléfono es opcional en la persona y obligatorio en el contacto (ahí lleva `[Required]`).
- `[MaxLength(3)]` cuenta los elementos de la lista (CA-004).
- `IValidatableObject` corre después de los atributos y valida que tenga rol (CA-002).
- `[ApiController]` valida también cada `ContactoEmergenciaRequest` de la lista.

### 2.2 Interfaz

Agrega a `IPersonaService` (y `using RxBox.Core.DTOs.Requests.Personas;` ya está):

```csharp
        Task<PersonaResponse> CrearAsync(PersonaCreateRequest request, CancellationToken ct = default);
```

### 2.3 Servicio

Agrega a `PersonaService`:

```csharp
        public async Task<PersonaResponse> CrearAsync(PersonaCreateRequest request, CancellationToken ct = default)
        {
            var persona = new Persona { Codigo = await GenerarCodigoAsync(ct) };
            AplicarDatos(persona, request.Nombre, request.Apellidos, request.Email, request.Telefono,
                request.EsAtleta, request.EsCoach);

            // Persona y contactos juntos o nada (CA-005). Los contactos necesitan persona.Id,
            // que existe hasta el primer SaveAsync.
            await using var tx = await uow.BeginTransactionAsync(ct);

            await Personas.AddAsync(persona, ct);
            await uow.SaveAsync(ct);

            if (request.Contactos is { Count: > 0 })
            {
                foreach (var contacto in request.Contactos)
                    await Contactos.AddAsync(NuevoContacto(persona.Id, contacto), ct);
                await uow.SaveAsync(ct);
            }

            await tx.CommitAsync(ct);
            return await ObtenerAsync(persona.PublicId, ct);
        }

        /// <summary>6 dígitos al azar, único en el box (el query filter limita la búsqueda al box).</summary>
        private async Task<string> GenerarCodigoAsync(CancellationToken ct)
        {
            for (var intento = 0; intento < 10; intento++)
            {
                var codigo = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
                if (!await Personas.ExistsAsync(p => p.Codigo == codigo, ct)) return codigo;
            }

            // Con un millón de combinaciones solo pasa si el box tiene cientos de miles de personas.
            throw new ConflictException("No se pudo generar un código único. Intenta de nuevo.");
        }

        private static void AplicarDatos(
            Persona p, string nombre, string apellidos, string? email, string? telefono, bool esAtleta, bool esCoach)
        {
            p.Nombre = Texto.Normalizar(nombre);
            p.Apellidos = Texto.Normalizar(apellidos);
            p.Email = Texto.Opcional(email)?.ToLowerInvariant();
            p.Telefono = Texto.Opcional(telefono);
            p.EsAtleta = esAtleta;
            p.EsCoach = esCoach;
        }

        private static ContactoEmergencia NuevoContacto(int personaId, ContactoEmergenciaRequest request)
        {
            var contacto = new ContactoEmergencia { PersonaId = personaId };
            AplicarContacto(contacto, request);
            return contacto;
        }

        private static void AplicarContacto(ContactoEmergencia c, ContactoEmergenciaRequest request)
        {
            c.Nombre = Texto.Normalizar(request.Nombre);
            c.Parentesco = Texto.Opcional(request.Parentesco);
            c.Telefono = request.Telefono.Trim();
        }
```

> Si dos altas obtienen el mismo código en el mismo instante, el índice único `(box_id, codigo)` rechaza la segunda con un 500. Es muy improbable; no se cubre ahora.

### 2.4 Controller

```csharp
        [HttpPost]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> Crear([FromBody] PersonaCreateRequest request, CancellationToken ct)
        {
            var creada = await service.CrearAsync(request, ct);
            return CreatedAtAction(nameof(Obtener), new { publicId = creada.PublicId }, creada);
        }
```

### 2.5 Tests

Agrega a `PersonaTests`:

```csharp
        [Fact]
        public async Task Crear_genera_codigo_de_6_digitos_limpia_datos_y_guarda_contactos()
        {
            using var ctx = await ContextoAsync();

            var creada = await ctx.Servicio.CrearAsync(new(
                "  Ana ", "López   García", "ANA@Mail.com", "6621234567",
                Contactos: [new("Luis López", "Esposo", "6627654321"), new("Marta", null, "6620000000")]));

            Assert.Matches(@"^\d{6}$", creada.Codigo);
            Assert.Equal("Ana", creada.Nombre);
            Assert.Equal("López García", creada.Apellidos);
            Assert.Equal("ana@mail.com", creada.Email);
            Assert.True(creada.EsAtleta);
            Assert.False(creada.EsCoach);
            Assert.Equal(["Luis López", "Marta"], creada.Contactos.Select(c => c.Nombre));
        }

        [Fact]
        public async Task Crear_invalido_responde_400()
        {
            using var client = await factory.CrearClienteAsync(await factory.CrearUsuarioAsync(await factory.CrearBoxAsync()));
            var contacto = new { nombre = "Luis", telefono = "6621234567" };

            object[] invalidos =
            [
                new { nombre = "Ana", apellidos = "López", esAtleta = false, esCoach = false },   // sin rol
                new { nombre = "Ana", apellidos = "López", telefono = "662-123-45" },            // teléfono
                new { nombre = "   ", apellidos = "López" },                                     // nombre vacío
                new { nombre = "Ana", apellidos = "López", contactos = new[] { contacto, contacto, contacto, contacto } }
            ];

            foreach (var body in invalidos)
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/personas", body)).StatusCode);
        }

        [Fact]
        public async Task Coach_no_puede_agregar()
        {
            var coach = await factory.CrearCoachAsync(await factory.CrearBoxAsync());
            using var client = await factory.CrearClienteAsync(coach);

            var response = await client.PostAsJsonAsync("/api/personas", new { nombre = "Ana", apellidos = "López" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Listar_busca_por_nombre_completo_y_codigo_filtra_por_tipo_y_pagina()
        {
            using var ctx = await ContextoAsync();
            var ana = await ctx.Servicio.CrearAsync(new("Ana", "López", null, null, EsCoach: true));
            await ctx.Servicio.CrearAsync(new("Beto", "García", null, null, EsCoach: true));

            var porNombre = await ctx.Servicio.ListarAsync(new PersonaFiltroRequest { Buscar = "ana l" });
            Assert.Equal([ana.PublicId], porNombre.Items.Select(p => p.PublicId));

            var porCodigo = await ctx.Servicio.ListarAsync(new PersonaFiltroRequest { Buscar = ana.Codigo });
            Assert.Contains(porCodigo.Items, p => p.PublicId == ana.PublicId);

            var coaches = await ctx.Servicio.ListarAsync(new PersonaFiltroRequest { Tipo = TipoPersona.COACH, Tamano = 1 });
            Assert.Single(coaches.Items);
            Assert.Equal(2, coaches.Total);
            Assert.Equal(2, coaches.TotalPaginas);
            Assert.Equal("García", coaches.Items[0].Apellidos);   // ordena por apellidos
        }
```

**Commit:** `feat(personas): agregar persona con código y contactos`

---

## Etapa 3 — Editar (RF-PER-003)

### 3.1 DTO

Agrega a `PersonaRequests.cs`. Sin valores por defecto en los roles: al editar, el front siempre manda ambos.

```csharp
    public record PersonaUpdateRequest(
        [Required, StringLength(80)] string Nombre,
        [Required, StringLength(120)] string Apellidos,
        [EmailAddress, StringLength(150)] string? Email,
        [RegularExpression(ReglasPersona.Telefono, ErrorMessage = ReglasPersona.TelefonoMensaje)] string? Telefono,
        bool EsAtleta,
        bool EsCoach)
        : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            ReglasPersona.ValidarRol(EsAtleta, EsCoach);
    }
```

### 3.2 Interfaz y servicio

`IPersonaService`:

```csharp
        Task<PersonaResponse> EditarAsync(Guid publicId, PersonaUpdateRequest request, CancellationToken ct = default);
```

`PersonaService`:

```csharp
        public async Task<PersonaResponse> EditarAsync(Guid publicId, PersonaUpdateRequest request, CancellationToken ct = default)
        {
            var persona = await ObtenerEntidadAsync(publicId, ct);   // rastreada; el código no se toca (RN-001)

            AplicarDatos(persona, request.Nombre, request.Apellidos, request.Email, request.Telefono,
                request.EsAtleta, request.EsCoach);

            await uow.SaveAsync(ct);
            return await ObtenerAsync(persona.PublicId, ct);
        }
```

### 3.3 Controller

```csharp
        [HttpPut("{publicId:guid}")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> Editar(Guid publicId, [FromBody] PersonaUpdateRequest request, CancellationToken ct) =>
            Ok(await service.EditarAsync(publicId, request, ct));
```

### 3.4 Seed

La persona del admin del seed se crea sin rol (`EsAtleta` y `EsCoach` en falso) y no se podría editar sin marcar uno. En `DevelopmentSeeder.ObtenerPersonaAsync`, agrega `EsCoach = true` al crear la persona. Solo afecta bases nuevas; en tu base local actual márcala al editarla.

### 3.5 Test

```csharp
        [Fact]
        public async Task Editar_cambia_los_datos_pero_no_el_codigo()
        {
            using var ctx = await ContextoAsync();
            var creada = await ctx.Servicio.CrearAsync(new("Ana", "López", null, null));

            var editada = await ctx.Servicio.EditarAsync(creada.PublicId,
                new("Ana María", "López", "ana@mail.com", null, EsAtleta: true, EsCoach: true));

            Assert.Equal(creada.Codigo, editada.Codigo);
            Assert.Equal("Ana María", editada.Nombre);
            Assert.True(editada.EsCoach);
        }
```

**Commit:** `feat(personas): editar persona`

---

## Etapa 4 — Activar / desactivar (RF-PER-004)

### 4.1 Interfaz y servicio

`IPersonaService`:

```csharp
        Task<PersonaResponse> CambiarEstadoAsync(Guid publicId, CancellationToken ct = default);
```

`PersonaService`:

```csharp
        public async Task<PersonaResponse> CambiarEstadoAsync(Guid publicId, CancellationToken ct = default)
        {
            var persona = await ObtenerEntidadAsync(publicId, ct);

            // Persona, acceso y tokens juntos. Si una validación lanza, el using deshace todo.
            await using var tx = await uow.BeginTransactionAsync(ct);

            if (persona.Activo) await QuitarAccesoAsync(persona, ct);
            persona.Activo = !persona.Activo;   // reactivar NO devuelve el acceso (RN-002)

            await uow.SaveAsync(ct);
            await tx.CommitAsync(ct);
            return await ObtenerAsync(persona.PublicId, ct);
        }

        private async Task QuitarAccesoAsync(Persona persona, CancellationToken ct)
        {
            var acceso = await db.UsuarioBoxes.FirstOrDefaultAsync(ub =>
                ub.BoxId == persona.BoxId && ub.PersonaId == persona.Id && ub.Activo, ct);
            if (acceso is null) return;

            if (acceso.UsuarioId == currentUser.UsuarioId)
                throw new ConflictException("No puedes desactivarte a ti mismo.");

            if (acceso.Rol == RolUsuario.ADMIN)
            {
                var adminsActivos = await db.UsuarioBoxes.CountAsync(ub =>
                    ub.BoxId == persona.BoxId && ub.Rol == RolUsuario.ADMIN && ub.Activo, ct);
                if (adminsActivos <= 1)
                    throw new ConflictException("No se puede desactivar al único administrador del box.");
            }

            acceso.Activo = false;   // rastreado: se guarda con la persona y queda en bitácora

            // Corta sus sesiones en este box. El access token vigente dura hasta 15 min más.
            await db.RefreshTokens
                .Where(t => t.UsuarioId == acceso.UsuarioId && t.BoxId == acceso.BoxId && t.RevocadoEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, DateTimeOffset.UtcNow), ct);
        }
```

`ExecuteUpdateAsync` corre dentro de la transacción abierta, así que también se deshace si algo falla después.

### 4.2 Controller

```csharp
        [HttpPatch("{publicId:guid}/toggle")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> CambiarEstado(Guid publicId, CancellationToken ct) =>
            Ok(await service.CambiarEstadoAsync(publicId, ct));
```

### 4.3 Tests

```csharp
        [Fact]
        public async Task Desactivar_persona_con_acceso_le_quita_el_acceso_y_reactivar_no_lo_devuelve()
        {
            using var ctx = await ContextoAsync();
            var otroAdmin = await factory.CrearUsuarioAsync(ctx.Box);
            using (await factory.CrearClienteAsync(otroAdmin)) { }   // deja un refresh token vivo

            var acceso = await ctx.Db.UsuarioBoxes.AsNoTracking()
                .SingleAsync(ub => ub.UsuarioId == otroAdmin.Id && ub.BoxId == ctx.Box.Id);
            var persona = await ctx.Db.Personas.AsNoTracking().SingleAsync(p => p.Id == acceso.PersonaId);

            var desactivada = await ctx.Servicio.CambiarEstadoAsync(persona.PublicId);

            Assert.False(desactivada.Activo);
            Assert.False(await AccesoActivoAsync(ctx, acceso.Id));
            Assert.False(await ctx.Db.RefreshTokens.AnyAsync(t => t.UsuarioId == otroAdmin.Id && t.RevocadoEn == null));

            var reactivada = await ctx.Servicio.CambiarEstadoAsync(persona.PublicId);

            Assert.True(reactivada.Activo);
            Assert.False(await AccesoActivoAsync(ctx, acceso.Id));
        }

        private static Task<bool> AccesoActivoAsync(Contexto ctx, int usuarioBoxId) =>
            ctx.Db.UsuarioBoxes.AsNoTracking().Where(ub => ub.Id == usuarioBoxId).Select(ub => ub.Activo).SingleAsync();

        [Fact]
        public async Task No_puedes_desactivarte_a_ti_mismo()
        {
            using var ctx = await ContextoAsync();
            var acceso = await ctx.Db.UsuarioBoxes.AsNoTracking()
                .SingleAsync(ub => ub.UsuarioId == ctx.Admin.Id && ub.BoxId == ctx.Box.Id);
            var mia = await ctx.Db.Personas.AsNoTracking().SingleAsync(p => p.Id == acceso.PersonaId);

            await Assert.ThrowsAsync<ConflictException>(() => ctx.Servicio.CambiarEstadoAsync(mia.PublicId));
        }

        [Fact]
        public async Task No_se_puede_desactivar_al_unico_admin_y_no_queda_nada_a_medias()
        {
            var box = await factory.CrearBoxAsync();
            var unicoAdmin = await factory.CrearUsuarioAsync(box);
            var otro = await factory.CrearUsuarioAsync();   // sin acceso a este box: no es "uno mismo"
            using var scope = factory.ScopeDeUsuario(otro.Id, box);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var servicio = scope.ServiceProvider.GetRequiredService<IPersonaService>();

            var acceso = await db.UsuarioBoxes.AsNoTracking().SingleAsync(ub => ub.UsuarioId == unicoAdmin.Id);
            var persona = await db.Personas.AsNoTracking().SingleAsync(p => p.Id == acceso.PersonaId);

            await Assert.ThrowsAsync<ConflictException>(() => servicio.CambiarEstadoAsync(persona.PublicId));
            Assert.True(await db.Personas.AsNoTracking().Where(p => p.Id == persona.Id).Select(p => p.Activo).SingleAsync());
        }
```

**Commit:** `feat(personas): activar y desactivar persona`

---

## Etapa 5 — Contactos de emergencia (RF-CEM-001 a 003)

### 5.1 Interfaz

`IPersonaService`:

```csharp
        Task<ContactoEmergenciaResponse> AgregarContactoAsync(Guid personaPublicId, ContactoEmergenciaRequest request, CancellationToken ct = default);
        Task<ContactoEmergenciaResponse> EditarContactoAsync(Guid personaPublicId, Guid contactoPublicId, ContactoEmergenciaRequest request, CancellationToken ct = default);
        Task EliminarContactoAsync(Guid personaPublicId, Guid contactoPublicId, CancellationToken ct = default);
```

### 5.2 Servicio

`PersonaService`:

```csharp
        public async Task<ContactoEmergenciaResponse> AgregarContactoAsync(
            Guid personaPublicId, ContactoEmergenciaRequest request, CancellationToken ct = default)
        {
            var persona = await ObtenerEntidadAsync(personaPublicId, ct);

            var actuales = await Contactos.AsQueryable().CountAsync(c => c.PersonaId == persona.Id, ct);
            if (actuales >= MaxContactos)
                throw new ConflictException($"Una persona puede tener máximo {MaxContactos} contactos de emergencia.");

            var contacto = NuevoContacto(persona.Id, request);
            await Contactos.AddAsync(contacto, ct);
            await uow.SaveAsync(ct);
            return ContactoResponse(contacto);
        }

        public async Task<ContactoEmergenciaResponse> EditarContactoAsync(
            Guid personaPublicId, Guid contactoPublicId, ContactoEmergenciaRequest request, CancellationToken ct = default)
        {
            var contacto = await ObtenerContactoAsync(personaPublicId, contactoPublicId, ct);
            AplicarContacto(contacto, request);
            await uow.SaveAsync(ct);
            return ContactoResponse(contacto);
        }

        public async Task EliminarContactoAsync(Guid personaPublicId, Guid contactoPublicId, CancellationToken ct = default)
        {
            var contacto = await ObtenerContactoAsync(personaPublicId, contactoPublicId, ct);
            Contactos.Remove(contacto);   // físico: la bitácora guarda lo que tenía (RN-001)
            await uow.SaveAsync(ct);
        }

        /// <summary>El contacto debe ser de esa persona; si no, 404 (CA-002 de RF-CEM-002).</summary>
        private async Task<ContactoEmergencia> ObtenerContactoAsync(Guid personaPublicId, Guid contactoPublicId, CancellationToken ct)
        {
            var persona = await ObtenerEntidadAsync(personaPublicId, ct);

            return await Contactos.AsQueryable()
                .FirstOrDefaultAsync(c => c.PublicId == contactoPublicId && c.PersonaId == persona.Id, ct)
                ?? throw new NotFoundException("Contacto de emergencia", contactoPublicId);
        }

        private static ContactoEmergenciaResponse ContactoResponse(ContactoEmergencia c) =>
            new(c.PublicId, c.Nombre, c.Parentesco, c.Telefono);
```

### 5.3 Controller

```csharp
        [HttpPost("{publicId:guid}/contactos")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> AgregarContacto(
            Guid publicId, [FromBody] ContactoEmergenciaRequest request, CancellationToken ct)
        {
            var contacto = await service.AgregarContactoAsync(publicId, request, ct);
            return CreatedAtAction(nameof(Obtener), new { publicId }, contacto);
        }

        [HttpPut("{publicId:guid}/contactos/{contactoPublicId:guid}")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> EditarContacto(
            Guid publicId, Guid contactoPublicId, [FromBody] ContactoEmergenciaRequest request, CancellationToken ct) =>
            Ok(await service.EditarContactoAsync(publicId, contactoPublicId, request, ct));

        [HttpDelete("{publicId:guid}/contactos/{contactoPublicId:guid}")]
        [Authorize(Roles = nameof(RolUsuario.ADMIN))]
        public async Task<IActionResult> EliminarContacto(Guid publicId, Guid contactoPublicId, CancellationToken ct)
        {
            await service.EliminarContactoAsync(publicId, contactoPublicId, ct);
            return NoContent();
        }
```

### 5.4 Test

```csharp
        [Fact]
        public async Task Contactos_maximo_tres_solo_de_su_persona_y_se_eliminan()
        {
            using var ctx = await ContextoAsync();
            var ana = await ctx.Servicio.CrearAsync(new("Ana", "López", null, null,
                Contactos: [new("A", null, "6620000001"), new("B", null, "6620000002")]));

            var tercero = await ctx.Servicio.AgregarContactoAsync(ana.PublicId, new("C", "Hermana", "6620000003"));
            await Assert.ThrowsAsync<ConflictException>(() =>
                ctx.Servicio.AgregarContactoAsync(ana.PublicId, new("D", null, "6620000004")));

            var editado = await ctx.Servicio.EditarContactoAsync(ana.PublicId, tercero.PublicId, new("Carla", "Hermana", "6620000033"));
            Assert.Equal("Carla", editado.Nombre);

            var beto = await ctx.Servicio.CrearAsync(new("Beto", "García", null, null));
            await Assert.ThrowsAsync<NotFoundException>(() =>
                ctx.Servicio.EditarContactoAsync(beto.PublicId, tercero.PublicId, new("X", null, "6620000000")));

            await ctx.Servicio.EliminarContactoAsync(ana.PublicId, tercero.PublicId);
            Assert.Equal(2, (await ctx.Servicio.ObtenerAsync(ana.PublicId)).Contactos.Count);
        }
```

**Commit:** `feat(personas): contactos de emergencia`

---

## Checklist del módulo

- [ ] Paso 0: `TipoPersona`, `Texto`, `CrearCoachAsync`
- [ ] Etapa 1: listado con búsqueda, tipo y paginación; detalle con contactos
- [ ] Etapa 2: alta con código de 6 dígitos y contactos en una transacción; 400 en validaciones; 403 para Coach
- [ ] Etapa 3: edición sin tocar el código; seed con `EsCoach = true`
- [ ] Etapa 4: desactivar quita acceso y sesiones; no a uno mismo ni al único admin; reactivar no devuelve acceso
- [ ] Etapa 5: contactos máximo 3, solo de su persona, borrado físico
- [ ] Suite completa en verde
- [ ] Postman: carpeta Personas probada con Admin y con un Coach
