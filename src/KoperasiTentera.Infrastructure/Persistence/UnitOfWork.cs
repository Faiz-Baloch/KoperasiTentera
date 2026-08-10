using KoperasiTentera.Application.Persistence;
using KoperasiTentera.Infrastructure.DATA;
using Microsoft.Extensions.Logging;

namespace KoperasiTentera.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("UnitOfWork: SaveChangesAsync started");
            var affected = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("UnitOfWork: {Affected} record(s) saved", affected);
            return affected;
        }
    }
}
