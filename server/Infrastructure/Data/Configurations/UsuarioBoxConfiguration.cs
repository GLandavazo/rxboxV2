using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class UsuarioBoxConfiguration : IEntityTypeConfiguration<UsuarioBox>
    {
        public void Configure(EntityTypeBuilder<UsuarioBox> builder)
        {
            builder.ToTable("usuario_boxes", t =>
                t.HasCheckConstraint("ck_usuario_boxes_rol", "rol IN ('ADMIN', 'COACH')"));

            builder.Property(ub => ub.Rol).HasConversion<string>();

            builder.HasIndex(ub => new { ub.UsuarioId, ub.BoxId }).IsUnique();

            builder.HasOne(ub => ub.Usuario).WithMany().HasForeignKey(ub => ub.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(ub => ub.Box).WithMany().HasForeignKey(ub => ub.BoxId).OnDelete(DeleteBehavior.Restrict);
            builder.HasTenantFk<Persona>(nameof(UsuarioBox.PersonaId));
        }
    }
}
