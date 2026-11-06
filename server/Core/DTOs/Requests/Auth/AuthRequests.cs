using System.ComponentModel.DataAnnotations;

namespace RxBox.Core.DTOs.Requests.Auth
{
    public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

    public record SeleccionarBoxRequest([Required] string SelectionToken, Guid BoxPublicId);

    public record CambiarBoxRequest(Guid BoxPublicId);

    public record RefreshRequest([Required] string RefreshToken);

    public record LogoutRequest([Required] string RefreshToken);
}
