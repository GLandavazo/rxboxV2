using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class PagoConfiguration : IEntityTypeConfiguration<Pago>
    {
        public void Configure(EntityTypeBuilder<Pago> builder)
        {
            builder.ToTable("pagos", t => t.HasCheckConstraint("ck_pagos_monto", "monto > 0"));

            builder.Property(p => p.Monto).HasPrecision(12, 2);

            builder.HasIndex(p => new { p.BoxId, p.FechaPago });
            builder.HasIndex(p => new { p.BoxId, p.MembresiaId });

            builder.HasTenantFk<Membresia>(nameof(Pago.MembresiaId));
            builder.HasTenantFk<Persona>(nameof(Pago.PersonaId));
            builder.HasTenantFk<Catalogo>(nameof(Pago.MetodoPagoId));
            builder.HasUsuarioFk(nameof(Pago.RegistradoPor));
        }
    }
}
