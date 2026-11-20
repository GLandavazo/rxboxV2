using Microsoft.EntityFrameworkCore;
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




    }
}
