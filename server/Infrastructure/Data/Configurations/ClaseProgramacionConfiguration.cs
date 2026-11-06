using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class ClaseProgramacionConfiguration : IEntityTypeConfiguration<ClaseProgramacion>
    {
        public void Configure(EntityTypeBuilder<ClaseProgramacion> builder)
        {
            builder.ToTable("clase_programaciones", t =>
            {
                t.HasCheckConstraint("ck_clase_programaciones_horas", "hora_fin > hora_inicio");
                t.HasCheckConstraint("ck_clase_programaciones_cupo", "cupo > 0");
                t.HasCheckConstraint("ck_clase_programaciones_sobrecupo", "sobrecupo >= 0");
            });

            builder.HasTenantFk<Persona>(nameof(ClaseProgramacion.CoachId));
        }
    }
}
