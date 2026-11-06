using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    public class ContactoEmergenciaConfiguration : IEntityTypeConfiguration<ContactoEmergencia>
    {
        public void Configure(EntityTypeBuilder<ContactoEmergencia> builder)
        {
            builder.ToTable("contactos_emergencia");

            builder.HasTenantFk<Persona>(nameof(ContactoEmergencia.PersonaId));
        }
    }
}
