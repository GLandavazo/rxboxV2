using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    public class Clase : AuditableEntity, ITenantEntity
    {
        public int BoxId { get; set; }
        public int? ProgramacionId { get; set; }
        public DateOnly Fecha { get; set; }
        public string Nombre { get; set; } = null!;
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public int? CoachId { get; set; }
        public short Cupo { get; set; }
        public short Sobrecupo { get; set; }
        public bool SobrecupoHabilitado { get; set; }

        /// <summary>Open box o festivo: ignora las restricciones de membresía.</summary>
        public bool EsEspecial { get; set; }
        public EstadoClase Estado { get; set; }
        public string? Notas { get; set; }
    }
}
