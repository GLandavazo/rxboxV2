using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RxBox.Core.Entities;
using RxBox.Core.Interfaces;

namespace RxBox.Infrastructure.Data
{
    public partial class AppDbContext : DbContext
    {
        private static readonly MethodInfo AplicarFiltroTenantMethod =
            typeof(AppDbContext).GetMethod(nameof(AplicarFiltroTenant), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private readonly ICurrentUserService _currentUser;

        public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser) : base(options)
        {
            _currentUser = currentUser;
        }

        public DbSet<Box> Boxes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<UsuarioBox> UsuarioBoxes { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Persona> Personas { get; set; }
        public DbSet<ContactoEmergencia> ContactosEmergencia { get; set; }
        public DbSet<Catalogo> Catalogos { get; set; }
        public DbSet<TipoMembresia> TiposMembresia { get; set; }
        public DbSet<TipoMembresiaRestriccion> TipoMembresiaRestricciones { get; set; }
        public DbSet<Membresia> Membresias { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<ClaseProgramacion> ClaseProgramaciones { get; set; }
        public DbSet<ClaseProgramacionDia> ClaseProgramacionDias { get; set; }
        public DbSet<Clase> Clases { get; set; }
        public DbSet<Inscripcion> Inscripciones { get; set; }

        // El query filter lo evalúa en cada consulta. Sin box actual no regresa filas.
        private int? BoxIdActual => _currentUser.BoxId;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Toma todas las IEntityTypeConfiguration<T> de este ensamblado.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            foreach (var entity in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
                    modelBuilder.Entity(entity.ClrType).HasIndex(nameof(BaseEntity.PublicId)).IsUnique();

                if (typeof(ITenantEntity).IsAssignableFrom(entity.ClrType))
                    AplicarFiltroTenantMethod.MakeGenericMethod(entity.ClrType).Invoke(this, [modelBuilder]);
            }

            // Nombres en snake_case automáticos (tablas y columnas) salvo que
            // una configuración los fije explícitamente.
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                if (!string.IsNullOrEmpty(tableName) && tableName == entity.ClrType.Name)
                    entity.SetTableName(ToSnakeCase(tableName));

                foreach (var property in entity.GetProperties())
                    if (property.GetColumnName() == property.Name)
                        property.SetColumnName(ToSnakeCase(property.Name));
            }
        }

        private void AplicarFiltroTenant<T>(ModelBuilder modelBuilder) where T : class, ITenantEntity
        {
            modelBuilder.Entity<T>().HasQueryFilter(e => e.BoxId == BoxIdActual);
            modelBuilder.Entity<T>().HasOne<Box>().WithMany()
                .HasForeignKey(nameof(ITenantEntity.BoxId))
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static string ToSnakeCase(string name) =>
            SnakeCaseRegex().Replace(name, "$1_$2").ToLowerInvariant();

        [GeneratedRegex("([a-z0-9])([A-Z])")]
        private static partial Regex SnakeCaseRegex();
    }
}
