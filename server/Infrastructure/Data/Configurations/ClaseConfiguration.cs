using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class ClaseConfiguration : IEntityTypeConfiguration<Clase>
    {
        public void Configure(EntityTypeBuilder<Clase> builder)
        {
            builder.ToTable("clases", t =>
                t.HasCheckConstraint("ck_clases_estado", ConfigurationExtensions.ValoresDe<EstadoClase>("estado")));

            builder.Property(c => c.Estado).HasConversion<string>();

            builder.HasIndex(c => new { c.BoxId, c.Fecha });
            builder.HasIndex(c => new { c.ProgramacionId, c.Fecha })
                .IsUnique()
                .HasFilter("programacion_id IS NOT NULL");

            builder.HasTenantFk<ClaseProgramacion>(nameof(Clase.ProgramacionId));
            builder.HasTenantFk<Persona>(nameof(Clase.CoachId));
        }
    }
}
