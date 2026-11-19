# Implementación 00 — Transversal: bitácora e historial de membresías

Requerimientos: `docs/requerimientos/00-transversal.md` (RF-TRV-001 a 004).

Archivos:

```
server/
├── API/
│   ├── Program.cs                                             (paso 0, pendiente del módulo 01)
│   ├── Extensions/ServiceExtensions.cs                        (etapa 4)
│   └── Controllers/BitacoraController.cs                      (nuevo, etapa 4)
├── Core/
│   ├── Enums/EnumsSistema.cs                                  (etapa 1)
│   ├── Entities/Bitacora.cs                                   (nuevo)
│   ├── Entities/MembresiaEvento.cs                            (nuevo)
│   ├── Entities/Membresia.cs                                  (+ MembresiaAnteriorId)
│   ├── Interfaces/ICurrentUserService.cs                      (+ Ip)
│   ├── Interfaces/IBitacoraService.cs                         (nuevo)
│   ├── DTOs/Requests/Comun/PaginacionRequest.cs               (nuevo, se reusa en Personas)
│   ├── DTOs/Responses/Comun/PaginaResponse.cs                 (nuevo, se reusa en Personas)
│   ├── DTOs/Requests/Auditoria/BitacoraFiltroRequest.cs       (nuevo)
│   └── DTOs/Responses/Auditoria/BitacoraResponse.cs           (nuevo)
├── Infrastructure/
│   ├── Auth/CurrentUserService.cs                             (+ Ip)
│   ├── Auth/AuthService.cs                                    (etapa 3)
│   ├── Data/AppDbContext.cs                                   (+ DbSets)
│   ├── Data/Configurations/BitacoraConfiguration.cs           (nuevo)
│   ├── Data/Configurations/MembresiaEventoConfiguration.cs    (nuevo)
│   ├── Data/Configurations/MembresiaConfiguration.cs          (+ membresia_anterior_id)
│   ├── Data/Interceptors/AuditoriaSaveChangesInterceptor.cs   (etapa 2)
│   ├── Data/Migrations/…_Transversal.cs                       (generada)
│   ├── Services/Comun/PaginacionExtensions.cs                 (nuevo)
│   └── Services/Auditoria/BitacoraService.cs                  (nuevo)
└── Tests/
    ├── RxBoxApiFactory.cs                                     (+ CrearClienteAsync)
    └── BitacoraTests.cs                                       (nuevo)
```

> La carpeta de servicios se llama `Auditoria`, no `Bitacora`: un namespace `…Services.Bitacora` chocaría con la clase `Bitacora`.

---

## Paso 0 — Pendiente del módulo 01: enums como texto en JSON

`API/Program.cs` todavía tiene `builder.Services.AddControllers();` sin convertidor. Hoy `GET /api/catalogos` regresa `"tipo": 1` en lugar de `"tipo": "METODO_PAGO"`. Los tests de Catálogos pasan porque llaman al servicio directo, no por HTTP.

```csharp
// arriba
using System.Text.Json.Serialization;

// reemplaza: builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false)));
```

Comprueba en Postman: `Catálogos / Listar métodos de pago` debe mostrar `"tipo": "METODO_PAGO"`.

**Commit:** `fix(api): serializar enums como texto`

---

## Etapa 1 — Estructura (RF-TRV-004 y tablas de RF-TRV-001)

### 1.1 Enums

Agrega al final de `Core/Enums/EnumsSistema.cs`:

```csharp
    public enum AccionBitacora
    {
        CREAR,
        EDITAR,
        ELIMINAR,
        LOGIN,
        [Description("Login fallido")] LOGIN_FALLIDO,
        [Description("Cambio de box")] CAMBIO_BOX
    }

    public enum TipoEventoMembresia
    {
        ALTA,
        [Description("Renovación")] RENOVACION,
        PAGO,
        [Description("Pago anulado")] PAGO_ANULADO,
        [Description("Extensión")] EXTENSION,
        [Description("Cancelación")] CANCELACION
    }
```

### 1.2 Entidades

`Core/Entities/Bitacora.cs`

```csharp
using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    /// <summary>
    /// Bitácora técnica. Insert-only y sin FK hacia lo que registra, para sobrevivir a
    /// borrados. No es ITenantEntity (hay eventos sin box, como un login fallido):
    /// las consultas filtran por box a mano.
    /// </summary>
    public class Bitacora
    {
        public long Id { get; set; }
        public int? BoxId { get; set; }
        public int? UsuarioId { get; set; }
        public string Entidad { get; set; } = null!;
        public Guid? EntidadPublicId { get; set; }
        public AccionBitacora Accion { get; set; }

        /// <summary>jsonb: <c>{ "columna": { "antes": x, "despues": y } }</c>, o datos del acceso en LOGIN*.</summary>
        public string? Cambios { get; set; }

        public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
        public string? Ip { get; set; }
    }
}
```

