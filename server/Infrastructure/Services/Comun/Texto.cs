

namespace RxBox.Infrastructure.Services.Comun
{
    public static class Texto
    {
        /// <summary>"  Ana   María " → "Ana María"</summary>
        public static string Normalizar(string valor) =>
            string.Join(' ', valor.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        /// <summary>null, "" o "   " → null; si no, el valor sin espacios en los extremos.</summary>
        public static string? Opcional(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
