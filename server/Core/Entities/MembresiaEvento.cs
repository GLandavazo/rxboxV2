using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    /// <summary>
    /// Historial de negocio de una membresía. Lo escribe el servicio de membresías/pagos
    /// (módulo 04), no el interceptor. Insert-only, sin public_id ni auditoría.
    /// </summary>
    public class MembresiaEvento : ITenantEntity
    {
        public int Id { get; set; }
        public int BoxId { get; set; }
        public int MembresiaId { get; set; }
        public int PersonaId { get; set; }
        public int? PagoId { get; set; }
        public TipoEventoMembresia Tipo { get; set; }
        public string Descripcion { get; set; } = null!;

        /// <summary>jsonb con el detalle, ej. EXTENSION: { fecha_fin_antes, fecha_fin_despues, dias, motivo }.</summary>
        public string? Datos { get; set; }

        public int? UsuarioId { get; set; }
        public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    }
}
