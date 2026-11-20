using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
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

    }
}
