using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class TipoMembresiaConfiguration : IEntityTypeConfiguration<TipoMembresia>
    {
        public void Configure(EntityTypeBuilder<TipoMembresia> builder)
        {
            builder.ToTable("tipos_membresia", t =>
            {
                t.HasCheckConstraint("ck_tipos_membresia_tipo_cobertura", ConfigurationExtensions.ValoresDe<TipoCobertura>("tipo_cobertura"));
                t.HasCheckConstraint("ck_tipos_membresia_expira_desde", ConfigurationExtensions.ValoresDe<ExpiraDesde>("expira_desde"));
                t.HasCheckConstraint("ck_tipos_membresia_monto", "monto >= 0");
                t.HasCheckConstraint("ck_tipos_membresia_duracion_dias", "duracion_dias > 0");
                t.HasCheckConstraint("ck_tipos_membresia_max_personas", "max_personas >= 1");
                t.HasCheckConstraint("ck_tipos_membresia_visitas_incluidas", "tipo_cobertura = 'ILIMITADA' OR visitas_incluidas IS NOT NULL");
            });

            builder.Property(t => t.TipoCobertura).HasConversion<string>();
            builder.Property(t => t.ExpiraDesde).HasConversion<string>();
            builder.Property(t => t.Monto).HasPrecision(12, 2);
            builder.Property(t => t.MaxPersonas).HasDefaultValue((short)1);

            builder.HasIndex(t => new { t.BoxId, t.Nombre }).IsUnique();

            builder.HasTenantFk<Catalogo>(nameof(TipoMembresia.CategoriaId));
        }
    }
}
