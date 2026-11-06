using Microsoft.EntityFrameworkCore;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;

namespace RxBox.Infrastructure.Data.Seed
{
    public class CatalogoSeedService(AppDbContext db) : ICatalogoSeedService
    {
        private static readonly (TipoCatalogo Tipo, string Nombre, short Orden)[] Defaults =
        [
            (TipoCatalogo.CATEGORIA_MEMBRESIA, "General", 1),
            (TipoCatalogo.CATEGORIA_MEMBRESIA, "Estudiante", 2),
            (TipoCatalogo.METODO_PAGO, "Efectivo", 1),
            (TipoCatalogo.METODO_PAGO, "Transferencia", 2),
            (TipoCatalogo.METODO_PAGO, "Tarjeta", 3)
        ];

        public async Task SembrarAsync(int boxId, CancellationToken ct = default)
        {
            // Sin query filter: al crear un box el usuario actual no pertenece a él.
            var existentes = await db.Catalogos.IgnoreQueryFilters()
                .Where(c => c.BoxId == boxId)
                .Select(c => new { c.Tipo, c.Nombre })
                .ToListAsync(ct);

            var faltantes = Defaults
                .Where(d => !existentes.Any(e => e.Tipo == d.Tipo && e.Nombre == d.Nombre))
                .Select(d => new Catalogo { BoxId = boxId, Tipo = d.Tipo, Nombre = d.Nombre, Orden = d.Orden });

            db.Catalogos.AddRange(faltantes);
            await db.SaveChangesAsync(ct);
        }
    }
}
