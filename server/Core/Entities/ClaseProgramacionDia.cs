namespace RxBox.Core.Entities
{
    /// <summary>Hija, sin public_id ni auditoría. <see cref="DiaSemana"/>: 1 = lunes.</summary>
    public class ClaseProgramacionDia : ITenantEntity
    {
        public int Id { get; set; }
        public int BoxId { get; set; }
        public int ProgramacionId { get; set; }
        public short DiaSemana { get; set; }
    }
}
