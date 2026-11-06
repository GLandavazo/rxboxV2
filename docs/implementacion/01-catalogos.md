# Implementación 01 — Catálogos

Requerimientos: `docs/requerimientos/01-catalogos.md` (RF-CAF-001, RF-CAT-001 a 004).

Archivos que se crean o tocan:

```
server/
├── API/
│   ├── Program.cs                                        (paso 0)
│   ├── Extensions/ServiceExtensions.cs                   (registro)
│   └── Controllers/
│       ├── CatalogosController.cs                        (nuevo)
│       └── CatalogosFijosController.cs                   (nuevo)
├── Core/
│   ├── Enums/EnumsSistema.cs                             (paso 0)
│   ├── DTOs/Requests/Catalogos/CatalogoRequests.cs       (nuevo)
│   ├── DTOs/Responses/Catalogos/CatalogoResponses.cs     (nuevo)
│   └── Interfaces/
│       ├── ICatalogoService.cs                           (nuevo)
│       └── ICatalogoFijoService.cs                       (nuevo)
├── Infrastructure/Services/Catalogos/
│   ├── CatalogoService.cs                                (nuevo)
│   └── CatalogoFijoService.cs                            (nuevo)
└── Tests/CatalogoTests.cs                                (nuevo)
```

---

## Paso 0 — Preparación

### 0.1 Enums como texto en JSON

Hoy `AddControllers()` serializa los enums como número (`"tipo": 1`). El esquema guarda texto y el front debe mandar y recibir `"METODO_PAGO"`. En `API/Program.cs`:

```csharp
// arriba
using System.Text.Json.Serialization;

// reemplaza: builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false)));
```

`allowIntegerValues: false` hace que `"tipo": 99` responda 400 en vez de colarse como un valor inválido.

> Revisa después de este cambio que los tests de Auth sigan pasando: `LoginResponse` y `BoxOpcionResponse` usan `string` para el rol, así que no deberían cambiar.

### 0.2 Descripciones legibles en los enums

La descripción de un catálogo fijo se arma sola a partir del valor (`PRIMERA_VISITA` → "Primera visita"). Solo los que no salen bien llevan `[Description]`. En `Core/Enums/EnumsSistema.cs`:

```csharp
using System.ComponentModel;

namespace RxBox.Core.Enums
{
    public enum TipoCatalogo
    {
        [Description("Categoría de membresía")] CATEGORIA_MEMBRESIA,
        [Description("Método de pago")] METODO_PAGO
    }

    // ... TipoCobertura, ExpiraDesde, EstadoClase, EstadoInscripcion, CanalReserva sin cambios

    public enum CanalCheckin
    {
        COACH,
        PORTAL,
        [Description("QR")] QR
    }
}
```

`[Description]` no cambia el valor guardado en BD ni el del JSON; no hace falta migración.

---

## Etapa 1 — Listado (RF-CAF-001, RF-CAT-001)

### 1.1 DTOs de respuesta

`Core/DTOs/Responses/Catalogos/CatalogoResponses.cs`

```csharp
using RxBox.Core.Enums;

namespace RxBox.Core.DTOs.Responses.Catalogos
{
    public record CatalogoResponse(Guid PublicId, TipoCatalogo Tipo, string Nombre, short Orden, bool Activo);

    public record CatalogoFijoResponse(string Valor, string Descripcion);
}
```

### 1.2 Interfaces

`Core/Interfaces/ICatalogoService.cs`

```csharp
using RxBox.Core.DTOs.Responses.Catalogos;
using RxBox.Core.Enums;

namespace RxBox.Core.Interfaces
{
    public interface ICatalogoService
    {
        Task<IReadOnlyList<CatalogoResponse>> ListarAsync(TipoCatalogo? tipo, bool soloActivos, CancellationToken ct = default);
        Task<CatalogoResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default);
    }
}
```

`Core/Interfaces/ICatalogoFijoService.cs`

