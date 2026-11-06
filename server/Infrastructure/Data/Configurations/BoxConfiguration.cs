using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class BoxConfiguration : IEntityTypeConfiguration<Box>
    {
        public void Configure(EntityTypeBuilder<Box> builder)
        {
            builder.ToTable("boxes");

            builder.Property(b => b.ZonaHoraria).HasDefaultValue("America/Hermosillo");
            builder.HasIndex(b => b.Slug).IsUnique();

            // Slug inmutable: EF lanza excepción si se intenta modificar.
            builder.Property(b => b.Slug).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        }
    }
}
