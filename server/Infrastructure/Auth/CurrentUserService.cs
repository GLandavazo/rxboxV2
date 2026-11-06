using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RxBox.Core.Interfaces;

namespace RxBox.Infrastructure.Auth
{
    /// <summary>
    /// Lee los claims del usuario autenticado desde el HttpContext. Sin request o sin
    /// token todo regresa null, y el query filter de tenant no devuelve filas.
    /// </summary>
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        public int? UsuarioId =>
            int.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

        public int? BoxId =>
            int.TryParse(User?.FindFirstValue(JwtTokenService.ClaimBoxId), out var id) ? id : null;

        public string? Rol => User?.FindFirstValue(JwtTokenService.ClaimRol);

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    }
}
