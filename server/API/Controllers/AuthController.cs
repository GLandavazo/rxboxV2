using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxBox.Core.DTOs.Requests.Auth;
using RxBox.Core.Interfaces;

namespace RxBox.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Authorize]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) =>
            Ok(await authService.LoginAsync(request, ct));

        [HttpPost("seleccionar-box")]
        [AllowAnonymous]
        public async Task<IActionResult> SeleccionarBox([FromBody] SeleccionarBoxRequest request, CancellationToken ct) =>
            Ok(await authService.SeleccionarBoxAsync(request, ct));

        [HttpPost("cambiar-box")]
        public async Task<IActionResult> CambiarBox([FromBody] CambiarBoxRequest request, CancellationToken ct) =>
            Ok(await authService.CambiarBoxAsync(request, ct));

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct) =>
            Ok(await authService.RefreshAsync(request, ct));

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
        {
            await authService.LogoutAsync(request, ct);
            return NoContent();
        }
    }
}
