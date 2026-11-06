using RxBox.Core.Entities;

namespace RxBox.Core.Interfaces.Repositories
{
    /// <summary>
    /// A medida que agregues entidades con consultas propias, expón aquí su
    /// repositorio específico (ej. <c>IUsuarioRepository Usuarios { get; }</c>)
    /// con lazy-init, siguiendo el mismo patrón que <c>Repository&lt;T&gt;()</c>.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : BaseEntity;
        Task<int> SaveAsync(CancellationToken ct = default);
        Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
    }
}
