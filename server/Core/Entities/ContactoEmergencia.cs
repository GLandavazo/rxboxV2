namespace RxBox.Core.Entities
{
    public class ContactoEmergencia : AuditableEntity, ITenantEntity
    {
        public int BoxId { get; set; }
        public int PersonaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Parentesco { get; set; }
        public string Telefono { get; set; } = null!;
    }
}
