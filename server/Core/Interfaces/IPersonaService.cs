using RxBox.Core.DTOs.Requests.Personas;
using RxBox.Core.DTOs.Responses.Comun;
using RxBox.Core.DTOs.Responses.Personas.RxBox.Core.DTOs.Responses.Personas;

namespace RxBox.Core.Interfaces
{
    public interface IPersonaService
    {
        Task<PaginaResponse<PersonaListItemResponse>> ListarAsync(PersonaFiltroRequest filtro, CancellationToken ct = default);
        Task<PersonaResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default);
        Task<PersonaResponse> CrearAsync(PersonaCreateRequest request, CancellationToken ct = default);
        Task<PersonaResponse> EditarAsync(Guid publicId, PersonaUpdateRequest request, CancellationToken ct = default);
        Task<PersonaResponse> CambiarEstadoAsync(Guid publicId, CancellationToken ct = default);
        Task<ContactoEmergenciaResponse> AgregarContactoAsync(Guid personaPublicId, ContactoEmergenciaRequest request, CancellationToken ct = default);
        Task<ContactoEmergenciaResponse> EditarContactoAsync(Guid personaPublicId, Guid contactoPublicId, ContactoEmergenciaRequest request, CancellationToken ct = default);
        Task EliminarContactoAsync(Guid personaPublicId, Guid contactoPublicId, CancellationToken ct = default);




    }
}
