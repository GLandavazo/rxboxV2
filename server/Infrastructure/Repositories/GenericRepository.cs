using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RxBox.Core.Entities;
using RxBox.Core.Interfaces.Repositories;
using RxBox.Infrastructure.Data;

namespace RxBox.Infrastructure.Repositories
{
    public class GenericRepository<T>(AppDbContext db) : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext _db = db;

        public Task<T?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default) =>
            _db.Set<T>().FirstOrDefaultAsync(e => e.PublicId == publicId, ct);

        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
            await _db.Set<T>().AsNoTracking().ToListAsync(ct);

        public async Task AddAsync(T entity, CancellationToken ct = default) =>
            await _db.Set<T>().AddAsync(entity, ct);

        public void Update(T entity) => _db.Set<T>().Update(entity);

        public void Remove(T entity) => _db.Set<T>().Remove(entity);

        public async Task<IReadOnlyList<T>> FindAsync(
            Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            await _db.Set<T>().Where(predicate).ToListAsync(ct);

        public Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            _db.Set<T>().AnyAsync(predicate, ct);

        public IQueryable<T> AsQueryable() => _db.Set<T>();
    }
}
