namespace ProductService.Application.Repositories;

public interface IUnitOfWork
{
    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();

    Task SaveChangesAsync();
}