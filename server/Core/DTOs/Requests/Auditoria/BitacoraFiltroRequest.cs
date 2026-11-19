using RxBox.Core.DTOs.Requests.Comun;
using RxBox.Core.Enums;

namespace RxBox.Core.DTOs.Requests.Auditoria
{
    public class BitacoraFiltroRequest : PaginacionRequest
    {
        public string? Entidad { get; set; }
        public Guid? EntidadPublicId { get; set; }
        public AccionBitacora? Accion { get; set; }

        /// <summary>Fechas en la zona horaria del box; "hasta" incluye el día completo.</summary>
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
    }
}
