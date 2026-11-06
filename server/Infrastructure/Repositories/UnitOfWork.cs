using Microsoft.EntityFrameworkCore.Storage;
using RxBox.Core.Entities;
using RxBox.Core.Interfaces.Repositories;
using RxBox.Infrastructure.Data;

namespace RxBox.Infrastructure.Repositories
{
    public class UnitOfWork(AppDbContext db) : IUnitOfWork
    {
        private readonly AppDbContext _db = db;
        private readonly Dictionary<Type, object> _repos = [];

        public IGenericRepository<T> Repository<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo))
            {
                repo = new GenericRepository<T>(_db);
                _repos[typeof(T)] = repo;
            }
            return (IGenericRepository<T>)repo;
        }

        public Task<int> SaveAsync(CancellationToken ct = default) =>
            _db.SaveChangesAsync(ct);

        public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default) =>
            new EfTransaction(await _db.Database.BeginTransactionAsync(ct));

        public void Dispose() => _db.Dispose();

        private sealed class EfTransaction(IDbContextTransaction tx) : ITransaction
        {
            public Task CommitAsync(CancellationToken ct = default) => tx.CommitAsync(ct);
            public Task RollbackAsync(CancellationToken ct = default) => tx.RollbackAsync(ct);
            public ValueTask DisposeAsync() => tx.DisposeAsync();
        }
    }
}
