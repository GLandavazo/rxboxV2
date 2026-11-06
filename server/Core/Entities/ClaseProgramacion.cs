namespace RxBox.Core.Entities
{
    public class ClaseProgramacion : AuditableEntity, ITenantEntity, IActivable
    {
        public int BoxId { get; set; }
        public string Nombre { get; set; } = null!;
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public int? CoachId { get; set; }
        public short Cupo { get; set; }
        public short Sobrecupo { get; set; }
        public bool ExigeReservaCheckin { get; set; }
        public DateOnly VigenteDesde { get; set; }
        public DateOnly? VigenteHasta { get; set; }
        public bool Activo { get; set; } = true;
    }
}