```csharp
using RxBox.Core.DTOs.Responses.Catalogos;

namespace RxBox.Core.Interfaces
{
    public interface ICatalogoFijoService
    {
        IReadOnlyList<string> Nombres();
        IReadOnlyList<CatalogoFijoResponse> Obtener(string nombre);
    }
}
```

### 1.3 Servicios

`Infrastructure/Services/Catalogos/CatalogoFijoService.cs`

```csharp
using System.ComponentModel;
using System.Reflection;
using RxBox.Core.DTOs.Responses.Catalogos;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;

namespace RxBox.Infrastructure.Services.Catalogos
{
    /// <summary>
    /// Catálogos fijos = enums de sistema. Para exponer otro, agrega una línea al diccionario.
    /// </summary>
    public class CatalogoFijoService : ICatalogoFijoService
    {
        private static readonly Dictionary<string, Type> Catalogos = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tipos-cobertura"] = typeof(TipoCobertura),
            ["expira-desde"] = typeof(ExpiraDesde),
            ["estados-clase"] = typeof(EstadoClase),
            ["estados-inscripcion"] = typeof(EstadoInscripcion),
            ["canales-reserva"] = typeof(CanalReserva),
            ["canales-checkin"] = typeof(CanalCheckin),
            ["roles-usuario"] = typeof(RolUsuario),
            ["tipos-catalogo"] = typeof(TipoCatalogo)
        };

        public IReadOnlyList<string> Nombres() => [.. Catalogos.Keys.Order()];

        public IReadOnlyList<CatalogoFijoResponse> Obtener(string nombre)
        {
            if (!Catalogos.TryGetValue(nombre, out var tipo))
                throw new NotFoundException("Catálogo fijo", nombre);

            // Enum.GetNames respeta el orden de declaración.
            return [.. Enum.GetNames(tipo).Select(valor => new CatalogoFijoResponse(valor, Descripcion(tipo, valor)))];
        }

        private static string Descripcion(Type tipo, string valor)
        {
            var atributo = tipo.GetField(valor)?.GetCustomAttribute<DescriptionAttribute>();
            if (atributo is not null) return atributo.Description;

            var texto = valor.Replace('_', ' ').ToLowerInvariant();
            return char.ToUpperInvariant(texto[0]) + texto[1..];
        }
    }
}
```

`Infrastructure/Services/Catalogos/CatalogoService.cs` (se completa en las siguientes etapas)

```csharp
using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Responses.Catalogos;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Core.Interfaces.Repositories;

namespace RxBox.Infrastructure.Services.Catalogos
{
    public class CatalogoService(IUnitOfWork uow) : ICatalogoService
    {
        private IGenericRepository<Catalogo> Repo => uow.Repository<Catalogo>();

        public async Task<IReadOnlyList<CatalogoResponse>> ListarAsync(
            TipoCatalogo? tipo, bool soloActivos, CancellationToken ct = default)
        {
            var query = Repo.AsQueryable().AsNoTracking();

            if (tipo is not null) query = query.Where(c => c.Tipo == tipo.Value);
            if (soloActivos) query = query.Where(c => c.Activo);

            return await query
                .OrderBy(c => c.Tipo).ThenBy(c => c.Orden).ThenBy(c => c.Nombre)
                .Select(c => new CatalogoResponse(c.PublicId, c.Tipo, c.Nombre, c.Orden, c.Activo))
                .ToListAsync(ct);
        }

        public async Task<CatalogoResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default) =>
            ToResponse(await ObtenerEntidadAsync(publicId, ct));

        // El query filter de tenant hace que un publicId de otro box también dé 404.
        private async Task<Catalogo> ObtenerEntidadAsync(Guid publicId, CancellationToken ct) =>
            await Repo.GetByPublicIdAsync(publicId, ct)
                ?? throw new NotFoundException("Catálogo", publicId);

        private static CatalogoResponse ToResponse(Catalogo c) =>
            new(c.PublicId, c.Tipo, c.Nombre, c.Orden, c.Activo);
    }
}
```

