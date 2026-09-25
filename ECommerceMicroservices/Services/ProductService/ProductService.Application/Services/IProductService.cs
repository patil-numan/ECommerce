using ProductService.Application.DTOs;

namespace ProductService.Application.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();

    Task<ProductDto?> GetByIdAsync(int id);

    Task<ProductDto> CreateAsync(CreateProductDto dto);

    Task<bool> UpdateAsync(int id, UpdateProductDto dto);

    Task<bool> DeleteAsync(int id);

    Task<bool> ReduceStockAsync(int id, int quantity);
}