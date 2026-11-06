using Microsoft.EntityFrameworkCore;
using RxBox.Infrastructure.Data;
using RxBox.Infrastructure.Data.Seed;

namespace RxBox.API.Extensions
{
    public static class StartupExtensions
    {
        /// <summary>
        /// Aplica migraciones pendientes al arrancar. Si falla, la API no
        /// levanta (mejor caer temprano que servir con esquema inconsistente).
        /// </summary>
        public static async Task MigrateAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                logger.LogInformation("Aplicando migraciones...");
                await db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error al aplicar migraciones. La API no puede arrancar.");
                throw;
            }
        }

        /// <summary>Siembra los datos de Development. Va después de las migraciones.</summary>
        public static async Task SeedDevelopmentAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync();
        }
    }
}
