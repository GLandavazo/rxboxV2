using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    /// <summary>
    /// Bitácora técnica. Insert-only y sin FK hacia lo que registra, para sobrevivir a
    /// borrados. No es ITenantEntity (hay eventos sin box, como un login fallido):
    /// las consultas filtran por box a mano.
    /// </summary>
    public class Bitacora
    {
        public long Id { get; set; }
        public int? BoxId { get; set; }
        public int? UsuarioId { get; set; }
        public string Entidad { get; set; } = null!;
        public Guid? EntidadPublicId { get; set; }
        public AccionBitacora Accion { get; set; }

        /// <summary>jsonb: <c>{ "columna": { "antes": x, "despues": y } }</c>, o datos del acceso en LOGIN*.</summary>
        public string? Cambios { get; set; }

        public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
        public string? Ip { get; set; }
    }
}
