using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Requests.Auth;
using RxBox.Core.DTOs.Responses.Auth;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Infrastructure.Data;

namespace RxBox.Infrastructure.Auth
{
    public class AuthService(
        AppDbContext db,
        IPasswordHasher<Usuario> hasher,
        JwtTokenService tokens,
        ICurrentUserService currentUser) : IAuthService
    {
        // Se verifica contra este hash cuando el email no existe, para que el tiempo
        // de respuesta no revele qué emails están registrados.
        private static readonly string HashFicticio =
            new PasswordHasher<Usuario>().HashPassword(new Usuario(), Guid.NewGuid().ToString());

        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var usuario = await ValidarCredencialesAsync(request, ct);

            var boxes = await BoxesActivos(usuario.Id).Select(ADeUsuario).ToListAsync(ct);
            if (boxes.Count == 0)
                throw new UnauthorizedAccessException("El usuario no tiene ningún box activo.");

            if (boxes.Count > 1)
                return new LoginResponse
                {
                    RequiereSeleccion = true,
                    Boxes = boxes.Select(b => new BoxOpcionResponse(b.PublicId, b.Nombre, b.Rol.ToString())).ToList(),
                    SelectionToken = tokens.CrearSelectionToken(usuario.Id)
                };

            var emitidos = await EmitirTokensAsync(usuario.Id, boxes[0], ct);
            return new LoginResponse
            {
                AccessToken = emitidos.AccessToken,
                RefreshToken = emitidos.RefreshToken,
                AccessExpiraEn = emitidos.AccessExpiraEn
            };
        }

        public async Task<TokensResponse> SeleccionarBoxAsync(SeleccionarBoxRequest request, CancellationToken ct = default)
        {
            var usuarioId = await tokens.ValidarSelectionTokenAsync(request.SelectionToken)
                ?? throw new UnauthenticatedException("El token de selección no es válido o expiró.");

            return await EmitirParaBoxAsync(usuarioId, request.BoxPublicId, ct);
        }

        public Task<TokensResponse> CambiarBoxAsync(CambiarBoxRequest request, CancellationToken ct = default) =>
            EmitirParaBoxAsync(currentUser.UsuarioId!.Value, request.BoxPublicId, ct);

        public async Task<TokensResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
        {
            var hash = JwtTokenService.HashRefreshToken(request.RefreshToken);
            var token = await db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
                ?? throw RefreshInvalido();

            var ahora = DateTimeOffset.UtcNow;
            if (token.ExpiraEn <= ahora) throw RefreshInvalido();

            // Rotación: el update condicional deja pasar una sola petición aunque
            // lleguen dos con el mismo token.
            var rotados = await db.RefreshTokens
                .Where(t => t.Id == token.Id && t.RevocadoEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, ahora), ct);

            if (rotados == 0)
            {
                // Reutilizar un token ya rotado indica robo: se corta toda la sesión del usuario en ese box.
                await db.RefreshTokens
                    .Where(t => t.UsuarioId == token.UsuarioId && t.BoxId == token.BoxId && t.RevocadoEn == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, ahora), ct);
                throw RefreshInvalido();
            }

            var box = await BoxesActivos(token.UsuarioId).Where(ub => ub.BoxId == token.BoxId).Select(ADeUsuario).FirstOrDefaultAsync(ct)
                ?? throw RefreshInvalido();

            return await EmitirTokensAsync(token.UsuarioId, box, ct);
        }

        public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
        {
            var hash = JwtTokenService.HashRefreshToken(request.RefreshToken);
            var usuarioId = currentUser.UsuarioId;

            await db.RefreshTokens
                .Where(t => t.TokenHash == hash && t.UsuarioId == usuarioId && t.RevocadoEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, DateTimeOffset.UtcNow), ct);
        }

        private async Task<Usuario> ValidarCredencialesAsync(LoginRequest request, CancellationToken ct)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var usuario = await db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Activo, ct);

            if (usuario is null)
            {
                hasher.VerifyHashedPassword(new Usuario(), HashFicticio, request.Password);
                throw CredencialesInvalidas();
            }

            var verificacion = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
            if (verificacion == PasswordVerificationResult.Failed)
                throw CredencialesInvalidas();

            // ExecuteUpdate para no ensuciar la auditoría: iniciar sesión no es editar el usuario.
            var hash = verificacion == PasswordVerificationResult.SuccessRehashNeeded
                ? hasher.HashPassword(usuario, request.Password)
                : usuario.PasswordHash;
            await db.Usuarios
                .Where(u => u.Id == usuario.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.UltimoLogin, DateTimeOffset.UtcNow)
                    .SetProperty(u => u.PasswordHash, hash), ct);

            return usuario;
        }

        private async Task<TokensResponse> EmitirParaBoxAsync(int usuarioId, Guid boxPublicId, CancellationToken ct)
        {
            var box = await BoxesActivos(usuarioId).Where(ub => ub.Box.PublicId == boxPublicId).Select(ADeUsuario).FirstOrDefaultAsync(ct)
                ?? throw new UnauthorizedAccessException("No tienes acceso a ese box.");

            return await EmitirTokensAsync(usuarioId, box, ct);
        }

        private async Task<TokensResponse> EmitirTokensAsync(int usuarioId, BoxDeUsuario box, CancellationToken ct)
        {
            var (accessToken, accessExpiraEn) = tokens.CrearAccessToken(usuarioId, box.BoxId, box.Rol);
            var (refreshToken, hash, refreshExpiraEn) = tokens.CrearRefreshToken();

            db.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = usuarioId,
                BoxId = box.BoxId,
                TokenHash = hash,
                ExpiraEn = refreshExpiraEn
            });
            await db.SaveChangesAsync(ct);

            return new TokensResponse(accessToken, refreshToken, accessExpiraEn);
        }

        private IQueryable<UsuarioBox> BoxesActivos(int usuarioId) =>
            db.UsuarioBoxes.AsNoTracking()
                .Where(ub => ub.UsuarioId == usuarioId && ub.Activo && ub.Usuario.Activo && ub.Box.Activo)
                .OrderBy(ub => ub.Box.Nombre);

        private static UnauthenticatedException CredencialesInvalidas() =>
            new("Email o contraseña incorrectos.");

        private static UnauthenticatedException RefreshInvalido() =>
            new("El refresh token no es válido o expiró.");

        private static readonly Expression<Func<UsuarioBox, BoxDeUsuario>> ADeUsuario =
            ub => new BoxDeUsuario(ub.BoxId, ub.Box.PublicId, ub.Box.Nombre, ub.Rol);

        private sealed record BoxDeUsuario(int BoxId, Guid PublicId, string Nombre, RolUsuario Rol);
    }
}