### 1.4 Registro

En `API/Extensions/ServiceExtensions.cs`, dentro de `AddAppServices`:

```csharp
// arriba
using RxBox.Infrastructure.Services.Catalogos;

// dentro de AddAppServices
services.AddScoped<ICatalogoService, CatalogoService>();
services.AddSingleton<ICatalogoFijoService, CatalogoFijoService>(); // sin estado ni BD
```

### 1.5 Controllers

`API/Controllers/CatalogosFijosController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/catalogos-fijos")]
    [Authorize]
    public class CatalogosFijosController(ICatalogoFijoService service) : ControllerBase
    {
        [HttpGet]
        public IActionResult Nombres() => Ok(service.Nombres());

        [HttpGet("{nombre}")]
        public IActionResult Obtener(string nombre) => Ok(service.Obtener(nombre));
    }
}
```

`API/Controllers/CatalogosController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/catalogos")]
    [Authorize]
    public class CatalogosController(ICatalogoService service) : ControllerBase
    {
        /// <summary>GET /api/catalogos?tipo=METODO_PAGO&amp;soloActivos=true</summary>
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] TipoCatalogo? tipo, [FromQuery] bool soloActivos = true, CancellationToken ct = default) =>
            Ok(await service.ListarAsync(tipo, soloActivos, ct));

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Obtener(Guid publicId, CancellationToken ct) =>
            Ok(await service.ObtenerAsync(publicId, ct));
    }
}
```

Un `?tipo=FOO` no se puede convertir al enum, y `[ApiController]` responde 400 solo (CA-004).

### 1.6 Prueba manual

Con la API corriendo, en Postman: `Auth / Login`, luego `Catálogos fijos / Nombres`, `Catálogos fijos / Tipos de cobertura` y `Catálogos / Listar métodos de pago`. Deberías ver Efectivo, Transferencia y Tarjeta del seed.

**Commit:** `feat(catalogos): listado de catálogos fijos y editables`

---

## Etapa 2 — Agregar (RF-CAT-002)

### 2.1 DTO de entrada

`Core/DTOs/Requests/Catalogos/CatalogoRequests.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using RxBox.Core.Enums;

namespace RxBox.Core.DTOs.Requests.Catalogos
{
    // Tipo es nullable solo para que [Required] detecte cuando no viene.
    public record CatalogoCreateRequest(
        [Required] TipoCatalogo? Tipo,
        [Required, StringLength(60)] string Nombre,
        short? Orden);
}
```

`[Required]` en un string rechaza `""` y `"   "`, así que un nombre de puros espacios responde 400 (CA-005).

### 2.2 Interfaz

Agrega a `ICatalogoService`:

```csharp
// arriba
using RxBox.Core.DTOs.Requests.Catalogos;

Task<CatalogoResponse> CrearAsync(CatalogoCreateRequest request, CancellationToken ct = default);
```

### 2.3 Servicio

Agrega a `CatalogoService` (y `using RxBox.Core.DTOs.Requests.Catalogos;` arriba):

