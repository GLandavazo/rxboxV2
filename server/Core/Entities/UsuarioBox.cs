using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    /// <summary>
    /// Pertenencia de un usuario a un box. Lleva box_id pero no es
    /// <see cref="ITenantEntity"/>: se consulta en login, antes de que haya box actual.
    /// </summary>
    public class UsuarioBox : IAuditable, IActivable
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int BoxId { get; set; }
        public int PersonaId { get; set; }
        public RolUsuario Rol { get; set; }
        public bool Activo { get; set; } = true;

        public int? CreadoPor { get; set; }
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
        public int? EditadoPor { get; set; }
        public DateTimeOffset? FechaEdicion { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public Box Box { get; set; } = null!;
    }
}
