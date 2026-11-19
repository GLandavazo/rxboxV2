using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class BitacoraConfiguration : IEntityTypeConfiguration<Bitacora>
    {
        public void Configure(EntityTypeBuilder<Bitacora> builder)
        {
            builder.ToTable("bitacora", t =>
                t.HasCheckConstraint("ck_bitacora_accion", ConfigurationExtensions.ValoresDe<AccionBitacora>("accion")));

            builder.Property(b => b.Accion).HasConversion<string>();
            builder.Property(b => b.Cambios).HasColumnType("jsonb");

            builder.HasIndex(b => new { b.BoxId, b.Entidad, b.EntidadPublicId });
            builder.HasIndex(b => new { b.BoxId, b.Fecha });
        }
    }
}
