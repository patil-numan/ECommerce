using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);

        return product;
    }

    public async Task AddRangeAsync(
        IEnumerable<Product> products)
    {
        await _context.Products.AddRangeAsync(products);
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
    }

    public async Task UpdateRangeAsync(
        IEnumerable<Product> products)
    {
        _context.Products.UpdateRange(products);
    }

    public async Task DeleteAsync(int id)
    {
        var product =
            await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return;
        }

        // Soft delete:
        // Keep the product in the database so existing
        // OrderItem records can continue referencing it.
        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;
    }
}