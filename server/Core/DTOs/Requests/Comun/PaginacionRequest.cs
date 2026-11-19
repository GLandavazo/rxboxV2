using System.ComponentModel.DataAnnotations;

namespace RxBox.Core.DTOs.Requests.Comun
{
    /// <summary>Base de los filtros paginados. Se recibe con [FromQuery].</summary>
    public abstract class PaginacionRequest
    {
        [Range(1, int.MaxValue)]
        public int Pagina { get; set; } = 1;

        [Range(1, 100)]
        public int Tamano { get; set; } = 20;

        public int Saltar => (Pagina - 1) * Tamano;   // solo lectura: el model binding lo ignora
    }
}
