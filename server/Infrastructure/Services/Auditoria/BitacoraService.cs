using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Requests.Auditoria;
using RxBox.Core.DTOs.Responses.Auditoria;
using RxBox.Core.DTOs.Responses.Comun;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Infrastructure.Data;
using RxBox.Infrastructure.Services.Comun;

namespace RxBox.Infrastructure.Services.Auditoria
{
    /// <summary>Bitacora no tiene query filter: el box se filtra aquí a mano.</summary>
    public class BitacoraService(AppDbContext db, ICurrentUserService currentUser) : IBitacoraService
    {
        public async Task<PaginaResponse<BitacoraResponse>> ListarAsync(BitacoraFiltroRequest f, CancellationToken ct = default)
        {
            var boxId = currentUser.BoxId ?? throw new UnauthenticatedException("La sesión no tiene box.");

            var query = db.Bitacora.AsNoTracking().Where(b => b.BoxId == boxId);

            if (!string.IsNullOrWhiteSpace(f.Entidad)) query = query.Where(b => b.Entidad == f.Entidad);
            if (f.EntidadPublicId is not null) query = query.Where(b => b.EntidadPublicId == f.EntidadPublicId);
            if (f.Accion is not null) query = query.Where(b => b.Accion == f.Accion.Value);

            if (f.Desde is not null || f.Hasta is not null)
            {
                var zona = TimeZoneInfo.FindSystemTimeZoneById(
                    await db.Boxes.Where(b => b.Id == boxId).Select(b => b.ZonaHoraria).SingleAsync(ct));

                if (f.Desde is not null)
                {
                    var desde = InicioDelDiaUtc(f.Desde.Value, zona);
                    query = query.Where(b => b.Fecha >= desde);
                }
                if (f.Hasta is not null)
                {
                    var hastaExclusivo = InicioDelDiaUtc(f.Hasta.Value.AddDays(1), zona);
                    query = query.Where(b => b.Fecha < hastaExclusivo);
                }
            }

            // usuarios es global: sin query filter, el join no pierde filas.
            var filas =
                from b in query
                join u in db.Usuarios on b.UsuarioId equals (int?)u.Id into us
                from u in us.DefaultIfEmpty()
                orderby b.Fecha descending, b.Id descending
                select new Fila(b.Fecha, b.Accion, b.Entidad, b.EntidadPublicId, u != null ? u.Email : null, b.Cambios, b.Ip);

            var pagina = await filas.ToPaginaAsync(f, ct);

            return new PaginaResponse<BitacoraResponse>(
                [.. pagina.Items.Select(ToResponse)], pagina.Total, pagina.Pagina, pagina.Tamano);
        }

        private static DateTimeOffset InicioDelDiaUtc(DateOnly dia, TimeZoneInfo zona) =>
            new(TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue), zona));

        private static BitacoraResponse ToResponse(Fila f) =>
            new(f.Fecha, f.Accion, f.Entidad, f.EntidadPublicId, f.UsuarioEmail, ComoJson(f.Cambios), f.Ip);

        private static JsonElement? ComoJson(string? json)
        {
            if (json is null) return null;
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        private sealed record Fila(
            DateTimeOffset Fecha, AccionBitacora Accion, string Entidad, Guid? EntidadPublicId,
            string? UsuarioEmail, string? Cambios, string? Ip);
    }
}
