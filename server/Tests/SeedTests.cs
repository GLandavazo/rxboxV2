using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;
using RxBox.Infrastructure.Data;
using RxBox.Infrastructure.Data.Seed;

namespace RxBox.Tests
{
    [Collection(ApiCollection.Nombre)]
    public class SeedTests(RxBoxApiFactory factory)
    {
        [Fact]
        public async Task La_siembra_de_catalogos_es_por_box_e_idempotente()
        {
            var box = await factory.CrearBoxAsync();

            using var scope = factory.Services.CreateScope();
            var catalogos = scope.ServiceProvider.GetRequiredService<ICatalogoSeedService>();
            await catalogos.SembrarAsync(box.Id);
            await catalogos.SembrarAsync(box.Id);

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var nombres = await db.Catalogos.IgnoreQueryFilters()
                .Where(c => c.BoxId == box.Id)
                .OrderBy(c => c.Tipo).ThenBy(c => c.Orden)
                .Select(c => c.Nombre)
                .ToListAsync();

            Assert.Equal(["General", "Estudiante", "Efectivo", "Transferencia", "Tarjeta"], nombres);
        }

        [Fact]
        public async Task El_seed_de_development_crea_alpha_con_su_admin_y_es_idempotente()
        {
            using (var scope = factory.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync();
            }

            using (var scope = factory.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync();

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var box = await db.Boxes.SingleAsync(b => b.Slug == "alpha");
                var usuario = await db.Usuarios.SingleAsync(u => u.Email == RxBoxApiFactory.SeedEmail);
                var usuarioBox = await db.UsuarioBoxes.SingleAsync(ub => ub.UsuarioId == usuario.Id && ub.BoxId == box.Id);

                Assert.Equal(RolUsuario.ADMIN, usuarioBox.Rol);
                Assert.Equal(1, await db.Personas.IgnoreQueryFilters().CountAsync(p => p.BoxId == box.Id));
                Assert.Equal(5, await db.Catalogos.IgnoreQueryFilters().CountAsync(c => c.BoxId == box.Id));
            }

            using var client = factory.CreateClient();
            var login = await client.PostAsJsonAsync("/api/auth/login",
                new { email = RxBoxApiFactory.SeedEmail, password = RxBoxApiFactory.SeedPassword });
            login.EnsureSuccessStatusCode();
        }
    }
}
