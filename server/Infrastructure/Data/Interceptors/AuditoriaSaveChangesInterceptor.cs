using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using RxBox.Core.Interfaces;

namespace RxBox.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// En cada SaveChanges: asigna <see cref="ITenantEntity.BoxId"/> a las filas nuevas,
    /// rellena <see cref="IAuditable"/> y escribe una fila de <see cref="Bitacora"/> por
    /// cada registro creado, modificado o borrado, en la misma transacción.
    /// </summary>
    public class AuditoriaSaveChangesInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
    {
        // No se registran: la propia bitácora, los tokens y el historial de negocio.
        private static readonly HashSet<Type> TiposExcluidos =
            [typeof(Bitacora), typeof(RefreshToken), typeof(MembresiaEvento)];

        // Nunca van en "cambios". Las de auditoría, id y box ya están en la fila.
        private static readonly HashSet<string> PropiedadesExcluidas =
        [
            nameof(BaseEntity.Id), nameof(BaseEntity.PublicId), nameof(ITenantEntity.BoxId),
            nameof(IAuditable.CreadoPor), nameof(IAuditable.FechaCreacion),
            nameof(IAuditable.EditadoPor), nameof(IAuditable.FechaEdicion),
            nameof(Usuario.PasswordHash), nameof(RefreshToken.TokenHash)
        ];

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,   // "antes" / "despues"
            Converters = { new JsonStringEnumConverter() }
        };

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            Preparar(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Preparar(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void Preparar(DbContext? context)
        {
            if (context is null) return;

            AsignarBox(context);
            AplicarAuditoria(context);
            RegistrarBitacora(context);
        }

        // Respeta un BoxId explícito: los procesos sin request (seed, alta de box) no tienen box actual.
        private void AsignarBox(DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.BoxId == 0)
                    entry.Entity.BoxId = currentUser.BoxId
                        ?? throw new InvalidOperationException(
                            $"No hay box actual para insertar {entry.Metadata.ClrType.Name}.");
            }
        }

        private void AplicarAuditoria(DbContext context)
        {
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

        private void RegistrarBitacora(DbContext context)
        {
            var ahora = DateTimeOffset.UtcNow;
            var registros = new List<Bitacora>();

            // ToList: no se puede agregar al ChangeTracker mientras se recorre.
            foreach (var entry in context.ChangeTracker.Entries().ToList())
            {
                if (TiposExcluidos.Contains(entry.Metadata.ClrType)) continue;

                AccionBitacora? accion = entry.State switch
                {
                    EntityState.Added => AccionBitacora.CREAR,
                    EntityState.Modified => AccionBitacora.EDITAR,
                    EntityState.Deleted => AccionBitacora.ELIMINAR,
                    _ => null
                };
                if (accion is null) continue;

                var cambios = Cambios(entry);
                if (accion == AccionBitacora.EDITAR && cambios.Count == 0) continue;   // Update() sin cambios reales

                registros.Add(new Bitacora
                {
                    BoxId = BoxDe(entry),
                    UsuarioId = currentUser.UsuarioId,
                    Entidad = entry.Metadata.ClrType.Name,
                    EntidadPublicId = (entry.Entity as BaseEntity)?.PublicId,
                    Accion = accion.Value,
                    Cambios = cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios, Json),
                    Fecha = ahora,
                    Ip = currentUser.Ip
                });
            }

            context.Set<Bitacora>().AddRange(registros);
        }

        // El box del propio registro si tiene columna box_id (también usuario_boxes, que no es
        // ITenantEntity); si no (usuarios, boxes), el del usuario actual.
        private int? BoxDe(EntityEntry entry)
        {
            if (entry.Metadata.FindProperty(nameof(ITenantEntity.BoxId)) is null) return currentUser.BoxId;

            var propiedad = entry.Property(nameof(ITenantEntity.BoxId));
            return (int?)(entry.State == EntityState.Deleted ? propiedad.OriginalValue : propiedad.CurrentValue);
        }

        private static Dictionary<string, Cambio> Cambios(EntityEntry entry)
        {
            var cambios = new Dictionary<string, Cambio>();

            foreach (var p in entry.Properties)
            {
                if (PropiedadesExcluidas.Contains(p.Metadata.Name)) continue;
                var columna = p.Metadata.GetColumnName();

                switch (entry.State)
                {
                    case EntityState.Added when p.CurrentValue is not null:
                        cambios[columna] = new(null, p.CurrentValue);
                        break;

                    case EntityState.Modified when p.IsModified && !Equals(p.OriginalValue, p.CurrentValue):
                        cambios[columna] = new(p.OriginalValue, p.CurrentValue);
                        break;

                    case EntityState.Deleted:
                        cambios[columna] = new(p.OriginalValue, null);
                        break;
                }
            }

            return cambios;
        }

        private sealed record Cambio(object? Antes, object? Despues);
    }
}
