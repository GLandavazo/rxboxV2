namespace RxBox.Core.Entities
{
    /// <summary>Insert-only: una corrección es anular y registrar un pago nuevo.</summary>
    public class Pago : AuditableEntity, ITenantEntity
    {
        public int BoxId { get; set; }
        public int MembresiaId { get; set; }
        public int PersonaId { get; set; }
        public decimal Monto { get; set; }
        public int MetodoPagoId { get; set; }
        public DateOnly FechaPago { get; set; }
        public int RegistradoPor { get; set; }
        public DateTimeOffset? AnuladoEn { get; set; }
        public string? Notas { get; set; }
    }
}
