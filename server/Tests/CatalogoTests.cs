using Microsoft.Extensions.DependencyInjection;
using RxBox.Core.Entities;
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

        // Solo crea datos. El scope con el usuario se crea en cada test, no aquí.
        private async Task<(Box Box, Usuario Usuario)> CrearBoxConUsuarioAsync()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);
            return (box, usuario);
        }

        private static ICatalogoService Servicio(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<ICatalogoService>();

        [Fact]
        public async Task Crear_sin_orden_asigna_el_siguiente()
        {
            var (box, usuario) = await CrearBoxConUsuarioAsync();
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
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
            var (box, usuario) = await CrearBoxConUsuarioAsync();
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
            var service = Servicio(scope);
            await service.CrearAsync(new(Metodo, "Efectivo", null));

            await Assert.ThrowsAsync<ConflictException>(() => service.CrearAsync(new(Metodo, " efectivo ", null)));
        }

        [Fact]
        public async Task Crear_duplicado_de_un_inactivo_pide_reactivarlo()
        {
            var (box, usuario) = await CrearBoxConUsuarioAsync();
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
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
            var (box, usuario) = await CrearBoxConUsuarioAsync();
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
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
            var (box, usuario) = await CrearBoxConUsuarioAsync();
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
            var service = Servicio(scope);
            var unico = await service.CrearAsync(new(Metodo, "Efectivo", null));

            await Assert.ThrowsAsync<ConflictException>(() => service.CambiarEstadoAsync(unico.PublicId));
        }

        [Fact]
        public async Task Listar_solo_activos_omite_los_desactivados()
        {
            var (box, usuario) = await CrearBoxConUsuarioAsync();
            using var scope = factory.ScopeDeUsuario(usuario.Id, box);
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
            var (boxA, usuarioA) = await CrearBoxConUsuarioAsync();
            var (boxB, usuarioB) = await CrearBoxConUsuarioAsync();

            Guid idDelBoxA;
            using (var scopeA = factory.ScopeDeUsuario(usuarioA.Id, boxA))
                idDelBoxA = (await Servicio(scopeA).CrearAsync(new(Metodo, "Efectivo", null))).PublicId;

            using var scopeB = factory.ScopeDeUsuario(usuarioB.Id, boxB);
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