
namespace RxBox.Core.DTOs.Responses.Personas
{
    namespace RxBox.Core.DTOs.Responses.Personas
    {
        public record ContactoEmergenciaResponse(Guid PublicId, string Nombre, string? Parentesco, string Telefono);

        public record PersonaListItemResponse(
            Guid PublicId, string Codigo, string Nombre, string Apellidos,
            string? Email, string? Telefono, bool EsAtleta, bool EsCoach, bool Activo);

        public record PersonaResponse(
            Guid PublicId, string Codigo, string Nombre, string Apellidos,
            string? Email, string? Telefono, bool EsAtleta, bool EsCoach, bool Activo,
            DateTimeOffset FechaCreacion,
            IReadOnlyList<ContactoEmergenciaResponse> Contactos);
    }

}