```csharp
public async Task<CatalogoResponse> CrearAsync(CatalogoCreateRequest request, CancellationToken ct = default)
{
    var tipo = request.Tipo!.Value;
    var nombre = Normalizar(request.Nombre);

    await ValidarNombreDisponibleAsync(tipo, nombre, excluir: null, ct);

    var catalogo = new Catalogo
    {
        Tipo = tipo,
        Nombre = nombre,
        Orden = request.Orden ?? await SiguienteOrdenAsync(tipo, ct)
    };

    await Repo.AddAsync(catalogo, ct);
    await uow.SaveAsync(ct);   // el interceptor pone BoxId y auditoría
    return ToResponse(catalogo);
}

// "  Pago   en línea " → "Pago en línea"
private static string Normalizar(string nombre) =>
    string.Join(' ', nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries));

/// <summary>Único por box y tipo, sin distinguir mayúsculas, contando inactivos.</summary>
private async Task ValidarNombreDisponibleAsync(TipoCatalogo tipo, string nombre, Guid? excluir, CancellationToken ct)
{
    var nombreLower = nombre.ToLower();

    var existente = await Repo.AsQueryable().AsNoTracking()
        .Where(c => c.Tipo == tipo
                 && c.Nombre.ToLower() == nombreLower
                 && (excluir == null || c.PublicId != excluir.Value))
        .Select(c => new { c.Nombre, c.Activo })
        .FirstOrDefaultAsync(ct);

    if (existente is null) return;

    throw new ConflictException(existente.Activo
        ? $"Ya existe '{existente.Nombre}' en este catálogo."
        : $"Ya existe '{existente.Nombre}' pero está inactivo. Reactívalo en lugar de crear otro.");
}

private async Task<short> SiguienteOrdenAsync(TipoCatalogo tipo, CancellationToken ct)
{
    var maximo = await Repo.AsQueryable()
        .Where(c => c.Tipo == tipo)
        .MaxAsync(c => (short?)c.Orden, ct);   // null si no hay ninguno

    return (short)((maximo ?? 0) + 1);
}
```

> El índice único `(box_id, tipo, nombre)` de la BD distingue mayúsculas; la validación sin distinguirlas vive aquí. Si dos admins crean el mismo nombre en el mismo instante, el segundo choca con el índice y responde 500 en vez de 409. Es poco probable en catálogos; no vale la pena cubrirlo ahora.

### 2.4 Controller

Agrega a `CatalogosController` (y `using RxBox.Core.DTOs.Requests.Catalogos;` arriba):

```csharp
[HttpPost]
[Authorize(Roles = nameof(RolUsuario.ADMIN))]
public async Task<IActionResult> Crear([FromBody] CatalogoCreateRequest request, CancellationToken ct)
{
    var creado = await service.CrearAsync(request, ct);
    return CreatedAtAction(nameof(Obtener), new { publicId = creado.PublicId }, creado);
}
```

### 2.5 Prueba manual

`Catálogos / Agregar método de pago` con `"Mercado Pago"` → 201. Mándalo otra vez → 409. Con `"nombre": "   "` → 400.

**Commit:** `feat(catalogos): agregar elemento`

---

## Etapa 3 — Editar (RF-CAT-003)

### 3.1 DTO

Agrega a `CatalogoRequests.cs`:

```csharp
// Sin Tipo: no se puede cambiar después de crear (RN-001).
public record CatalogoUpdateRequest(
    [Required, StringLength(60)] string Nombre,
    short? Orden);
```

### 3.2 Interfaz

Agrega a `ICatalogoService`:

```csharp
Task<CatalogoResponse> EditarAsync(Guid publicId, CatalogoUpdateRequest request, CancellationToken ct = default);
```

### 3.3 Servicio

Agrega a `CatalogoService`:

```csharp
public async Task<CatalogoResponse> EditarAsync(Guid publicId, CatalogoUpdateRequest request, CancellationToken ct = default)
{
    var catalogo = await ObtenerEntidadAsync(publicId, ct);   // rastreada
    var nombre = Normalizar(request.Nombre);

    // Se excluye a sí mismo: "Efectivo" → "EFECTIVO" es válido (CA-003).
    await ValidarNombreDisponibleAsync(catalogo.Tipo, nombre, excluir: catalogo.PublicId, ct);

    catalogo.Nombre = nombre;
    if (request.Orden is not null) catalogo.Orden = request.Orden.Value;

    await uow.SaveAsync(ct);   // el interceptor pone EditadoPor y FechaEdicion
    return ToResponse(catalogo);
}
```

### 3.4 Controller

```csharp
[HttpPut("{publicId:guid}")]
[Authorize(Roles = nameof(RolUsuario.ADMIN))]
public async Task<IActionResult> Editar(Guid publicId, [FromBody] CatalogoUpdateRequest request, CancellationToken ct) =>
    Ok(await service.EditarAsync(publicId, request, ct));
```

