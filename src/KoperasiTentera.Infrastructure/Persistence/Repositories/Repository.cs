using KoperasiTentera.Application.Abstractions.Persistence.Repositories;
using KoperasiTentera.Infrastructure.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; 
using System.Linq.Expressions;

namespace KoperasiTentera.Infrastructure.Persistence.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<TEntity> _dbSet;
        private readonly ILogger<Repository<TEntity>> _logger;

        public Repository(ApplicationDbContext context, ILogger<Repository<TEntity>> logger)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
            _logger = logger;
        }

        public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Repository<{Entity}>: GetByIdAsync started. Id={Id}", typeof(TEntity).Name, id);

            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);

            if (entity is null)
                _logger.LogInformation("Repository<{Entity}>: Entity not found. Id={Id}", typeof(TEntity).Name, id);
            else
                _logger.LogDebug("Repository<{Entity}>: Entity found. Id={Id}", typeof(TEntity).Name, id);

            return entity;
        }

        public async Task<IReadOnlyList<TEntity>> FindAsync(
    Expression<Func<TEntity, bool>> predicate,
    CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Repository<{Entity}>: FindAsync started", typeof(TEntity).Name);

            // Tracking ON — taake baad mein Update() kaam kare (OTP IsUsed flag etc.)
            var list = await _dbSet
                .Where(predicate)
                .ToListAsync(cancellationToken);

            _logger.LogDebug(
                "Repository<{Entity}>: FindAsync returned {Count} records",
                typeof(TEntity).Name, list.Count);

            return list;
        }

        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Repository<{Entity}>: FirstOrDefaultAsync started", typeof(TEntity).Name);

            var entity = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(predicate, cancellationToken);

            if (entity is null)
                _logger.LogDebug("Repository<{Entity}>: No entity matched the predicate", typeof(TEntity).Name);

            return entity;
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Repository<{Entity}>: GetAllAsync started", typeof(TEntity).Name);

            var list = await _dbSet
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Repository<{Entity}>: Retrieved {Count} records", typeof(TEntity).Name, list.Count);

            return list;
        }

        public async Task<(IReadOnlyList<TEntity> Items, long TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Repository<{Entity}>: GetPagedAsync started. Page={Page}, PageSize={PageSize}",
                typeof(TEntity).Name, page, pageSize);

            IQueryable<TEntity> query = _dbSet.AsNoTracking();

            if (predicate is not null)
                query = query.Where(predicate);

            var totalCount = await query.LongCountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Repository<{Entity}>: Paged result. Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}, Returned={Returned}",
                typeof(TEntity).Name, page, pageSize, totalCount, items.Count);

            return (items, totalCount);
        }

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Repository<{Entity}>: AddAsync started", typeof(TEntity).Name);

            await _dbSet.AddAsync(entity, cancellationToken);

            _logger.LogDebug("Repository<{Entity}>: Entity added to change tracker", typeof(TEntity).Name);
        }

        public void Update(TEntity entity)
        {
            _logger.LogInformation("Repository<{Entity}>: Update started", typeof(TEntity).Name);
            _dbSet.Update(entity);
        }

        public void Remove(TEntity entity)
        {
            _logger.LogInformation("Repository<{Entity}>: Remove started", typeof(TEntity).Name);
            _dbSet.Remove(entity);
        }

        public async Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Repository<{Entity}>: ExistsAsync started", typeof(TEntity).Name);

            var exists = await _dbSet.AnyAsync(predicate, cancellationToken);

            _logger.LogDebug("Repository<{Entity}>: Exists={Exists}", typeof(TEntity).Name, exists);

            return exists;
        }
    }
}