`Core/Entities/MembresiaEvento.cs`

```csharp
using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    /// <summary>
    /// Historial de negocio de una membresía. Lo escribe el servicio de membresías/pagos
    /// (módulo 04), no el interceptor. Insert-only, sin public_id ni auditoría.
    /// </summary>
    public class MembresiaEvento : ITenantEntity
    {
        public int Id { get; set; }
        public int BoxId { get; set; }
        public int MembresiaId { get; set; }
        public int PersonaId { get; set; }
        public int? PagoId { get; set; }
        public TipoEventoMembresia Tipo { get; set; }
        public string Descripcion { get; set; } = null!;

        /// <summary>jsonb con el detalle, ej. EXTENSION: { fecha_fin_antes, fecha_fin_despues, dias, motivo }.</summary>
        public string? Datos { get; set; }

        public int? UsuarioId { get; set; }
        public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    }
}
```

En `Core/Entities/Membresia.cs`, debajo de `TipoMembresiaId`:

```csharp
        /// <summary>La membresía que esta renueva. Arma la cadena de renovaciones.</summary>
        public int? MembresiaAnteriorId { get; set; }
```

### 1.3 Configuraciones

`Infrastructure/Data/Configurations/BitacoraConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class BitacoraConfiguration : IEntityTypeConfiguration<Bitacora>
    {
        public void Configure(EntityTypeBuilder<Bitacora> builder)
        {
            builder.ToTable("bitacora", t =>
                t.HasCheckConstraint("ck_bitacora_accion", ConfigurationExtensions.ValoresDe<AccionBitacora>("accion")));

            builder.Property(b => b.Accion).HasConversion<string>();
            builder.Property(b => b.Cambios).HasColumnType("jsonb");

            builder.HasIndex(b => new { b.BoxId, b.Entidad, b.EntidadPublicId });
            builder.HasIndex(b => new { b.BoxId, b.Fecha });
        }
    }
}
```

`Infrastructure/Data/Configurations/MembresiaEventoConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class MembresiaEventoConfiguration : IEntityTypeConfiguration<MembresiaEvento>
    {
        public void Configure(EntityTypeBuilder<MembresiaEvento> builder)
        {
            builder.ToTable("membresia_eventos", t =>
                t.HasCheckConstraint("ck_membresia_eventos_tipo", ConfigurationExtensions.ValoresDe<TipoEventoMembresia>("tipo")));

            builder.Property(e => e.Tipo).HasConversion<string>();
            builder.Property(e => e.Datos).HasColumnType("jsonb");

            builder.HasIndex(e => new { e.BoxId, e.MembresiaId, e.Fecha });
            builder.HasIndex(e => new { e.BoxId, e.PersonaId, e.Fecha });

            builder.HasTenantFk<Membresia>(nameof(MembresiaEvento.MembresiaId));
            builder.HasTenantFk<Persona>(nameof(MembresiaEvento.PersonaId));
            builder.HasTenantFk<Pago>(nameof(MembresiaEvento.PagoId));
            builder.HasUsuarioFk(nameof(MembresiaEvento.UsuarioId));
        }
    }
}
```

`AppDbContext` ya le aplica el query filter y la FK a `boxes` porque implementa `ITenantEntity`.

En `MembresiaConfiguration.Configure`, al final:

```csharp
            builder.HasTenantFk<Membresia>(nameof(Membresia.MembresiaAnteriorId));

            // Una membresía solo se renueva una vez.
            builder.HasIndex(m => m.MembresiaAnteriorId).IsUnique()
                .HasFilter("membresia_anterior_id IS NOT NULL");
```

### 1.4 DbSets

En `AppDbContext`, junto a los demás:

```csharp
        public DbSet<Bitacora> Bitacora { get; set; }
        public DbSet<MembresiaEvento> MembresiaEventos { get; set; }
```

### 1.5 Migración

Desde `server/`:

```bash
dotnet ef migrations add Transversal -p Infrastructure -s API -o Data/Migrations
```

Revisa el archivo generado antes de aplicarlo. Debe:
- crear `bitacora` y `membresia_eventos` con sus CHECK e índices,
- agregar `membresia_anterior_id` a `membresias` con su FK compuesta y el índice único filtrado,
- **no** tocar ninguna otra tabla. Si toca algo más, el modelo no coincide con el snapshot y hay que revisarlo antes de seguir.

La API aplica la migración sola al arrancar (`MigrateAsync`).

