using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class TipoMembresiaRestriccionConfiguration : IEntityTypeConfiguration<TipoMembresiaRestriccion>
    {
        public void Configure(EntityTypeBuilder<TipoMembresiaRestriccion> builder)
        {
            builder.ToTable("tipo_membresia_restricciones", t =>
            {
                t.HasCheckConstraint("ck_tipo_membresia_restricciones_programacion_u_horario",
                    "(programacion_id IS NOT NULL AND hora_inicio IS NULL AND hora_fin IS NULL AND dias_semana IS NULL)"
                    + " OR (programacion_id IS NULL AND hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND hora_fin > hora_inicio)");
                t.HasCheckConstraint("ck_tipo_membresia_restricciones_dias_semana",
                    "dias_semana <@ ARRAY[1, 2, 3, 4, 5, 6, 7]::smallint[]");
            });

            builder.Property(r => r.DiasSemana).HasColumnType("smallint[]");

            builder.HasTenantFk<TipoMembresia>(nameof(TipoMembresiaRestriccion.TipoMembresiaId));
            builder.HasTenantFk<ClaseProgramacion>(nameof(TipoMembresiaRestriccion.ProgramacionId));
        }
    }
}
