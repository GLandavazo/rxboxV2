namespace RxBox.Core.Interfaces
{
    /// <summary>
    /// Datos del usuario autenticado en la request actual, extraídos del JWT propio.
    /// Lo consumen AppDbContext (query filter), el interceptor (box y auditoría) y
    /// cualquier servicio que necesite saber quién ejecuta la operación.
    /// </summary>
    public interface ICurrentUserService
    {
        int? UsuarioId { get; }   // claim "sub"
        int? BoxId { get; }       // claim "box_id"
        string? Rol { get; }      // claim "rol"
        bool IsAuthenticated { get; }
    }
}
