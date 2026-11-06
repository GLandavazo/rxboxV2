namespace RxBox.Core.Options
{
    /// <summary>
    /// Sección "Jwt" de appsettings. <see cref="Secret"/> se carga por variable de
    /// entorno (RXBOX__Jwt__Secret) fuera de Development.
    /// </summary>
    public class JwtOptions
    {
        public string Secret { get; set; } = "";
        public string Issuer { get; set; } = "rxbox-api";

        /// <summary>Audiencia del access token.</summary>
        public string Audience { get; set; } = "rxbox-web";

        /// <summary>Audiencia propia del selection token: así el bearer lo rechaza como access token.</summary>
        public string SelectionAudience { get; set; } = "rxbox-seleccion-box";

        public int AccessMinutos { get; set; } = 15;
        public int RefreshDias { get; set; } = 30;
        public int SelectionMinutos { get; set; } = 5;
    }
}
