using RxBox.Core.DTOs.Responses.Catalogos;

namespace RxBox.Core.Interfaces
{
    public interface ICatalogoFijoService
    {
        IReadOnlyList<string> Nombres();
        IReadOnlyList<CatalogoFijoResponse> Obtener(string nombre);
    }
}
