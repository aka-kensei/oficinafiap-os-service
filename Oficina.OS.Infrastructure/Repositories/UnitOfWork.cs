using Oficina.OS.Application.Interfaces;
using Oficina.OS.Infrastructure.Database;

namespace Oficina.OS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly OSDbContext _context;

    public UnitOfWork(OSDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
