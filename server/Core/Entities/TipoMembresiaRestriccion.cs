namespace RxBox.Core.Entities
{
    /// <summary>
    /// Hija, sin public_id ni auditoría. Sin filas = sin restricción; con filas, la clase
    /// debe cumplir al menos una. <see cref="DiasSemana"/>: 1 = lunes.
    /// </summary>
    public class TipoMembresiaRestriccion : ITenantEntity
    {
        public int Id { get; set; }
        public int BoxId { get; set; }
        public int TipoMembresiaId { get; set; }
        public int? ProgramacionId { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public short[]? DiasSemana { get; set; }
    }
}
