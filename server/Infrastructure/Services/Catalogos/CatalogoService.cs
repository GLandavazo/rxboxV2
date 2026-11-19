using Microsoft.EntityFrameworkCore;
using RxBox.Core.DTOs.Requests.Catalogos;
using RxBox.Core.DTOs.Responses.Catalogos;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Exceptions;
using RxBox.Core.Interfaces;
using RxBox.Core.Interfaces.Repositories;
namespace RxBox.Infrastructure.Services.Catalogos
{
    public class CatalogoService(IUnitOfWork uow) : ICatalogoService
    {
        private IGenericRepository<Catalogo> Repo => uow.Repository<Catalogo>();

        public async Task<IReadOnlyList<CatalogoResponse>> ListarAsync(
            TipoCatalogo? tipo, bool soloActivos, CancellationToken ct = default)
        {
            var query = Repo.AsQueryable().AsNoTracking();

            if (tipo is not null) query = query.Where(c => c.Tipo == tipo.Value);
            if (soloActivos) query = query.Where(c => c.Activo);

            return await query
                .OrderBy(c => c.Tipo).ThenBy(c => c.Orden).ThenBy(c => c.Nombre)
                .Select(c => new CatalogoResponse(c.PublicId, c.Tipo, c.Nombre, c.Orden, c.Activo))
                .ToListAsync(ct);
        }

        public async Task<CatalogoResponse> ObtenerAsync(Guid publicId, CancellationToken ct = default) =>
            ToResponse(await ObtenerEntidadAsync(publicId, ct));

        // El query filter de tenant hace que un publicId de otro box también dé 404.
        private async Task<Catalogo> ObtenerEntidadAsync(Guid publicId, CancellationToken ct) =>
            await Repo.GetByPublicIdAsync(publicId, ct)
                ?? throw new NotFoundException("Catálogo", publicId);


        public async Task<CatalogoResponse> CrearAsync(CatalogoCreateRequest request, CancellationToken ct = default)
        {
            var tipo = request.Tipo!.Value;
            var nombre = Normalizar(request.Nombre);

            await ValidarNombreDisponibleAsync(tipo, nombre, excluir: null, ct);

            var catalogo = new Catalogo
            {
                Tipo = tipo,
                Nombre = nombre,
                Orden = request.Orden ?? await SiguienteOrdenAsync(tipo, ct)
            };

            await Repo.AddAsync(catalogo, ct);
            await uow.SaveAsync(ct);   // el interceptor pone BoxId y auditoría
            return ToResponse(catalogo);
        }

        public async Task<CatalogoResponse> EditarAsync(Guid publicId, CatalogoUpdateRequest request, CancellationToken ct = default)
        {
            var catalogo = await ObtenerEntidadAsync(publicId, ct);   // rastreada
            var nombre = Normalizar(request.Nombre);

            // Se excluye a sí mismo: "Efectivo" → "EFECTIVO" es válido (CA-003).
            await ValidarNombreDisponibleAsync(catalogo.Tipo, nombre, excluir: catalogo.PublicId, ct);

            catalogo.Nombre = nombre;
            if (request.Orden is not null) catalogo.Orden = request.Orden.Value;

            await uow.SaveAsync(ct);   // el interceptor pone EditadoPor y FechaEdicion
            return ToResponse(catalogo);
        }

        public async Task<CatalogoResponse> CambiarEstadoAsync(Guid publicId, CancellationToken ct = default)
        {
            var catalogo = await ObtenerEntidadAsync(publicId, ct);

            if (catalogo.Activo)
            {
                var activosDelTipo = await Repo.AsQueryable()
                    .CountAsync(c => c.Tipo == catalogo.Tipo && c.Activo, ct);

                if (activosDelTipo <= 1)
                    throw new ConflictException("Debe quedar al menos un elemento activo en este catálogo.");
            }

            catalogo.Activo = !catalogo.Activo;
            await uow.SaveAsync(ct);
            return ToResponse(catalogo);
        }



        private static CatalogoResponse ToResponse(Catalogo c) =>
            new(c.PublicId, c.Tipo, c.Nombre, c.Orden, c.Activo);

        // "  Pago   en línea " → "Pago en línea"
        private static string Normalizar(string nombre) =>
            string.Join(' ', nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries));


        /// <summary>Único por box y tipo, sin distinguir mayúsculas, contando inactivos.</summary>
        private async Task ValidarNombreDisponibleAsync(TipoCatalogo tipo, string nombre, Guid? excluir, CancellationToken ct)
        {
            var nombreLower = nombre.ToLower();

            var existente = await Repo.AsQueryable().AsNoTracking()
                .Where(c => c.Tipo == tipo
                         && c.Nombre.ToLower() == nombreLower
                         && (excluir == null || c.PublicId != excluir.Value))
                .Select(c => new { c.Nombre, c.Activo })
                .FirstOrDefaultAsync(ct);

            if (existente is null) return;

            throw new ConflictException(existente.Activo
                ? $"Ya existe '{existente.Nombre}' en este catálogo."
                : $"Ya existe '{existente.Nombre}' pero está inactivo. Reactívalo en lugar de crear otro.");
        }

        private async Task<short> SiguienteOrdenAsync(TipoCatalogo tipo, CancellationToken ct)
        {
            var maximo = await Repo.AsQueryable()
                .Where(c => c.Tipo == tipo)
                .MaxAsync(c => (short?)c.Orden, ct);   // null si no hay ninguno

            return (short)((maximo ?? 0) + 1);
        }
    }
}
