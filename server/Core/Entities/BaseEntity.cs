namespace RxBox.Core.Entities
{
    /// <summary>
    /// Raíz de la jerarquía de entidades. <see cref="Id"/> es interno (identity) y
    /// nunca sale de la API; lo único que se expone en DTOs y rutas es
    /// <see cref="PublicId"/>.
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.CreateVersion7();
    }

    /// <summary>
    /// Entidades con <c>public_id</c> y rastro de auditoría. El
    /// <c>AuditoriaSaveChangesInterceptor</c> rellena estos campos en cada SaveChanges.
    /// </summary>
    public abstract class AuditableEntity : BaseEntity, IAuditable
    {
        public int? CreadoPor { get; set; }
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
        public int? EditadoPor { get; set; }
        public DateTimeOffset? FechaEdicion { get; set; }
    }

    /// <summary>
    /// Campos de auditoría. Es interfaz porque hay tablas auditadas sin
    /// <c>public_id</c> (usuario_boxes), que no pueden heredar de <see cref="AuditableEntity"/>.
    /// </summary>
    public interface IAuditable
    {
        int? CreadoPor { get; set; }
        DateTimeOffset FechaCreacion { get; set; }
        int? EditadoPor { get; set; }
        DateTimeOffset? FechaEdicion { get; set; }
    }

    /// <summary>
    /// Entidad de un box: AppDbContext le aplica el query filter por box del usuario
    /// actual y el interceptor asigna <see cref="BoxId"/> al insertar.
    /// </summary>
    public interface ITenantEntity
    {
        int BoxId { get; set; }
    }

    /// <summary>
    /// Entidades que se habilitan o deshabilitan sin borrarse físicamente
    /// (soft delete / activación). Se togglea vía PATCH .../toggle.
    /// </summary>
    public interface IActivable
    {
        bool Activo { get; set; }
    }
}
