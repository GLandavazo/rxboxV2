using RxBox.Core.DTOs.Requests.Comun;
using RxBox.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RxBox.Core.DTOs.Requests.Personas
{
    internal static class ReglasPersona
    {
        public const string Telefono = @"^\d{10}$";
        public const string TelefonoMensaje = "El teléfono debe tener 10 dígitos, sin espacios ni guiones.";

        public static IEnumerable<ValidationResult> ValidarRol(bool esAtleta, bool esCoach)
        {
            if (!esAtleta && !esCoach)
                yield return new ValidationResult(
                    "La persona debe ser atleta, coach o ambos.", ["EsAtleta", "EsCoach"]);
        }
    }

    public record ContactoEmergenciaRequest(
        [Required, StringLength(120)] string Nombre,
        [StringLength(40)] string? Parentesco,
        [Required, RegularExpression(ReglasPersona.Telefono, ErrorMessage = ReglasPersona.TelefonoMensaje)] string Telefono);

    public record PersonaCreateRequest(
        [Required, StringLength(80)] string Nombre,
        [Required, StringLength(120)] string Apellidos,
        [EmailAddress, StringLength(150)] string? Email,
        [RegularExpression(ReglasPersona.Telefono, ErrorMessage = ReglasPersona.TelefonoMensaje)] string? Telefono,
        bool EsAtleta = true,
        bool EsCoach = false,
        [MaxLength(3, ErrorMessage = "Máximo 3 contactos de emergencia.")] IReadOnlyList<ContactoEmergenciaRequest>? Contactos = null)
        : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            ReglasPersona.ValidarRol(EsAtleta, EsCoach);
    }
    public class PersonaFiltroRequest : PaginacionRequest
    {
        /// <summary>Texto en nombre completo, código, email o teléfono.</summary>
        [StringLength(100)]
        public string? Buscar { get; set; }

        public TipoPersona? Tipo { get; set; }
        public bool SoloActivos { get; set; } = true;
    }

    public record PersonaUpdateRequest(
    [Required, StringLength(80)] string Nombre,
    [Required, StringLength(120)] string Apellidos,
    [EmailAddress, StringLength(150)] string? Email,
    [RegularExpression(ReglasPersona.Telefono, ErrorMessage = ReglasPersona.TelefonoMensaje)] string? Telefono,
    bool EsAtleta,
    bool EsCoach)
    : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            ReglasPersona.ValidarRol(EsAtleta, EsCoach);
    }

}
