using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class InscripcionConfiguration : IEntityTypeConfiguration<Inscripcion>
    {
        public void Configure(EntityTypeBuilder<Inscripcion> builder)
        {
            builder.ToTable("inscripciones", t =>
            {
                t.HasCheckConstraint("ck_inscripciones_estado", ConfigurationExtensions.ValoresDe<EstadoInscripcion>("estado"));
                t.HasCheckConstraint("ck_inscripciones_canal_reserva", ConfigurationExtensions.ValoresDe<CanalReserva>("canal_reserva"));
                t.HasCheckConstraint("ck_inscripciones_canal_checkin", ConfigurationExtensions.ValoresDe<CanalCheckin>("canal_checkin"));
            });

            builder.Property(i => i.Estado).HasConversion<string>();
            builder.Property(i => i.CanalReserva).HasConversion<string>();
            builder.Property(i => i.CanalCheckin).HasConversion<string>();

            builder.HasIndex(i => new { i.ClaseId, i.PersonaId })
                .IsUnique()
                .HasFilter("estado <> 'CANCELADO'");
            builder.HasIndex(i => new { i.BoxId, i.PersonaId });
            builder.HasIndex(i => new { i.BoxId, i.ClaseId });

            builder.HasTenantFk<Clase>(nameof(Inscripcion.ClaseId));
            builder.HasTenantFk<Persona>(nameof(Inscripcion.PersonaId));
            builder.HasTenantFk<Membresia>(nameof(Inscripcion.MembresiaId));
            builder.HasUsuarioFk(nameof(Inscripcion.CheckinBy));
        }
    }
}
