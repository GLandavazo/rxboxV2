using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;
using RxBox.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class MembresiaEventoConfiguration : IEntityTypeConfiguration<MembresiaEvento>
    {
        public void Configure(EntityTypeBuilder<MembresiaEvento> builder)
        {
            builder.ToTable("membresia_eventos", t =>
                t.HasCheckConstraint("ck_membresia_eventos_tipo", ConfigurationExtensions.ValoresDe<TipoEventoMembresia>("tipo")));

            builder.Property(e => e.Tipo).HasConversion<string>();
            builder.Property(e => e.Datos).HasColumnType("jsonb");

            builder.HasIndex(e => new { e.BoxId, e.MembresiaId, e.Fecha });
            builder.HasIndex(e => new { e.BoxId, e.PersonaId, e.Fecha });

            builder.HasTenantFk<Membresia>(nameof(MembresiaEvento.MembresiaId));
            builder.HasTenantFk<Persona>(nameof(MembresiaEvento.PersonaId));
            builder.HasTenantFk<Pago>(nameof(MembresiaEvento.PagoId));
            builder.HasUsuarioFk(nameof(MembresiaEvento.UsuarioId));
        }
    }
}
