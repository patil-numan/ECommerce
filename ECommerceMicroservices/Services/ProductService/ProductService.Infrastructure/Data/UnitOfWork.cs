using Microsoft.EntityFrameworkCore.Storage;
using ProductService.Application.Repositories;

namespace ProductService.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ProductDbContext _context;

    private IDbContextTransaction? _transaction;

    public UnitOfWork(ProductDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}