**Commit:** `feat(transversal): tablas de bitácora y eventos de membresía`

---

## Etapa 2 — La bitácora se llena sola (RF-TRV-001)

### 2.1 IP del usuario actual

`Core/Interfaces/ICurrentUserService.cs`, agrega:

```csharp
        string? Ip { get; }
```

`Infrastructure/Auth/CurrentUserService.cs`, agrega:

```csharp
        public string? Ip => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
```

> Detrás de un proxy (Nginx, Render, etc.) esto da la IP del proxy. Al desplegar se agrega `app.UseForwardedHeaders()`; no hace falta ahora.

### 2.2 Interceptor

Reemplaza `Infrastructure/Data/Interceptors/AuditoriaSaveChangesInterceptor.cs` completo. `AsignarBox` y `AplicarAuditoria` quedan igual; lo nuevo es `RegistrarBitacora`, que corre al final para ver el `BoxId` ya asignado.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;

namespace RxBox.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// En cada SaveChanges: asigna <see cref="ITenantEntity.BoxId"/> a las filas nuevas,
    /// rellena <see cref="IAuditable"/> y escribe una fila de <see cref="Bitacora"/> por
    /// cada registro creado, modificado o borrado, en la misma transacción.
    /// </summary>
    public class AuditoriaSaveChangesInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
    {
        // No se registran: la propia bitácora, los tokens y el historial de negocio.
        private static readonly HashSet<Type> TiposExcluidos =
            [typeof(Bitacora), typeof(RefreshToken), typeof(MembresiaEvento)];

        // Nunca van en "cambios". Las de auditoría, id y box ya están en la fila.
        private static readonly HashSet<string> PropiedadesExcluidas =
        [
            nameof(BaseEntity.Id), nameof(BaseEntity.PublicId), nameof(ITenantEntity.BoxId),
            nameof(IAuditable.CreadoPor), nameof(IAuditable.FechaCreacion),
            nameof(IAuditable.EditadoPor), nameof(IAuditable.FechaEdicion),
            nameof(Usuario.PasswordHash), nameof(RefreshToken.TokenHash)
        ];

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,   // "antes" / "despues"
            Converters = { new JsonStringEnumConverter() }
        };

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            Preparar(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Preparar(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void Preparar(DbContext? context)
        {
            if (context is null) return;

            AsignarBox(context);
            AplicarAuditoria(context);
            RegistrarBitacora(context);
        }

        // Respeta un BoxId explícito: los procesos sin request (seed, alta de box) no tienen box actual.
        private void AsignarBox(DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.BoxId == 0)
                    entry.Entity.BoxId = currentUser.BoxId
                        ?? throw new InvalidOperationException(
                            $"No hay box actual para insertar {entry.Metadata.ClrType.Name}.");
            }
        }

        private void AplicarAuditoria(DbContext context)
        {
            var ahora = DateTimeOffset.UtcNow;
            var usuarioId = currentUser.UsuarioId;

            foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.FechaCreacion = ahora;
                        entry.Entity.CreadoPor ??= usuarioId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.FechaEdicion = ahora;
                        entry.Entity.EditadoPor = usuarioId;
                        break;
                }
            }
        }

        private void RegistrarBitacora(DbContext context)
        {
            var ahora = DateTimeOffset.UtcNow;
            var registros = new List<Bitacora>();

            // ToList: no se puede agregar al ChangeTracker mientras se recorre.
            foreach (var entry in context.ChangeTracker.Entries().ToList())
            {
                if (TiposExcluidos.Contains(entry.Metadata.ClrType)) continue;

                AccionBitacora? accion = entry.State switch
                {
                    EntityState.Added => AccionBitacora.CREAR,
                    EntityState.Modified => AccionBitacora.EDITAR,
                    EntityState.Deleted => AccionBitacora.ELIMINAR,
                    _ => null
                };
                if (accion is null) continue;

                var cambios = Cambios(entry);
                if (accion == AccionBitacora.EDITAR && cambios.Count == 0) continue;   // Update() sin cambios reales

                registros.Add(new Bitacora
                {
                    BoxId = BoxDe(entry),
                    UsuarioId = currentUser.UsuarioId,
                    Entidad = entry.Metadata.ClrType.Name,
                    EntidadPublicId = (entry.Entity as BaseEntity)?.PublicId,
                    Accion = accion.Value,
                    Cambios = cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios, Json),
                    Fecha = ahora,
                    Ip = currentUser.Ip
                });
            }

            context.Set<Bitacora>().AddRange(registros);
        }

        // El box del propio registro si tiene columna box_id (también usuario_boxes, que no es
        // ITenantEntity); si no (usuarios, boxes), el del usuario actual.
        private int? BoxDe(EntityEntry entry)
        {
            if (entry.Metadata.FindProperty(nameof(ITenantEntity.BoxId)) is null) return currentUser.BoxId;

            var propiedad = entry.Property(nameof(ITenantEntity.BoxId));
            return (int?)(entry.State == EntityState.Deleted ? propiedad.OriginalValue : propiedad.CurrentValue);
        }

        private static Dictionary<string, Cambio> Cambios(EntityEntry entry)
        {
            var cambios = new Dictionary<string, Cambio>();

            foreach (var p in entry.Properties)
            {
                if (PropiedadesExcluidas.Contains(p.Metadata.Name)) continue;
                var columna = p.Metadata.GetColumnName();

                switch (entry.State)
                {
                    case EntityState.Added when p.CurrentValue is not null:
                        cambios[columna] = new(null, p.CurrentValue);
                        break;

                    case EntityState.Modified when p.IsModified && !Equals(p.OriginalValue, p.CurrentValue):
                        cambios[columna] = new(p.OriginalValue, p.CurrentValue);
                        break;

                    case EntityState.Deleted:
                        cambios[columna] = new(p.OriginalValue, null);
                        break;
                }
            }

            return cambios;
        }

        private sealed record Cambio(object? Antes, object? Despues);
    }
}
```

Por qué funciona en la misma transacción: `ChangeTracker.Entries()` ya corre `DetectChanges`, y las filas de `Bitacora` agregadas aquí entran en el mismo `SaveChanges` que las causó. Si el guardado falla, no queda ni el cambio ni su bitácora (CA-006).

### 2.3 Helper de tests

Agrega a `Tests/RxBoxApiFactory.cs` (y `using System.Net.Http.Headers;`, `using System.Net.Http.Json;`, `using System.Text.Json;` arriba). Lo usan los tests de este módulo y de Personas:

```csharp
        /// <summary>HttpClient con el access token de un usuario de un solo box.</summary>
        public async Task<HttpClient> CrearClienteAsync(Usuario usuario)
        {
            var client = CreateClient();
            var login = await client.PostAsJsonAsync("/api/auth/login", new { email = usuario.Email, password = Password });
            login.EnsureSuccessStatusCode();

            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
```

### 2.4 Tests

`Tests/BitacoraTests.cs` (esta etapa; las siguientes agregan más):

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Infrastructure.Data;

namespace RxBox.Tests
{
    [Collection(ApiCollection.Nombre)]
    public class BitacoraTests(RxBoxApiFactory factory)
    {
        [Fact]
        public async Task Crear_y_editar_queda_en_bitacora_solo_con_lo_que_cambio()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persona = new Persona { Codigo = Guid.NewGuid().ToString("N"), Nombre = "Ana", Apellidos = "Prueba" };
            db.Personas.Add(persona);
            await db.SaveChangesAsync();
            persona.Nombre = "Ana María";
            await db.SaveChangesAsync();

            var registros = await db.Bitacora.AsNoTracking()
                .Where(b => b.EntidadPublicId == persona.PublicId)
                .OrderBy(b => b.Id)
                .ToListAsync();

            Assert.Equal([AccionBitacora.CREAR, AccionBitacora.EDITAR], registros.Select(r => r.Accion));
            Assert.All(registros, r =>
            {
                Assert.Equal(box.Id, r.BoxId);
                Assert.Equal(usuario.Id, r.UsuarioId);
                Assert.Equal(nameof(Persona), r.Entidad);
            });

            using var cambios = JsonDocument.Parse(registros[1].Cambios!);
            var nombre = cambios.RootElement.GetProperty("nombre");
            Assert.Equal("Ana", nombre.GetProperty("antes").GetString());
            Assert.Equal("Ana María", nombre.GetProperty("despues").GetString());
            Assert.Single(cambios.RootElement.EnumerateObject());
        }

        [Fact]
        public async Task Update_sin_cambios_reales_no_genera_registro()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);
            var creada = await factory.CrearPersonaAsync(box);
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persona = await db.Personas.SingleAsync(p => p.PublicId == creada.PublicId);
            db.Personas.Update(persona);
            await db.SaveChangesAsync();

            Assert.False(await db.Bitacora.AnyAsync(b =>
                b.EntidadPublicId == creada.PublicId && b.Accion == AccionBitacora.EDITAR));
        }

        [Fact]
        public async Task Borrar_registra_los_valores_que_tenia()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);
            var persona = await factory.CrearPersonaAsync(box);
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contacto = new ContactoEmergencia { PersonaId = persona.Id, Nombre = "Luis", Telefono = "6621234567" };
            db.ContactosEmergencia.Add(contacto);
            await db.SaveChangesAsync();
            db.ContactosEmergencia.Remove(contacto);
            await db.SaveChangesAsync();

            var borrado = await db.Bitacora.AsNoTracking()
                .SingleAsync(b => b.EntidadPublicId == contacto.PublicId && b.Accion == AccionBitacora.ELIMINAR);

            using var cambios = JsonDocument.Parse(borrado.Cambios!);
            Assert.Equal("Luis", cambios.RootElement.GetProperty("nombre").GetProperty("antes").GetString());
            Assert.Equal(JsonValueKind.Null, cambios.RootElement.GetProperty("nombre").GetProperty("despues").ValueKind);
        }

        [Fact]
        public async Task La_bitacora_nunca_guarda_password_hash()
        {
            var usuario = await factory.CrearUsuarioAsync(await factory.CrearBoxAsync());
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var alta = await db.Bitacora.AsNoTracking().SingleAsync(b =>
                b.Entidad == nameof(Usuario) && b.EntidadPublicId == usuario.PublicId && b.Accion == AccionBitacora.CREAR);

            Assert.Contains("email", alta.Cambios);
            Assert.DoesNotContain("password", alta.Cambios);
        }
    }
}
```

**Commit:** `feat(transversal): bitácora automática de cambios`

---

## Etapa 3 — Accesos (RF-TRV-002)

Cambios en `Infrastructure/Auth/AuthService.cs`. Agrega arriba `using System.Text.Json;`.

### 3.1 Helper para registrar

Al final de la clase, junto a los demás privados:

```csharp
        // Auth escribe la bitácora directo: login y cambio de box no son ediciones de una entidad.
        private void RegistrarAcceso(AccionBitacora accion, int? usuarioId, int? boxId, object? datos = null) =>
            db.Bitacora.Add(new Bitacora
            {
                BoxId = boxId,
                UsuarioId = usuarioId,
                Entidad = nameof(Usuario),
                Accion = accion,
                Cambios = datos is null ? null : JsonSerializer.Serialize(datos),
                Ip = currentUser.Ip
            });

        private async Task RegistrarLoginFallidoAsync(int? usuarioId, string email, CancellationToken ct)
        {
            RegistrarAcceso(AccionBitacora.LOGIN_FALLIDO, usuarioId, boxId: null, new { email });
            await db.SaveChangesAsync(ct);
        }
```

### 3.2 Login fallido

En `ValidarCredencialesAsync`, los dos `throw CredencialesInvalidas();` quedan así:

```csharp
            if (usuario is null)
            {
                hasher.VerifyHashedPassword(new Usuario(), HashFicticio, request.Password);
                await RegistrarLoginFallidoAsync(null, email, ct);
                throw CredencialesInvalidas();
            }

            var verificacion = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
            if (verificacion == PasswordVerificationResult.Failed)
            {
                await RegistrarLoginFallidoAsync(usuario.Id, email, ct);
                throw CredencialesInvalidas();
            }
```

### 3.3 Login, selección y cambio de box

`EmitirTokensAsync` recibe qué acceso registrar (null en refresh):

```csharp
        private async Task<TokensResponse> EmitirTokensAsync(
            int usuarioId, BoxDeUsuario box, AccionBitacora? acceso, CancellationToken ct)
        {
            var (accessToken, accessExpiraEn) = tokens.CrearAccessToken(usuarioId, box.BoxId, box.Rol);
            var (refreshToken, hash, refreshExpiraEn) = tokens.CrearRefreshToken();

            db.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = usuarioId,
                BoxId = box.BoxId,
                TokenHash = hash,
                ExpiraEn = refreshExpiraEn
            });
            if (acceso is not null) RegistrarAcceso(acceso.Value, usuarioId, box.BoxId);

            await db.SaveChangesAsync(ct);   // token y bitácora juntos

            return new TokensResponse(accessToken, refreshToken, accessExpiraEn);
        }

        private async Task<TokensResponse> EmitirParaBoxAsync(
            int usuarioId, Guid boxPublicId, AccionBitacora acceso, CancellationToken ct)
        {
            var box = await BoxesActivos(usuarioId).Where(ub => ub.Box.PublicId == boxPublicId).Select(ADeUsuario).FirstOrDefaultAsync(ct)
                ?? throw new UnauthorizedAccessException("No tienes acceso a ese box.");

            return await EmitirTokensAsync(usuarioId, box, acceso, ct);
        }
```

Y ajusta las cuatro llamadas:

```csharp
// LoginAsync, caso de un solo box
var emitidos = await EmitirTokensAsync(usuario.Id, boxes[0], AccionBitacora.LOGIN, ct);

// SeleccionarBoxAsync: con dos boxes el LOGIN se registra hasta aquí (CA-002)
return await EmitirParaBoxAsync(usuarioId, request.BoxPublicId, AccionBitacora.LOGIN, ct);

// CambiarBoxAsync
public Task<TokensResponse> CambiarBoxAsync(CambiarBoxRequest request, CancellationToken ct = default) =>
    EmitirParaBoxAsync(currentUser.UsuarioId!.Value, request.BoxPublicId, AccionBitacora.CAMBIO_BOX, ct);

// RefreshAsync: no es un acceso nuevo (RN-001)
return await EmitirTokensAsync(token.UsuarioId, box, acceso: null, ct);
```

### 3.4 Tests

Agrega a `BitacoraTests` (y `using System.Net.Http.Json;` arriba):

```csharp
        [Fact]
        public async Task Login_exitoso_y_fallido_quedan_registrados()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);
            using var client = factory.CreateClient();

            (await client.PostAsJsonAsync("/api/auth/login",
                new { email = usuario.Email, password = RxBoxApiFactory.Password })).EnsureSuccessStatusCode();
            await client.PostAsJsonAsync("/api/auth/login", new { email = usuario.Email, password = "incorrecto" });

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accesos = await db.Bitacora.AsNoTracking()
                .Where(b => b.UsuarioId == usuario.Id && b.Entidad == nameof(Usuario) && b.EntidadPublicId == null)
                .OrderBy(b => b.Id)
                .ToListAsync();

            Assert.Equal([AccionBitacora.LOGIN, AccionBitacora.LOGIN_FALLIDO], accesos.Select(a => a.Accion));
            Assert.Equal(box.Id, accesos[0].BoxId);
            Assert.Null(accesos[1].BoxId);
            Assert.Contains(usuario.Email, accesos[1].Cambios);
        }

        [Fact]
        public async Task Login_con_email_inexistente_registra_fallido_sin_usuario()
        {
            var email = $"{Guid.NewGuid():N}@nadie.test";
            using var client = factory.CreateClient();
            await client.PostAsJsonAsync("/api/auth/login", new { email, password = "x" });

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fallido = await db.Bitacora.AsNoTracking()
                .SingleAsync(b => b.Accion == AccionBitacora.LOGIN_FALLIDO && b.Cambios!.Contains(email));

            Assert.Null(fallido.UsuarioId);
        }
```

> `Cambios!.Contains(email)` sobre jsonb: Npgsql lo traduce comparando el texto. Sirve para el test; en la consulta real (etapa 4) no se busca dentro de `cambios`.

Corre también `AuthTests`: no deben cambiar.

**Commit:** `feat(auth): registrar accesos en bitácora`

---

## Etapa 4 — Consulta (RF-TRV-003)

### 4.1 Paginación común

Se reusa en Personas y en todo listado paginado.

`Core/DTOs/Requests/Comun/PaginacionRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace RxBox.Core.DTOs.Requests.Comun
{
    /// <summary>Base de los filtros paginados. Se recibe con [FromQuery].</summary>
    public abstract class PaginacionRequest
    {
        [Range(1, int.MaxValue)]
        public int Pagina { get; set; } = 1;

        [Range(1, 100)]
        public int Tamano { get; set; } = 20;

        public int Saltar => (Pagina - 1) * Tamano;   // solo lectura: el model binding lo ignora
    }
}
```

`Core/DTOs/Responses/Comun/PaginaResponse.cs`

```csharp
namespace RxBox.Core.DTOs.Responses.Comun
{
    public record PaginaResponse<T>(IReadOnlyList<T> Items, int Total, int Pagina, int Tamano)
    {
        public int TotalPaginas => Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamano);
    }
}
```

`Infrastructure/Services/Comun/PaginacionExtensions.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Requests.Comun;
using RxBox.Core.DTOs.Responses.Comun;

namespace RxBox.Infrastructure.Services.Comun
{
    public static class PaginacionExtensions
    {
        /// <summary>Cuenta y pagina. El query ya debe venir ordenado.</summary>
        public static async Task<PaginaResponse<T>> ToPaginaAsync<T>(
            this IQueryable<T> query, PaginacionRequest paginacion, CancellationToken ct = default)
        {
            var total = await query.CountAsync(ct);
            var items = await query.Skip(paginacion.Saltar).Take(paginacion.Tamano).ToListAsync(ct);
            return new PaginaResponse<T>(items, total, paginacion.Pagina, paginacion.Tamano);
        }
    }
}
```

### 4.2 DTOs de bitácora

`Core/DTOs/Requests/Auditoria/BitacoraFiltroRequest.cs`

```csharp
using RxBox.Core.DTOs.Requests.Comun;
using RxBox.Core.Enums;

namespace RxBox.Core.DTOs.Requests.Auditoria
{
    public class BitacoraFiltroRequest : PaginacionRequest
    {
        public string? Entidad { get; set; }
        public Guid? EntidadPublicId { get; set; }
        public AccionBitacora? Accion { get; set; }

        /// <summary>Fechas en la zona horaria del box; "hasta" incluye el día completo.</summary>
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
    }
}
```

`Core/DTOs/Responses/Auditoria/BitacoraResponse.cs`

```csharp
using System.Text.Json;
using RxBox.Core.Enums;

namespace RxBox.Core.DTOs.Responses.Auditoria
{
    /// <summary><see cref="Cambios"/> sale como objeto JSON, no como texto.</summary>
    public record BitacoraResponse(
        DateTimeOffset Fecha,
        AccionBitacora Accion,
        string Entidad,
        Guid? EntidadPublicId,
        string? UsuarioEmail,
        JsonElement? Cambios,
        string? Ip);
}
```

### 4.3 Servicio

`Core/Interfaces/IBitacoraService.cs`

```csharp
using RxBox.Core.DTOs.Requests.Auditoria;
using RxBox.Core.DTOs.Responses.Auditoria;
using RxBox.Core.DTOs.Responses.Comun;

namespace RxBox.Core.Interfaces
{
    public interface IBitacoraService
    {
        Task<PaginaResponse<BitacoraResponse>> ListarAsync(BitacoraFiltroRequest filtro, CancellationToken ct = default);
    }
}
```

`Infrastructure/Services/Auditoria/BitacoraService.cs`

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Requests.Auditoria;
using RxBox.Core.DTOs.Responses.Auditoria;
using RxBox.Core.DTOs.Responses.Comun;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Infrastructure.Data;
using RxBox.Infrastructure.Services.Comun;

namespace RxBox.Infrastructure.Services.Auditoria
{
    /// <summary>Bitacora no tiene query filter: el box se filtra aquí a mano.</summary>
    public class BitacoraService(AppDbContext db, ICurrentUserService currentUser) : IBitacoraService
    {
        public async Task<PaginaResponse<BitacoraResponse>> ListarAsync(BitacoraFiltroRequest f, CancellationToken ct = default)
        {
            var boxId = currentUser.BoxId ?? throw new UnauthenticatedException("La sesión no tiene box.");

            var query = db.Bitacora.AsNoTracking().Where(b => b.BoxId == boxId);

            if (!string.IsNullOrWhiteSpace(f.Entidad)) query = query.Where(b => b.Entidad == f.Entidad);
            if (f.EntidadPublicId is not null) query = query.Where(b => b.EntidadPublicId == f.EntidadPublicId);
            if (f.Accion is not null) query = query.Where(b => b.Accion == f.Accion.Value);

            if (f.Desde is not null || f.Hasta is not null)
            {
                var zona = TimeZoneInfo.FindSystemTimeZoneById(
                    await db.Boxes.Where(b => b.Id == boxId).Select(b => b.ZonaHoraria).SingleAsync(ct));

                if (f.Desde is not null)
                {
                    var desde = InicioDelDiaUtc(f.Desde.Value, zona);
                    query = query.Where(b => b.Fecha >= desde);
                }
                if (f.Hasta is not null)
                {
                    var hastaExclusivo = InicioDelDiaUtc(f.Hasta.Value.AddDays(1), zona);
                    query = query.Where(b => b.Fecha < hastaExclusivo);
                }
            }

            // usuarios es global: sin query filter, el join no pierde filas.
            var filas =
                from b in query
                join u in db.Usuarios on b.UsuarioId equals (int?)u.Id into us
                from u in us.DefaultIfEmpty()
                orderby b.Fecha descending, b.Id descending
                select new Fila(b.Fecha, b.Accion, b.Entidad, b.EntidadPublicId, u != null ? u.Email : null, b.Cambios, b.Ip);

            var pagina = await filas.ToPaginaAsync(f, ct);

            return new PaginaResponse<BitacoraResponse>(
                [.. pagina.Items.Select(ToResponse)], pagina.Total, pagina.Pagina, pagina.Tamano);
        }

        private static DateTimeOffset InicioDelDiaUtc(DateOnly dia, TimeZoneInfo zona) =>
            new(TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue), zona));

        private static BitacoraResponse ToResponse(Fila f) =>
            new(f.Fecha, f.Accion, f.Entidad, f.EntidadPublicId, f.UsuarioEmail, ComoJson(f.Cambios), f.Ip);

        private static JsonElement? ComoJson(string? json)
        {
            if (json is null) return null;
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        private sealed record Fila(
            DateTimeOffset Fecha, AccionBitacora Accion, string Entidad, Guid? EntidadPublicId,
            string? UsuarioEmail, string? Cambios, string? Ip);
    }
}
```

### 4.4 Registro y controller

En `AddAppServices` (y `using RxBox.Infrastructure.Services.Auditoria;`):

```csharp
            services.AddScoped<IBitacoraService, BitacoraService>();
