using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    public class TipoMembresia : AuditableEntity, ITenantEntity, IActivable
    {
        public int BoxId { get; set; }
        public string Nombre { get; set; } = null!;
        public int? CategoriaId { get; set; }
        public TipoCobertura TipoCobertura { get; set; }
        public decimal Monto { get; set; }
        public int DuracionDias { get; set; }
        public int? VisitasIncluidas { get; set; }
        public ExpiraDesde ExpiraDesde { get; set; }
        public short MaxPersonas { get; set; } = 1;
        public bool EsPromocion { get; set; }
        public string? DescripcionPromo { get; set; }
        public DateOnly? FechaCaducidad { get; set; }
        public bool Activo { get; set; } = true;
    }
}
