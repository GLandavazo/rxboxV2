using RxBox.Core.Enums;

namespace RxBox.Core.Entities
{
    public class Catalogo : AuditableEntity, ITenantEntity, IActivable
    {
        public int BoxId { get; set; }
        public TipoCatalogo Tipo { get; set; }
        public string Nombre { get; set; } = null!;
        public short Orden { get; set; }
        public bool Activo { get; set; } = true;
    }
}