### 3.5 Prueba manual

`Catálogos / Editar` sobre Mercado Pago → "Mercado Pago QR" → 200. Renombrarlo a "Efectivo" → 409.

**Commit:** `feat(catalogos): editar elemento`

---

## Etapa 4 — Activar / desactivar (RF-CAT-004)

### 4.1 Interfaz

Agrega a `ICatalogoService`:

```csharp
Task<CatalogoResponse> CambiarEstadoAsync(Guid publicId, CancellationToken ct = default);
```

### 4.2 Servicio

Agrega a `CatalogoService`:

```csharp
public async Task<CatalogoResponse> CambiarEstadoAsync(Guid publicId, CancellationToken ct = default)
{
    var catalogo = await ObtenerEntidadAsync(publicId, ct);

    if (catalogo.Activo)
    {
        var activosDelTipo = await Repo.AsQueryable()
            .CountAsync(c => c.Tipo == catalogo.Tipo && c.Activo, ct);

        if (activosDelTipo <= 1)
            throw new ConflictException("Debe quedar al menos un elemento activo en este catálogo.");
    }

    catalogo.Activo = !catalogo.Activo;
    await uow.SaveAsync(ct);
    return ToResponse(catalogo);
}
```

### 4.3 Controller

```csharp
[HttpPatch("{publicId:guid}/toggle")]
[Authorize(Roles = nameof(RolUsuario.ADMIN))]
public async Task<IActionResult> CambiarEstado(Guid publicId, CancellationToken ct) =>
    Ok(await service.CambiarEstadoAsync(publicId, ct));
```

### 4.4 Prueba manual

`Catálogos / Activar-desactivar` sobre Mercado Pago → `activo: false`. `Listar métodos de pago` ya no lo trae; con `soloActivos=false` sí. Desactiva todos menos uno e intenta el último → 409.

**Commit:** `feat(catalogos): activar y desactivar elemento`

---

## Tests

`Tests/CatalogoTests.cs`. Cada test crea su propio box, así no dependen del seed ni entre sí.

