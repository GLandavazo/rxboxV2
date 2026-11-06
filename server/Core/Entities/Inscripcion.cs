using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    public class Inscripcion : AuditableEntity, ITenantEntity
    {
        public int BoxId { get; set; }
        public int ClaseId { get; set; }
        public int PersonaId { get; set; }
        public int? MembresiaId { get; set; }
        public EstadoInscripcion Estado { get; set; }
        public CanalReserva CanalReserva { get; set; }
        public CanalCheckin? CanalCheckin { get; set; }
        public DateTimeOffset FechaReserva { get; set; }
        public DateTimeOffset? FechaCheckin { get; set; }
        public int? CheckinBy { get; set; }
    }
}
