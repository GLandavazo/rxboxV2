using RxBox.Core.DTOs.Requests.Catalogos;
using RxBox.Core.DTOs.Responses.Catalogos;
using RxBox.Core.Enums;

namespace RxBox.Core.Interfaces
{
    public interface ICatalogoService
    {
        Task<IReadOnlyList<CatalogoResponse>> ListarAsync(TipoCatalogo? tipo, bool soloActivos, CancellationToken ct = default);
        Task<CatalogoResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default);

        Task<CatalogoResponse> CrearAsync(CatalogoCreateRequest request, CancellationToken ct = default);
        Task<CatalogoResponse> EditarAsync(Guid publicId, CatalogoUpdateRequest request, CancellationToken ct = default);
        Task<CatalogoResponse> CambiarEstadoAsync(Guid publicId, CancellationToken ct = default);


    }
}
