using RxBox.Core.DTOs.Responses.Catalogos;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using System.ComponentModel;
using System.Reflection;

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
            ["tipos-catalogo"] = typeof(TipoCatalogo),
            ["acciones-bitacora"] = typeof(AccionBitacora)
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
