namespace RxBox.Core.Interfaces
{
    public interface ICatalogoSeedService
    {
        /// <summary>Siembra los catálogos default de un box. Es idempotente: solo agrega los que falten.</summary>
        Task SembrarAsync(int boxId, CancellationToken ct = default);
    }
}
