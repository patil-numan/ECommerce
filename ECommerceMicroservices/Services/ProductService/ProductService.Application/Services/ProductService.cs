using ProductService.Application.DTOs;
using ProductService.Application.Repositories;
using ProductService.Domain.Entities;

namespace ProductService.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();

        return products.Select(MapToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        ValidateProduct(
            dto.SKU,
            dto.Name,
            dto.Description,
            dto.Price,
            dto.StockQuantity);

        var category = await _categoryRepository
            .GetByIdAsync(dto.CategoryId);

        if (category == null)
            throw new ArgumentException("Category not found.");

        var product = new Product
        {
            SKU = dto.SKU.Trim(),
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            StockQuantity = dto.StockQuantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateProductDto dto)
    {
        ValidateProduct(
            dto.SKU,
            dto.Name,
            dto.Description,
            dto.Price,
            dto.StockQuantity);

        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            return false;

        var category = await _categoryRepository
            .GetByIdAsync(dto.CategoryId);

        if (category == null)
            throw new ArgumentException("Category not found.");

        product.SKU = dto.SKU.Trim();
        product.Name = dto.Name.Trim();
        product.Description = dto.Description.Trim();
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;
        product.StockQuantity = dto.StockQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            return false;

        await _productRepository.DeleteAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReduceStockAsync(
        int id,
        int quantity)
    {
        if (id <= 0)
            throw new ArgumentException(
                "Product ID must be greater than zero.");

        if (quantity <= 0)
            throw new ArgumentException(
                "Quantity must be greater than zero.");

        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            return false;

        if (product.StockQuantity < quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for product '{product.Name}'.");

        product.StockQuantity -= quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static void ValidateProduct(
        string sku,
        string name,
        string description,
        decimal price,
        int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");

        if (price <= 0)
            throw new ArgumentException(
                "Price must be greater than zero.");

        if (stockQuantity < 0)
            throw new ArgumentException(
                "Stock quantity cannot be negative.");
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            SKU = product.SKU,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            StockQuantity = product.StockQuantity
        };
    }
}