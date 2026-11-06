using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("usuarios");

            // El UQ sobre lower(email) es un índice de expresión: EF no lo modela,
            // va como SQL en la migración.
        }
    }
}
