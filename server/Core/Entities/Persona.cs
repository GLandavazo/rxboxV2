namespace RxBox.Core.Entities
{
    public class Persona : AuditableEntity, ITenantEntity, IActivable
    {
        public int BoxId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public bool EsAtleta { get; set; }
        public bool EsCoach { get; set; }
        public bool Activo { get; set; } = true;
    }
}
