using System.Linq.Expressions;
using RxBox.Core.Entities;

namespace RxBox.Core.Interfaces.Repositories
{
    /// <summary>
    /// CRUD + consultas por predicado LINQ simple. Úsalo vía
    /// <c>uow.Repository&lt;T&gt;()</c>. Crea un repositorio específico solo
    /// cuando necesites Include / proyección / SQL crudo, o encapsular una
    /// regla.
    /// </summary>
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(T entity, CancellationToken ct = default);
        void Update(T entity);
        void Remove(T entity);
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        IQueryable<T> AsQueryable();
    }
}
