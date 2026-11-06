namespace RxBox.Core.Entities
{
    /// <summary>Global, no tenant: rol y persona viven por box en <see cref="UsuarioBox"/>.</summary>
    public class Usuario : AuditableEntity, IActivable
    {
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public DateTimeOffset? UltimoLogin { get; set; }
        public bool Activo { get; set; } = true;
    }
}
