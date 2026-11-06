using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RxBox.Core.Entities;

namespace RxBox.Infrastructure.Data.Configurations
{
    internal static class ConfigurationExtensions
    {
        /// <summary>
        /// FK compuesta (box_id, x_id) → (box_id, id), que hace imposible referenciar
        /// filas de otro box. Declara la alternate key en el padre. Es opcional si
        /// <paramref name="propiedad"/> es nullable. Nunca SET NULL: anularía box_id.
        /// </summary>
        public static void HasTenantFk<TPadre>(this EntityTypeBuilder builder, string propiedad)
            where TPadre : BaseEntity, ITenantEntity =>
            builder.HasOne(typeof(TPadre))
                .WithMany()
                .HasForeignKey(nameof(ITenantEntity.BoxId), propiedad)
                .HasPrincipalKey(nameof(ITenantEntity.BoxId), nameof(BaseEntity.Id))
                .OnDelete(DeleteBehavior.Restrict);

        /// <summary>FK simple a usuarios, que es global.</summary>
        public static void HasUsuarioFk(this EntityTypeBuilder builder, string propiedad) =>
            builder.HasOne(typeof(Usuario))
                .WithMany()
                .HasForeignKey(propiedad)
                .OnDelete(DeleteBehavior.Restrict);

        /// <summary>Expresión de un CHECK que limita la columna a los valores del enum.</summary>
        public static string ValoresDe<TEnum>(string columna) where TEnum : struct, Enum =>
            $"{columna} IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})";
    }
}