```csharp
using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.DTOs.Requests.Catalogos;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Infrastructure.Services.Catalogos;

namespace RxBox.Tests
{
    [Collection(ApiCollection.Nombre)]
    public class CatalogoTests(RxBoxApiFactory factory)
    {
        private const TipoCatalogo Metodo = TipoCatalogo.METODO_PAGO;

        private async Task<IServiceScope> ScopeConBoxNuevoAsync()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);
            return factory.ScopeDeUsuario(usuario.Id, box);
        }

        private static ICatalogoService Servicio(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<ICatalogoService>();

        [Fact]
        public async Task Crear_sin_orden_asigna_el_siguiente()
        {
            using var scope = await ScopeConBoxNuevoAsync();
            var service = Servicio(scope);

            var a = await service.CrearAsync(new(Metodo, "Efectivo", null));
            var b = await service.CrearAsync(new(Metodo, "  Pago   en línea ", null));

            Assert.Equal((short)1, a.Orden);
            Assert.Equal((short)2, b.Orden);
            Assert.Equal("Pago en línea", b.Nombre);
        }

        [Fact]
        public async Task Crear_duplicado_sin_distinguir_mayusculas_es_conflicto()
        {
            using var scope = await ScopeConBoxNuevoAsync();
            var service = Servicio(scope);
            await service.CrearAsync(new(Metodo, "Efectivo", null));

            await Assert.ThrowsAsync<ConflictException>(() => service.CrearAsync(new(Metodo, " efectivo ", null)));
        }

        [Fact]
        public async Task Crear_duplicado_de_un_inactivo_pide_reactivarlo()
        {
            using var scope = await ScopeConBoxNuevoAsync();
            var service = Servicio(scope);
            var efectivo = await service.CrearAsync(new(Metodo, "Efectivo", null));
            await service.CrearAsync(new(Metodo, "Tarjeta", null));
            await service.CambiarEstadoAsync(efectivo.PublicId);

            var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CrearAsync(new(Metodo, "Efectivo", null)));
            Assert.Contains("inactivo", ex.Message);
        }

        [Fact]
        public async Task Editar_permite_cambiar_mayusculas_del_mismo_nombre_pero_no_tomar_otro()
        {
            using var scope = await ScopeConBoxNuevoAsync();
            var service = Servicio(scope);
            var efectivo = await service.CrearAsync(new(Metodo, "Efectivo", null));
            await service.CrearAsync(new(Metodo, "Tarjeta", null));

            var editado = await service.EditarAsync(efectivo.PublicId, new("EFECTIVO", null));
            Assert.Equal("EFECTIVO", editado.Nombre);
            Assert.Equal(efectivo.Orden, editado.Orden);

            await Assert.ThrowsAsync<ConflictException>(() => service.EditarAsync(efectivo.PublicId, new("tarjeta", null)));
        }

        [Fact]
        public async Task No_se_puede_desactivar_el_ultimo_activo_del_tipo()
        {
            using var scope = await ScopeConBoxNuevoAsync();
            var service = Servicio(scope);
            var unico = await service.CrearAsync(new(Metodo, "Efectivo", null));

            await Assert.ThrowsAsync<ConflictException>(() => service.CambiarEstadoAsync(unico.PublicId));
        }

        [Fact]
        public async Task Listar_solo_activos_omite_los_desactivados()
        {
            using var scope = await ScopeConBoxNuevoAsync();
            var service = Servicio(scope);
            await service.CrearAsync(new(Metodo, "Efectivo", null));
            var tarjeta = await service.CrearAsync(new(Metodo, "Tarjeta", null));
            await service.CambiarEstadoAsync(tarjeta.PublicId);

            var activos = await service.ListarAsync(Metodo, soloActivos: true);
            var todos = await service.ListarAsync(Metodo, soloActivos: false);

            Assert.Equal(["Efectivo"], activos.Select(c => c.Nombre));
            Assert.Equal(2, todos.Count);
        }

        [Fact]
        public async Task Obtener_de_otro_box_es_no_encontrado()
        {
            Guid idDelBoxA;
            using (var scopeA = await ScopeConBoxNuevoAsync())
                idDelBoxA = (await Servicio(scopeA).CrearAsync(new(Metodo, "Efectivo", null))).PublicId;

            using var scopeB = await ScopeConBoxNuevoAsync();
            await Assert.ThrowsAsync<NotFoundException>(() => Servicio(scopeB).ObtenerAsync(idDelBoxA));
        }

        [Fact]
        public void Catalogo_fijo_regresa_valores_en_orden_con_descripcion()
        {
            var service = new CatalogoFijoService();

            var valores = service.Obtener("expira-desde");

            Assert.Equal(["COMPRA", "PRIMERA_VISITA"], valores.Select(v => v.Valor));
            Assert.Equal("Primera visita", valores[1].Descripcion);
            Assert.Equal("QR", service.Obtener("canales-checkin")[2].Descripcion);
        }

        [Fact]
        public void Catalogo_fijo_inexistente_es_no_encontrado()
        {
            Assert.Throws<NotFoundException>(() => new CatalogoFijoService().Obtener("no-existe"));
        }
    }
}
```

Corre con `dotnet test` desde `server/` (necesita Docker por Testcontainers).

---

## Checklist del módulo

- [ ] Paso 0: enums como texto en JSON y `[Description]` en `TipoCatalogo` y `CanalCheckin.QR`
- [ ] Etapa 1: listado de fijos y editables, detalle por publicId
- [ ] Etapa 2: agregar con orden automático y nombre único
- [ ] Etapa 3: editar sin cambiar tipo
- [ ] Etapa 4: activar / desactivar, sin dejar el tipo sin activos
- [ ] Tests en verde
- [ ] Probado en Postman con Admin; un Coach recibe 403 en POST, PUT y PATCH
