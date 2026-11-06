using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    /// <summary>
    /// Pagada, vigente y visitas usadas se calculan, no se guardan. TipoNombre,
    /// TipoCobertura, CategoriaNombre y Monto son snapshot del tipo al vender.
    /// </summary>
    public class Membresia : AuditableEntity, ITenantEntity
    {
        public int BoxId { get; set; }
        public int PersonaId { get; set; }
        public int? PersonaVinculadaId { get; set; }
        public int? TipoMembresiaId { get; set; }
        public string TipoNombre { get; set; } = null!;
        public TipoCobertura TipoCobertura { get; set; }
        public string? CategoriaNombre { get; set; }
        public decimal Monto { get; set; }

        /// <summary>Nula mientras expira_desde = PRIMERA_VISITA y no hay visita, igual que <see cref="FechaFin"/>.</summary>
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public int? VisitasTotales { get; set; }
        public DateTimeOffset? CanceladaEn { get; set; }
        public int? CanceladaPor { get; set; }
        public string? Notas { get; set; }
    }
}
