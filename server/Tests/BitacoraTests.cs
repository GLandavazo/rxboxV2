using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;

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


    }
}
