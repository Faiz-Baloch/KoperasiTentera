using System.Linq.Expressions; 

namespace KoperasiTentera.Application.Abstractions.Persistence.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TEntity>> FindAsync(  Expression<Func<TEntity, bool>> predicate,  CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<TEntity> Items, long TotalCount)> GetPagedAsync(
            int page, int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default);
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
