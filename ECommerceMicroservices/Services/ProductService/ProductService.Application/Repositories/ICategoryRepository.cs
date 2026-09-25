using ProductService.Domain.Entities;

namespace ProductService.Application.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();

    Task<Category?> GetByIdAsync(int id);

    Task AddAsync(Category category);

    Task UpdateAsync(Category category);

    Task DeleteAsync(Category category);
}