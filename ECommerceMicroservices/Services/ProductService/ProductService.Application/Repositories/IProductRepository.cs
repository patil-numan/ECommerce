using ProductService.Domain.Entities;

namespace ProductService.Application.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(int id);

    Task AddAsync(Product product);

    Task UpdateAsync(Product product);

    Task UpdateRangeAsync(IEnumerable<Product> products);

    Task DeleteAsync(Product product);
}