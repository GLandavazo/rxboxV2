namespace RxBox.Core.Options
{
    /// <summary>Sección "Seed" de appsettings. Solo se usa en Development.</summary>
    public class SeedOptions
    {
        public string AdminEmail { get; set; } = "admin@rxbox.local";
        public string AdminPassword { get; set; } = "";
    }
}
