using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface IProductService
{
    // Customer operations
    Task<IEnumerable<ProductDto>> GetAllAsync();

    Task<ProductDto?> GetByIdAsync(int id);

    // Admin operations
    Task<ProductDto> CreateAsync(
        CreateProductDto dto);

    Task<ProductDto?> UpdateAsync(
        int id,
        UpdateProductDto dto);

    Task<bool> DeleteAsync(int id);

    // Bulk admin operation
    Task<BulkProductUpdateResultDto> BulkUpdateAsync(
        Stream fileStream);
}