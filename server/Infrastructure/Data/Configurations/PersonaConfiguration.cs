using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
    {
        public void Configure(EntityTypeBuilder<Persona> builder)
        {
            builder.ToTable("personas");

            builder.HasIndex(p => new { p.BoxId, p.Codigo }).IsUnique();
            builder.HasIndex(p => new { p.BoxId, p.Apellidos, p.Nombre });
        }
    }
}
