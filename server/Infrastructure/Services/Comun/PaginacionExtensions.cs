using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Requests.Comun;
using RxBox.Core.DTOs.Responses.Comun;

namespace RxBox.Infrastructure.Services.Comun
{
    public static class PaginacionExtensions
    {
        /// <summary>Cuenta y pagina. El query ya debe venir ordenado.</summary>
        public static async Task<PaginaResponse<T>> ToPaginaAsync<T>(
            this IQueryable<T> query, PaginacionRequest paginacion, CancellationToken ct = default)
        {
            var total = await query.CountAsync(ct);
            var items = await query.Skip(paginacion.Saltar).Take(paginacion.Tamano).ToListAsync(ct);
            return new PaginaResponse<T>(items, total, paginacion.Pagina, paginacion.Tamano);
        }
    }
}
