using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.DTOs.Responses.Comun;
using RxBox.Core.DTOs.Responses.Personas.RxBox.Core.DTOs.Responses.Personas;

namespace RxBox.Core.Interfaces
{
    public interface IPersonaService
    {
        Task<PaginaResponse<PersonaListItemResponse>> ListarAsync(PersonaFiltroRequest filtro, CancellationToken ct = default);
        Task<PersonaResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default);
    }
}
