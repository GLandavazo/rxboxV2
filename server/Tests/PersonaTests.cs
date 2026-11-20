using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.Entities;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

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
