using RxBox.Core.DTOs.Requests.Auth;
using RxBox.Core.DTOs.Responses.Auth;

namespace RxBox.Core.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
        Task<TokensResponse> SeleccionarBoxAsync(SeleccionarBoxRequest request, CancellationToken ct = default);
        Task<TokensResponse> CambiarBoxAsync(CambiarBoxRequest request, CancellationToken ct = default);
        Task<TokensResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
        Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);
    }
}
