using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;
using RxBox.Core.Options;

namespace RxBox.Infrastructure.Data.Seed
{
    /// <summary>
    /// Datos de arranque para Development: box "alpha", su admin y los catálogos.
    /// Es idempotente: se ejecuta en cada arranque y solo crea lo que falte.
    /// </summary>
    public class DevelopmentSeeder(
        AppDbContext db,
        IPasswordHasher<Usuario> hasher,
        ICatalogoSeedService catalogos,
        IOptions<SeedOptions> options)
    {
        private const string SlugBox = "alpha";
        private const string CodigoAdmin = "ADMIN";

        private readonly SeedOptions _options = options.Value;

        public async Task SeedAsync(CancellationToken ct = default)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var box = await ObtenerBoxAsync(ct);
            var usuario = await ObtenerUsuarioAsync(ct);
            var persona = await ObtenerPersonaAsync(box.Id, ct);

            if (!await db.UsuarioBoxes.AnyAsync(ub => ub.UsuarioId == usuario.Id && ub.BoxId == box.Id, ct))
            {
                db.UsuarioBoxes.Add(new UsuarioBox
                {
                    UsuarioId = usuario.Id,
                    BoxId = box.Id,
                    PersonaId = persona.Id,
                    Rol = RolUsuario.ADMIN
                });
                await db.SaveChangesAsync(ct);
            }

            await catalogos.SembrarAsync(box.Id, ct);
            await tx.CommitAsync(ct);
        }

        private async Task<Box> ObtenerBoxAsync(CancellationToken ct)
        {
            var box = await db.Boxes.FirstOrDefaultAsync(b => b.Slug == SlugBox, ct);
            if (box is not null) return box;

            box = new Box { Nombre = "Alpha", Slug = SlugBox, VentanaCheckinMinutos = 30, VentanaReservaDias = 7 };
            db.Boxes.Add(box);
            await db.SaveChangesAsync(ct);
            return box;
        }

        private async Task<Usuario> ObtenerUsuarioAsync(CancellationToken ct)
        {
            var email = _options.AdminEmail.ToLowerInvariant();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
            if (usuario is not null) return usuario;

            if (string.IsNullOrWhiteSpace(_options.AdminPassword))
                throw new InvalidOperationException("Falta 'Seed:AdminPassword' para crear el usuario admin.");

            usuario = new Usuario { Email = _options.AdminEmail };
            usuario.PasswordHash = hasher.HashPassword(usuario, _options.AdminPassword);
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync(ct);
            return usuario;
        }

        private async Task<Persona> ObtenerPersonaAsync(int boxId, CancellationToken ct)
        {
            var persona = await db.Personas.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.BoxId == boxId && p.Codigo == CodigoAdmin, ct);
            if (persona is not null) return persona;

            persona = new Persona
            {
                BoxId = boxId,
                Codigo = CodigoAdmin,
                Nombre = "Admin",
                Apellidos = "Alpha",
                Email = _options.AdminEmail
            };
            db.Personas.Add(persona);
            await db.SaveChangesAsync(ct);
            return persona;
        }
    }
}
