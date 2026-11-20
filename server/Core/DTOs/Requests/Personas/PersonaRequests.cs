using RxBox.Core.DTOs.Requests.Comun;
using RxBox.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RxBox.Core.DTOs.Requests.Personas
{
    public class PersonaFiltroRequest : PaginacionRequest
    {
        /// <summary>Texto en nombre completo, código, email o teléfono.</summary>
        [StringLength(100)]
        public string? Buscar { get; set; }

        public TipoPersona? Tipo { get; set; }
        public bool SoloActivos { get; set; } = true;
    }
}
