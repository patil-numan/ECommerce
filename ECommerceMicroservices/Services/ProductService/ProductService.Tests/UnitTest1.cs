using Moq;
using ProductService.Application.Repositories;
using ProductService.Domain.Entities;

namespace ProductService.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task ReduceStockAsync_ShouldReduceStock_WhenQuantityIsAvailable()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            SKU = "TEST-001",
            Name = "Test Product",
            Description = "Test product",
            Price = 100,
            CategoryId = 1,
            StockQuantity = 10
        };

        var productRepository = new Mock<IProductRepository>();
        var categoryRepository = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        productRepository
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        var service =
            new ProductService.Application.Services.ProductService(
                productRepository.Object,
                categoryRepository.Object,
                unitOfWork.Object);

        // Act
        var result = await service.ReduceStockAsync(1, 3);

        // Assert
        Assert.True(result);
        Assert.Equal(7, product.StockQuantity);

        productRepository.Verify(
            repository => repository.UpdateAsync(product),
            Times.Once);

        unitOfWork.Verify(
            unit => unit.SaveChangesAsync(),
            Times.Once);
    }
}