```

En `CatalogoFijoService`, agrega la línea para que el front pueda llenar el filtro de acción:

```csharp
            ["acciones-bitacora"] = typeof(AccionBitacora),
```

`API/Controllers/BitacoraController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.DTOs.Requests.Auditoria;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/bitacora")]
    [Authorize(Roles = nameof(RolUsuario.ADMIN))]
    public class BitacoraController(IBitacoraService service) : ControllerBase
    {
        /// <summary>GET /api/bitacora?entidad=Persona&amp;entidadPublicId=…&amp;desde=2026-11-01&amp;hasta=2026-11-06&amp;pagina=1&amp;tamano=20</summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] BitacoraFiltroRequest filtro, CancellationToken ct) =>
            Ok(await service.ListarAsync(filtro, ct));
    }
}
```

### 4.5 Tests

Agrega a `BitacoraTests`:

```csharp
        [Fact]
        public async Task La_consulta_solo_regresa_registros_del_box_de_la_sesion()
        {
            var boxA = await factory.CrearBoxAsync();
            var boxB = await factory.CrearBoxAsync();
            var adminA = await factory.CrearUsuarioAsync(boxA);
            var personaA = await factory.CrearPersonaAsync(boxA);
            var personaB = await factory.CrearPersonaAsync(boxB);
            using var client = await factory.CrearClienteAsync(adminA);

            var pagina = await client.GetFromJsonAsync<JsonElement>("/api/bitacora?entidad=Persona&tamano=100");
            var ids = pagina.GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("entidadPublicId").GetString())
                .ToList();

            Assert.Contains(personaA.PublicId.ToString(), ids);
            Assert.DoesNotContain(personaB.PublicId.ToString(), ids);
        }

        [Fact]
        public async Task Historial_de_un_registro_va_del_mas_reciente_al_mas_antiguo_con_cambios_como_objeto()
        {
            var box = await factory.CrearBoxAsync();
            var admin = await factory.CrearUsuarioAsync(box);
            using (var scope = factory.ScopeDeUsuario(admin.Id, box))
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persona = new Persona { Codigo = Guid.NewGuid().ToString("N"), Nombre = "Ana", Apellidos = "Prueba" };
                db.Personas.Add(persona);
                await db.SaveChangesAsync();
                persona.Apellidos = "López";
                await db.SaveChangesAsync();

                using var client = await factory.CrearClienteAsync(admin);
                var pagina = await client.GetFromJsonAsync<JsonElement>($"/api/bitacora?entidadPublicId={persona.PublicId}");
                var items = pagina.GetProperty("items").EnumerateArray().ToList();

                Assert.Equal(2, pagina.GetProperty("total").GetInt32());
                Assert.Equal("EDITAR", items[0].GetProperty("accion").GetString());
                Assert.Equal("López", items[0].GetProperty("cambios").GetProperty("apellidos").GetProperty("despues").GetString());
                Assert.Equal(admin.Email, items[0].GetProperty("usuarioEmail").GetString());
            }
        }

        [Fact]
        public async Task Tamano_mayor_a_100_responde_400()
        {
            using var client = await factory.CrearClienteAsync(await factory.CrearUsuarioAsync(await factory.CrearBoxAsync()));

            var response = await client.GetAsync("/api/bitacora?tamano=101");

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
```

El test de "accion": "EDITAR" también confirma el paso 0: sin el convertidor saldría `1`.

### 4.6 Prueba manual

Postman: `Bitácora / Últimos movimientos` y `Bitácora / Historial de un registro` (usa `catalogoPublicId`: edita un catálogo antes y verás su historial).

**Commit:** `feat(transversal): consulta de bitácora`

---

## Checklist del módulo

- [ ] Paso 0: enums como texto en JSON (pendiente de 01)
- [ ] Etapa 1: tablas `bitacora`, `membresia_eventos` y `membresias.membresia_anterior_id`; la migración no toca nada más
- [ ] Etapa 2: el interceptor escribe CREAR / EDITAR / ELIMINAR solo con lo que cambió y sin hashes
- [ ] Etapa 3: LOGIN, LOGIN_FALLIDO y CAMBIO_BOX desde Auth; `AuthTests` siguen en verde
- [ ] Etapa 4: consulta paginada solo del box, `cambios` como objeto, solo Admin
- [ ] Suite completa en verde
