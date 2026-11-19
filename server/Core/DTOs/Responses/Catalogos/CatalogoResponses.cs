using RxBox.Core.Enums;

namespace RxBox.Core.DTOs.Responses.Catalogos
{
    public record CatalogoResponse(Guid PublicId, TipoCatalogo Tipo, string Nombre, short Orden, bool Activo);

    public record CatalogoFijoResponse(string Valor, string Descripcion);
}
