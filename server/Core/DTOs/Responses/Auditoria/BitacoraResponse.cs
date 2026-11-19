using System.Text.Json;
using RxBox.Core.Enums;

namespace RxBox.Core.DTOs.Responses.Auditoria
{
    /// <summary><see cref="Cambios"/> sale como objeto JSON, no como texto.</summary>
    public record BitacoraResponse(
        DateTimeOffset Fecha,
        AccionBitacora Accion,
        string Entidad,
        Guid? EntidadPublicId,
        string? UsuarioEmail,
        JsonElement? Cambios,
        string? Ip);
}
