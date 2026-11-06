using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class CatalogoConfiguration : IEntityTypeConfiguration<Catalogo>
    {
        public void Configure(EntityTypeBuilder<Catalogo> builder)
        {
            builder.ToTable("catalogos", t =>
                t.HasCheckConstraint("ck_catalogos_tipo", ConfigurationExtensions.ValoresDe<TipoCatalogo>("tipo")));

            builder.Property(c => c.Tipo).HasConversion<string>();

            builder.HasIndex(c => new { c.BoxId, c.Tipo, c.Nombre }).IsUnique();
        }
    }
}
