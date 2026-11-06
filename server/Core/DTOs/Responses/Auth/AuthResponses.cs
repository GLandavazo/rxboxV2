using System.Text.Json.Serialization;

namespace RxBox.Core.DTOs.Responses.Auth
{
    public record TokensResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessExpiraEn);

    public record BoxOpcionResponse(Guid PublicId, string Nombre, string Rol);

    /// <summary>Un box activo: trae tokens. Varios: trae boxes y selectionToken.</summary>
    public record LoginResponse
    {
        public bool RequiereSeleccion { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AccessToken { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefreshToken { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? AccessExpiraEn { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<BoxOpcionResponse>? Boxes { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SelectionToken { get; init; }
    }
}
