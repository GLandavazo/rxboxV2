using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.Entities;
using RxBox.Core.Interfaces.Repositories;
using RxBox.Infrastructure.Data;

namespace RxBox.Tests
{
    [Collection(ApiCollection.Nombre)]
    public class TenantTests(RxBoxApiFactory factory)
    {
        [Fact]
        public async Task Un_usuario_del_box_A_no_ve_personas_del_box_B()
        {
            var boxA = await factory.CrearBoxAsync();
            var boxB = await factory.CrearBoxAsync();
            var usuarioA = await factory.CrearUsuarioAsync(boxA);
            var personaA = await factory.CrearPersonaAsync(boxA);
            var personaB = await factory.CrearPersonaAsync(boxB);

            using var scope = factory.ScopeDeUsuario(usuarioA.Id, boxA);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var visibles = await db.Personas.Select(p => p.Id).ToListAsync();

            Assert.Contains(personaA.Id, visibles);
            Assert.DoesNotContain(personaB.Id, visibles);
            Assert.NotNull(await uow.Repository<Persona>().GetByPublicIdAsync(personaA.PublicId));
            Assert.Null(await uow.Repository<Persona>().GetByPublicIdAsync(personaB.PublicId));
        }

        [Fact]
        public async Task Sin_box_actual_no_se_ve_ninguna_persona()
        {
            await factory.CrearPersonaAsync(await factory.CrearBoxAsync());

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.Empty(await db.Personas.ToListAsync());
        }

        [Fact]
        public async Task El_interceptor_asigna_box_y_auditoria_al_insertar()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);
            var persona = new Persona { Codigo = Guid.NewGuid().ToString("N"), Nombre = "Ana", Apellidos = "Prueba" };

            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Personas.Add(persona);
            await db.SaveChangesAsync();

            Assert.Equal(box.Id, persona.BoxId);
            Assert.Equal(usuario.Id, persona.CreadoPor);

            persona.Nombre = "Ana María";
            await db.SaveChangesAsync();

            Assert.Equal(usuario.Id, persona.EditadoPor);
            Assert.NotNull(persona.FechaEdicion);
        }

        [Fact]
        public async Task Insertar_entidad_de_box_sin_box_actual_falla()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Personas.Add(new Persona { Codigo = Guid.NewGuid().ToString("N"), Nombre = "Ana", Apellidos = "Prueba" });

            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
    }
}
