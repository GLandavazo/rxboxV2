namespace RxBox.Core.Entities
{
    /// <summary>Sin public_id, auditoría ni query filter. Solo se guarda el hash del token.</summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int BoxId { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTimeOffset ExpiraEn { get; set; }
        public DateTimeOffset? RevocadoEn { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public Usuario Usuario { get; set; } = null!;
        public Box Box { get; set; } = null!;
    }
}
