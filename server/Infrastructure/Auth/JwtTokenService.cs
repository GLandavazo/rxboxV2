using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RxBox.Core.Enums;
using RxBox.Core.Options;

namespace RxBox.Infrastructure.Auth
{
    /// <summary>Emite y valida los JWT propios (HS256) y genera los refresh tokens.</summary>
    public class JwtTokenService(IOptions<JwtOptions> options)
    {
        public const string ClaimBoxId = "box_id";
        public const string ClaimRol = "rol";

        private readonly JwtOptions _options = options.Value;
        private readonly JsonWebTokenHandler _handler = new();

        public (string Token, DateTimeOffset ExpiraEn) CrearAccessToken(int usuarioId, int boxId, RolUsuario rol)
        {
            var expiraEn = DateTimeOffset.UtcNow.AddMinutes(_options.AccessMinutos);
            var token = Crear(_options.Audience, expiraEn, new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuarioId.ToString(),
                [ClaimBoxId] = boxId.ToString(),
                [ClaimRol] = rol.ToString()
            });
            return (token, expiraEn);
        }

        public string CrearSelectionToken(int usuarioId) =>
            Crear(_options.SelectionAudience, DateTimeOffset.UtcNow.AddMinutes(_options.SelectionMinutos),
                new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = usuarioId.ToString() });

        /// <summary>Regresa el usuario del selection token, o null si no es válido.</summary>
        public async Task<int?> ValidarSelectionTokenAsync(string token)
        {
            var result = await _handler.ValidateTokenAsync(token, ParametrosValidacion(_options.SelectionAudience));
            return result.IsValid
                && result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub)
                && int.TryParse(sub?.ToString(), out var usuarioId)
                    ? usuarioId
                    : null;
        }

        public TokenValidationParameters ParametrosValidacion(string audience) => new()
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = LlaveDeFirma(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(60)
        };

        public (string Token, string Hash, DateTimeOffset ExpiraEn) CrearRefreshToken()
        {
            var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
            return (token, HashRefreshToken(token), DateTimeOffset.UtcNow.AddDays(_options.RefreshDias));
        }

        public static string HashRefreshToken(string token) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private string Crear(string audience, DateTimeOffset expiraEn, Dictionary<string, object> claims) =>
            _handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = audience,
                Expires = expiraEn.UtcDateTime,
                Claims = claims,
                SigningCredentials = new SigningCredentials(LlaveDeFirma(), SecurityAlgorithms.HmacSha256)
            });

        private SymmetricSecurityKey LlaveDeFirma() => new(Encoding.UTF8.GetBytes(_options.Secret));
    }
}
