namespace RxBox.Core.Entities
{
    /// <summary>Tenant. No lleva box_id ni query filter.</summary>
    public class Box : AuditableEntity, IActivable
    {
        public string Nombre { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string ZonaHoraria { get; set; } = "America/Hermosillo";
        public int VentanaCheckinMinutos { get; set; }
        public int VentanaReservaDias { get; set; }
        public string? LogoUrl { get; set; }
        public string? ColorPrimario { get; set; }
        public bool Activo { get; set; } = true;
    }
}
