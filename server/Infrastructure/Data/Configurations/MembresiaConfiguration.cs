using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class MembresiaConfiguration : IEntityTypeConfiguration<Membresia>
    {
        public void Configure(EntityTypeBuilder<Membresia> builder)
        {
            builder.ToTable("membresias", t =>
                t.HasCheckConstraint("ck_membresias_tipo_cobertura", ConfigurationExtensions.ValoresDe<TipoCobertura>("tipo_cobertura")));

            builder.Property(m => m.TipoCobertura).HasConversion<string>();
            builder.Property(m => m.Monto).HasPrecision(12, 2);

            builder.HasIndex(m => new { m.BoxId, m.PersonaId });
            builder.HasIndex(m => new { m.BoxId, m.FechaFin });

            builder.HasTenantFk<Persona>(nameof(Membresia.PersonaId));
            builder.HasTenantFk<Persona>(nameof(Membresia.PersonaVinculadaId));
            builder.HasTenantFk<TipoMembresia>(nameof(Membresia.TipoMembresiaId));
            builder.HasUsuarioFk(nameof(Membresia.CanceladaPor));
        }
    }
}
