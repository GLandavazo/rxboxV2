namespace RxBox.Core.DTOs.Responses.Comun
{
    public record PaginaResponse<T>(IReadOnlyList<T> Items, int Total, int Pagina, int Tamano)
    {
        public int TotalPaginas => Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamano);
    }
}
