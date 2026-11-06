using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RxBox.Core.Entities;
using RxBox.Core.Interfaces;

namespace RxBox.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// En cada SaveChanges asigna <see cref="ITenantEntity.BoxId"/> a las filas nuevas y
    /// rellena los campos de <see cref="IAuditable"/> con el usuario actual.
    /// Se registra en <c>AddDatabase</c> y se engancha al DbContext.
    /// </summary>
    public class AuditoriaSaveChangesInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            AsignarBox(eventData.Context);
            AplicarAuditoria(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            AsignarBox(eventData.Context);
            AplicarAuditoria(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // Respeta un BoxId explícito: los procesos sin request (seed, alta de box) no tienen box actual.
        private void AsignarBox(DbContext? context)
        {
            if (context is null) return;

            foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.BoxId == 0)
                    entry.Entity.BoxId = currentUser.BoxId
                        ?? throw new InvalidOperationException(
                            $"No hay box actual para insertar {entry.Metadata.ClrType.Name}.");
            }
        }

        private void AplicarAuditoria(DbContext? context)
        {
            if (context is null) return;

            var ahora = DateTimeOffset.UtcNow;
            var usuarioId = currentUser.UsuarioId;

            foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.FechaCreacion = ahora;
                        entry.Entity.CreadoPor ??= usuarioId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.FechaEdicion = ahora;
                        entry.Entity.EditadoPor = usuarioId;
                        break;
                }
            }
        }
    }
}
