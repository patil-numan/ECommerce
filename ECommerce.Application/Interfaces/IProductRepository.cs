using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(int id);

    Task<Product> AddAsync(Product product);

    Task UpdateAsync(Product product);

    Task UpdateRangeAsync(IEnumerable<Product> products);

    Task DeleteAsync(int id);
}