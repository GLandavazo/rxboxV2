using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class ClaseProgramacionDiaConfiguration : IEntityTypeConfiguration<ClaseProgramacionDia>
    {
        public void Configure(EntityTypeBuilder<ClaseProgramacionDia> builder)
        {
            builder.ToTable("clase_programacion_dias", t =>
                t.HasCheckConstraint("ck_clase_programacion_dias_dia_semana", "dia_semana BETWEEN 1 AND 7"));

            builder.HasIndex(d => new { d.ProgramacionId, d.DiaSemana }).IsUnique();

            builder.HasTenantFk<ClaseProgramacion>(nameof(ClaseProgramacionDia.ProgramacionId));
        }
    }
}
