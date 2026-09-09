using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace ECommerce.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepository;
    private readonly Mock<ICategoryRepository> _categoryRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepository =
            new Mock<IProductRepository>();

        _categoryRepository =
            new Mock<ICategoryRepository>();

        _unitOfWork =
            new Mock<IUnitOfWork>();

        _cache =
            new MemoryCache(
                new MemoryCacheOptions());

        _productService =
            new ProductService(
                _productRepository.Object,
                _categoryRepository.Object,
                _unitOfWork.Object,
                _cache);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product
            {
                Id = 1,
                SKU = "LAP-001",
                Name = "Laptop",
                Description = "Test laptop",
                Price = 50000,
                CategoryId = 1,
                StockQuantity = 10
            }
        };

        _productRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(products);

        // Act
        var result =
            await _productService.GetAllAsync();

        // Assert
        Assert.Single(result);

        var product =
            result.First();

        Assert.Equal(1, product.Id);
        Assert.Equal("LAP-001", product.SKU);
        Assert.Equal("Laptop", product.Name);
        Assert.Equal(50000, product.Price);
        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public async Task GetByIdAsync_ProductExists_ReturnsProduct()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            SKU = "LAP-001",
            Name = "Laptop",
            Description = "Test laptop",
            Price = 50000,
            CategoryId = 1,
            StockQuantity = 10
        };

        _productRepository
            .Setup(repository =>
                repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        // Act
        var result =
            await _productService.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("LAP-001", result.SKU);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal(50000, result.Price);
        Assert.Equal(10, result.StockQuantity);
    }

    [Fact]
    public async Task GetByIdAsync_ProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        _productRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result =
            await _productService.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_CreatesProduct()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            SKU = "LAP-001",
            Name = "Laptop",
            Description = "Test laptop",
            Price = 50000,
            CategoryId = 1,
            StockQuantity = 10
        };

        _productRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(
                new List<Product>());

        _productRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Product>()))
            .ReturnsAsync(
                (Product product) =>
                {
                    product.Id = 1;
                    return product;
                });

        // Act
        var result =
            await _productService.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("LAP-001", result.SKU);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("Test laptop", result.Description);
        Assert.Equal(50000, result.Price);
        Assert.Equal(1, result.CategoryId);
        Assert.Equal(10, result.StockQuantity);

        _productRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.Is<Product>(product =>
                        product.SKU == "LAP-001" &&
                        product.Name == "Laptop" &&
                        product.Description == "Test laptop" &&
                        product.Price == 50000 &&
                        product.CategoryId == 1 &&
                        product.StockQuantity == 10)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSku_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            SKU = "LAP-001",
            Name = "Another Laptop",
            Description = "Duplicate SKU test",
            Price = 60000,
            CategoryId = 1,
            StockQuantity = 5
        };

        _productRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(
                new List<Product>
                {
                    new Product
                    {
                        Id = 1,
                        SKU = "LAP-001",
                        Name = "Laptop"
                    }
                });

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    _productService.CreateAsync(dto));

        Assert.Contains(
            "already exists",
            exception.Message);

        _productRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<Product>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmptySku_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            SKU = "",
            Name = "Laptop",
            Description = "Test laptop",
            Price = 50000,
            CategoryId = 1,
            StockQuantity = 10
        };

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    _productService.CreateAsync(dto));

        Assert.Contains(
            "SKU is required",
            exception.Message);

        _productRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<Product>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ProductExists_UpdatesProduct()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            SKU = "LAP-001",
            Name = "Old Laptop",
            Description = "Old description",
            Price = 40000,
            CategoryId = 1,
            StockQuantity = 5
        };

        var dto = new UpdateProductDto
        {
            SKU = "LAP-001",
            Name = "Updated Laptop",
            Description = "Updated description",
            Price = 60000,
            CategoryId = 2,
            StockQuantity = 20
        };

        _productRepository
            .Setup(repository =>
                repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        _productRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(
                new List<Product>
                {
                    product
                });

        _productRepository
            .Setup(repository =>
                repository.UpdateAsync(
                    It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result =
            await _productService.UpdateAsync(
                1,
                dto);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            "LAP-001",
            result.SKU);

        Assert.Equal(
            "Updated Laptop",
            result.Name);

        Assert.Equal(
            "Updated description",
            result.Description);

        Assert.Equal(
            60000,
            result.Price);

        Assert.Equal(
            2,
            result.CategoryId);

        Assert.Equal(
            20,
            result.StockQuantity);

        _productRepository.Verify(
            repository =>
                repository.UpdateAsync(
                    It.Is<Product>(updatedProduct =>
                        updatedProduct.Id == 1 &&
                        updatedProduct.SKU ==
                            "LAP-001" &&
                        updatedProduct.Name ==
                            "Updated Laptop" &&
                        updatedProduct.Description ==
                            "Updated description" &&
                        updatedProduct.Price == 60000 &&
                        updatedProduct.CategoryId == 2 &&
                        updatedProduct.StockQuantity == 20)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateProductDto
        {
            SKU = "LAP-001",
            Name = "Updated Laptop",
            Description = "Updated description",
            Price = 60000,
            CategoryId = 1,
            StockQuantity = 20
        };

        _productRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result =
            await _productService.UpdateAsync(
                999,
                dto);

        // Assert
        Assert.Null(result);

        _productRepository.Verify(
            repository =>
                repository.UpdateAsync(
                    It.IsAny<Product>()),
            Times.Never);

        _productRepository.Verify(
            repository =>
                repository.GetAllAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateSku_ThrowsArgumentException()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            SKU = "LAP-001",
            Name = "Laptop"
        };

        var anotherProduct = new Product
        {
            Id = 2,
            SKU = "MOB-001",
            Name = "Mobile"
        };

        var dto = new UpdateProductDto
        {
            SKU = "MOB-001",
            Name = "Laptop",
            Description = "Updated laptop",
            Price = 60000,
            CategoryId = 1,
            StockQuantity = 20
        };

        _productRepository
            .Setup(repository =>
                repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        _productRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(
                new List<Product>
                {
                    product,
                    anotherProduct
                });

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    _productService.UpdateAsync(
                        1,
                        dto));

        Assert.Contains(
            "already exists",
            exception.Message);

        _productRepository.Verify(
            repository =>
                repository.UpdateAsync(
                    It.IsAny<Product>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptySku_ThrowsArgumentException()
    {
        // Arrange
        var dto = new UpdateProductDto
        {
            SKU = "",
            Name = "Updated Laptop",
            Description = "Updated description",
            Price = 60000,
            CategoryId = 1,
            StockQuantity = 20
        };

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    _productService.UpdateAsync(
                        999,
                        dto));

        Assert.Contains(
            "SKU is required",
            exception.Message);

        _productRepository.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ProductExists_ReturnsTrue()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            SKU = "LAP-001",
            Name = "Laptop",
            CategoryId = 1
        };

        _productRepository
            .Setup(repository =>
                repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        _productRepository
            .Setup(repository =>
                repository.DeleteAsync(1))
            .Returns(Task.CompletedTask);

        // Act
        var result =
            await _productService.DeleteAsync(1);

        // Assert
        Assert.True(result);

        _productRepository.Verify(
            repository =>
                repository.DeleteAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ProductDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _productRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result =
            await _productService.DeleteAsync(999);

        // Assert
        Assert.False(result);

        _productRepository.Verify(
            repository =>
                repository.DeleteAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_SecondCallUsesCache()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product
            {
                Id = 1,
                SKU = "LAP-001",
                Name = "Laptop",
                Description = "Test laptop",
                Price = 50000,
                CategoryId = 1,
                StockQuantity = 10
            }
        };

        _productRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(products);

        // Act
        await _productService.GetAllAsync();
        await _productService.GetAllAsync();

        // Assert
        _productRepository.Verify(
            repository =>
                repository.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_SecondCallUsesCache()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            SKU = "LAP-001",
            Name = "Laptop",
            Description = "Test laptop",
            Price = 50000,
            CategoryId = 1,
            StockQuantity = 10
        };

        _productRepository
            .Setup(repository =>
                repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        // Act
        await _productService.GetByIdAsync(1);
        await _productService.GetByIdAsync(1);

        // Assert
        _productRepository.Verify(
            repository =>
                repository.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAsync_ValidationFails_DoesNotStartTransaction()
    {
        // Arrange
        using var stream =
            CreateExcelFile(
                new[]
                {
                    new BulkProductRowDto
                    {
                        SKU = "",
                        Name = "Laptop",
                        Price = 50000,
                        StockQuantity = 10,
                        Category = "Electronics"
                    }
                });

        // Act
        var result =
            await _productService.BulkUpdateAsync(
                stream);

        // Assert
        Assert.True(
            result.FailedCount > 0);

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(),
            Times.Never);

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.CommitTransactionAsync(),
            Times.Never);

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.RollbackTransactionAsync(),
            Times.Never);
    }

    [Fact]
    public async Task BulkUpdateAsync_ValidExcel_CommitsTransaction()
    {
        // Arrange
        var category =
            new Category
            {
                Id = 1,
                Name = "Electronics"
            };

        var product =
            new Product
            {
                Id = 1,
                SKU = "LAP-001",
                Name = "Old Laptop",
                Description = "Laptop",
                Price = 40000,
                CategoryId = 1,
                StockQuantity = 5
            };

        _productRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(
                new List<Product>
                {
                    product
                });

        _categoryRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(
                new List<Category>
                {
                    category
                });

        _productRepository
            .Setup(repository =>
                repository.UpdateRangeAsync(
                    It.IsAny<IEnumerable<Product>>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(unitOfWork =>
                unitOfWork.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(unitOfWork =>
                unitOfWork.CommitTransactionAsync())
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(unitOfWork =>
                unitOfWork.RollbackTransactionAsync())
            .Returns(Task.CompletedTask);

        using var stream =
            CreateExcelFile(
                new[]
                {
                    new BulkProductRowDto
                    {
                        SKU = "LAP-001",
                        Name = "Updated Laptop",
                        Price = 75000,
                        StockQuantity = 25,
                        Category = "Electronics"
                    }
                });

        // Act
        var result =
            await _productService.BulkUpdateAsync(
                stream);

        // Assert
        Assert.Equal(
            1,
            result.UpdatedCount);

        Assert.Equal(
            0,
            result.CreatedCount);

        Assert.Equal(
            0,
            result.FailedCount);

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(),
            Times.Once);

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.CommitTransactionAsync(),
            Times.Once);

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.RollbackTransactionAsync(),
            Times.Never);
    }

    [Fact]
    public async Task BulkUpdateAsync_DatabaseFailure_RollsBackTransaction()
    {
        // Arrange
        var category =
            new Category
            {
                Id = 1,
                Name = "Electronics"
            };

        var product =
            new Product
            {
                Id = 1,
                SKU = "LAP-001",
                Name = "Old Laptop",
                Description = "Laptop",
                Price = 40000,
                CategoryId = 1,
                StockQuantity = 5
            };

        _productRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(
                new List<Product>
                {
                    product
                });

        _categoryRepository
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(
                new List<Category>
                {
                    category
                });

        _unitOfWork
            .Setup(unitOfWork =>
                unitOfWork.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(unitOfWork =>
                unitOfWork.RollbackTransactionAsync())
            .Returns(Task.CompletedTask);

        _productRepository
            .Setup(repository =>
                repository.UpdateRangeAsync(
                    It.IsAny<IEnumerable<Product>>()))
            .ThrowsAsync(
                new Exception(
                    "Database update failed."));

        using var stream =
            CreateExcelFile(
                new[]
                {
                    new BulkProductRowDto
                    {
                        SKU = "LAP-001",
                        Name = "Updated Laptop",
                        Price = 75000,
                        StockQuantity = 25,
                        Category = "Electronics"
                    }
                });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () =>
                _productService.BulkUpdateAsync(
                    stream));

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(),
            Times.Once);

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.RollbackTransactionAsync(),
            Times.Once);

        _unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.CommitTransactionAsync(),
            Times.Never);
    }

    private static MemoryStream CreateExcelFile(
        IEnumerable<BulkProductRowDto> rows)
    {
        var stream =
            new MemoryStream();

        using (var workbook =
            new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet =
                workbook.Worksheets.Add(
                    "Products");

            worksheet.Cell(1, 1).Value = "SKU";
            worksheet.Cell(1, 2).Value = "Name";
            worksheet.Cell(1, 3).Value = "Price";
            worksheet.Cell(1, 4).Value = "StockQuantity";
            worksheet.Cell(1, 5).Value = "Category";

            var rowNumber = 2;

            foreach (var row in rows)
            {
                worksheet.Cell(
                    rowNumber,
                    1).Value = row.SKU;

                worksheet.Cell(
                    rowNumber,
                    2).Value = row.Name;

                worksheet.Cell(
                    rowNumber,
                    3).Value = row.Price;

                worksheet.Cell(
                    rowNumber,
                    4).Value = row.StockQuantity;

                worksheet.Cell(
                    rowNumber,
                    5).Value = row.Category;

                rowNumber++;
            }

            workbook.SaveAs(stream);
        }

        stream.Position = 0;

        return stream;
    }
}