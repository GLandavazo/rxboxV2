using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace RxBox.Tests
{
    [CollectionDefinition(Nombre)]
    public class ApiCollection : ICollectionFixture<RxBoxApiFactory>
    {
        public const string Nombre = "Api";
    }

    /// <summary>La API real contra un Postgres desechable, compartida por toda la colección.</summary>
    public class RxBoxApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string Password = "Password-De-Prueba-1";
        public const string SeedEmail = "admin@alpha.test";
        public const string SeedPassword = "Password-Del-Seed-1";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            // Fuerza el arranque de la API, que aplica las migraciones.
            _ = Services;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = "secreto-de-pruebas-con-mas-de-32-bytes-de-largo",
                ["Seed:AdminEmail"] = SeedEmail,
                ["Seed:AdminPassword"] = SeedPassword
            }));
        }

        public async Task<Box> CrearBoxAsync(bool activo = true)
        {
            var slug = $"box-{Guid.NewGuid():N}";
            var box = new Box { Nombre = slug, Slug = slug, VentanaCheckinMinutos = 30, VentanaReservaDias = 7, Activo = activo };

            await EnDbAsync(db => db.Boxes.Add(box));
            return box;
        }

        /// <summary>Crea un usuario con membresía en cada box indicado.</summary>
        public async Task<Usuario> CrearUsuarioAsync(params Box[] boxes)
        {
            var usuario = new Usuario { Email = $"{Guid.NewGuid():N}@rxbox.test" };
            usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, Password);

            await EnDbAsync(db => db.Usuarios.Add(usuario));
            foreach (var box in boxes)
            {
                var persona = await CrearPersonaAsync(box);
                await EnDbAsync(db => db.UsuarioBoxes.Add(new UsuarioBox
                {
                    UsuarioId = usuario.Id,
                    BoxId = box.Id,
                    PersonaId = persona.Id,
                    Rol = RolUsuario.ADMIN
                }));
            }
            return usuario;
        }

        public async Task<Persona> CrearPersonaAsync(Box box)
        {
            var persona = new Persona { BoxId = box.Id, Codigo = Guid.NewGuid().ToString("N"), Nombre = "Ana", Apellidos = "Prueba" };

            await EnDbAsync(db => db.Personas.Add(persona));
            return persona;
        }

        /// <summary>
        /// Scope cuyo usuario actual es el indicado: pone en el HttpContext los mismos
        /// claims del JWT. Sin box, el scope queda sin box actual.
        /// </summary>
        public IServiceScope ScopeDeUsuario(int usuarioId, Box? box)
        {
            var claims = new List<Claim> { new("sub", usuarioId.ToString()) };
            if (box is not null) claims.Add(new Claim("box_id", box.Id.ToString()));

            var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            };
            return scope;
        }

        public async Task EnDbAsync(Action<AppDbContext> accion)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            accion(db);
            await db.SaveChangesAsync();
        }
    }
}
