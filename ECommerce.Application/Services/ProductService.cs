using ClosedXML.Excel;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    private const string ProductsCacheKey = "products_all";
    private const string ProductCacheKeyPrefix = "product_";

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMemoryCache cache)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var cachedProducts =
            _cache.Get(ProductsCacheKey) as IEnumerable<ProductDto>;

        if (cachedProducts != null)
        {
            return cachedProducts;
        }

        var products =
            await _productRepository.GetAllAsync();

        var result =
            products
                .Select(MapToDto)
                .ToList();

        _cache.Set(
            ProductsCacheKey,
            result,
            TimeSpan.FromMinutes(5));

        return result;
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var cacheKey =
            $"{ProductCacheKeyPrefix}{id}";

        var cachedProduct =
            _cache.Get(cacheKey) as ProductDto;

        if (cachedProduct != null)
        {
            return cachedProduct;
        }

        var product =
            await _productRepository.GetByIdAsync(id);

        if (product == null)
        {
            return null;
        }

        var result =
            MapToDto(product);

        _cache.Set(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5));

        return result;
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SKU))
        {
            throw new ArgumentException(
                "SKU is required.");
        }

        var sku =
            dto.SKU.Trim();

        var products =
            await _productRepository.GetAllAsync();

        var skuExists =
            products.Any(p =>
                string.Equals(
                    p.SKU?.Trim(),
                    sku,
                    StringComparison.OrdinalIgnoreCase));

        if (skuExists)
        {
            throw new ArgumentException(
                $"A product with SKU '{sku}' already exists.");
        }

        var product =
            new Product
            {
                SKU = sku,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                StockQuantity = dto.StockQuantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

        var createdProduct =
            await _productRepository.AddAsync(product);

        ClearProductCache();

        return MapToDto(createdProduct);
    }

    public async Task<ProductDto?> UpdateAsync(
        int id,
        UpdateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SKU))
        {
            throw new ArgumentException(
                "SKU is required.");
        }

        var product =
            await _productRepository.GetByIdAsync(id);

        if (product == null)
        {
            return null;
        }

        var sku =
            dto.SKU.Trim();

        var products =
            await _productRepository.GetAllAsync();

        var skuExists =
            products.Any(p =>
                p.Id != id &&
                string.Equals(
                    p.SKU?.Trim(),
                    sku,
                    StringComparison.OrdinalIgnoreCase));

        if (skuExists)
        {
            throw new ArgumentException(
                $"A product with SKU '{sku}' already exists.");
        }

        product.SKU = sku;
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);

        ClearProductCache(id);

        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product =
            await _productRepository.GetByIdAsync(id);

        if (product == null)
        {
            return false;
        }

        await _productRepository.DeleteAsync(id);

        ClearProductCache(id);

        return true;
    }

    public async Task<BulkProductUpdateResultDto> BulkUpdateAsync(
        Stream excelStream)
    {
        var result =
            new BulkProductUpdateResultDto();

        using var workbook =
            new XLWorkbook(excelStream);

        var worksheet =
            workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            result.FailedCount = 1;

            result.Errors.Add(
                new BulkProductUpdateErrorDto
                {
                    RowNumber = 1,
                    Error =
                        "The Excel file does not contain a worksheet."
                });

            return result;
        }

        var lastRowUsed =
            worksheet.LastRowUsed();

        var lastColumnUsed =
            worksheet.LastColumnUsed();

        if (lastRowUsed == null ||
            lastColumnUsed == null)
        {
            result.FailedCount = 1;

            result.Errors.Add(
                new BulkProductUpdateErrorDto
                {
                    RowNumber = 1,
                    Error =
                        "The Excel worksheet is empty."
                });

            return result;
        }

        var lastColumnNumber =
            lastColumnUsed.ColumnNumber();

        if (lastColumnNumber < 5)
        {
            result.FailedCount = 1;

            result.Errors.Add(
                new BulkProductUpdateErrorDto
                {
                    RowNumber = 1,
                    Error =
                        "The Excel file must contain these columns: " +
                        "SKU, Name, Price, StockQuantity, Category."
                });

            return result;
        }

        // ------------------------------------------------------------
        // READ HEADERS
        // ------------------------------------------------------------

        var headers =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        for (
            var column = 1;
            column <= lastColumnNumber;
            column++)
        {
            var header =
                worksheet
                    .Cell(1, column)
                    .GetString()
                    .Trim();

            if (!string.IsNullOrWhiteSpace(header))
            {
                headers[header] = column;
            }
        }

        var requiredHeaders =
            new[]
            {
                "SKU",
                "Name",
                "Price",
                "StockQuantity",
                "Category"
            };

        foreach (var requiredHeader in requiredHeaders)
        {
            if (!headers.ContainsKey(requiredHeader))
            {
                result.FailedCount = 1;

                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = 1,
                        Error =
                            $"Missing required column '{requiredHeader}'."
                    });

                return result;
            }
        }

        // ------------------------------------------------------------
        // LOAD EXISTING DATA
        // ------------------------------------------------------------

        var products =
            (await _productRepository.GetAllAsync())
            .ToList();

        var categories =
            (await _categoryRepository.GetAllAsync())
            .ToList();

        var productsBySku =
            products
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.SKU))
                .GroupBy(
                    p => p.SKU.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase);

        var categoriesByName =
            categories
                .Where(c =>
                    !string.IsNullOrWhiteSpace(c.Name))
                .GroupBy(
                    c => c.Name.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase);

        // ------------------------------------------------------------
        // VALIDATE EXCEL
        // ------------------------------------------------------------

        var excelSkus =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var rows =
            new List<BulkProductRowDto>();

        var lastRowNumber =
            lastRowUsed.RowNumber();

        for (
            var rowNumber = 2;
            rowNumber <= lastRowNumber;
            rowNumber++)
        {
            var row =
                worksheet.Row(rowNumber);

            var sku =
                row.Cell(headers["SKU"])
                    .GetString()
                    .Trim();

            var name =
                row.Cell(headers["Name"])
                    .GetString()
                    .Trim();

            var categoryName =
                row.Cell(headers["Category"])
                    .GetString()
                    .Trim();

            var priceCell =
                row.Cell(headers["Price"]);

            var stockCell =
                row.Cell(headers["StockQuantity"]);

            var rowHasData =
                !string.IsNullOrWhiteSpace(sku) ||
                !string.IsNullOrWhiteSpace(name) ||
                !string.IsNullOrWhiteSpace(categoryName) ||
                !priceCell.IsEmpty() ||
                !stockCell.IsEmpty();

            if (!rowHasData)
            {
                continue;
            }

            var rowHasError = false;

            // --------------------------------------------------------
            // SKU
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(sku))
            {
                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = rowNumber,
                        SKU = string.Empty,
                        Error = "SKU is required."
                    });

                rowHasError = true;
            }
            else if (!excelSkus.Add(sku))
            {
                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = rowNumber,
                        SKU = sku,
                        Error =
                            $"Duplicate SKU '{sku}' found in the Excel file."
                    });

                rowHasError = true;
            }

            // --------------------------------------------------------
            // NAME
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = rowNumber,
                        SKU = sku,
                        Error = "Name is required."
                    });

                rowHasError = true;
            }

            // --------------------------------------------------------
            // PRICE
            // --------------------------------------------------------

            decimal price;

            if (!decimal.TryParse(
                    priceCell.GetString(),
                    out price))
            {
                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = rowNumber,
                        SKU = sku,
                        Error =
                            "Price must be a valid decimal number."
                    });

                rowHasError = true;
            }
            else if (price < 0)
            {
                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = rowNumber,
                        SKU = sku,
                        Error =
                            "Price cannot be negative."
                    });

                rowHasError = true;
            }

            // --------------------------------------------------------
            // STOCK
            // --------------------------------------------------------

            int stockQuantity;

            if (!int.TryParse(
                    stockCell.GetString(),
                    out stockQuantity))
            {
                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = rowNumber,
                        SKU = sku,
                        Error =
                            "StockQuantity must be a valid integer."
                    });

                rowHasError = true;
            }
            else if (stockQuantity < 0)
            {
                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = rowNumber,
                        SKU = sku,
                        Error =
                            "StockQuantity cannot be negative."
                    });

                rowHasError = true;
            }

            // --------------------------------------------------------
            // CATEGORY
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                result.Errors.Add(
                    new BulkProductUpdateErrorDto
                    {
                        RowNumber = rowNumber,
                        SKU = sku,
                        Error =
                            "Category is required."
                    });

                rowHasError = true;
            }

            if (rowHasError)
            {
                continue;
            }

            rows.Add(
                new BulkProductRowDto
                {
                    SKU = sku,
                    Name = name,
                    Price = price,
                    StockQuantity = stockQuantity,
                    Category = categoryName
                });
        }

        // ------------------------------------------------------------
        // STOP IF VALIDATION FAILED
        // ------------------------------------------------------------

        if (result.Errors.Count > 0)
        {
            result.FailedCount =
                result.Errors.Count;

            return result;
        }

        // ------------------------------------------------------------
        // IDENTIFY NEW CATEGORIES
        // ------------------------------------------------------------

        var newCategories =
            new Dictionary<string, Category>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (categoriesByName.ContainsKey(row.Category))
            {
                continue;
            }

            if (!newCategories.ContainsKey(row.Category))
            {
                var category =
                    new Category
                    {
                        Name = row.Category.Trim(),
                        Description = string.Empty
                    };

                newCategories.Add(
                    row.Category,
                    category);
            }
        }

        // ------------------------------------------------------------
        // START TRANSACTION
        // ------------------------------------------------------------

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // --------------------------------------------------------
            // CREATE NEW CATEGORIES
            // --------------------------------------------------------

            foreach (var category in newCategories.Values)
            {
                var createdCategory =
                    await _categoryRepository.AddAsync(category);

                categoriesByName[
                    createdCategory.Name.Trim()] =
                    createdCategory;
            }

            var updatedCount = 0;
            var createdCount = 0;

            // --------------------------------------------------------
            // PROCESS PRODUCTS
            // --------------------------------------------------------

            foreach (var row in rows)
            {
                var category =
                    categoriesByName[row.Category.Trim()];

                // ----------------------------------------------------
                // EXISTING PRODUCT
                // ----------------------------------------------------

                if (productsBySku.TryGetValue(
                        row.SKU,
                        out var existingProduct))
                {
                    existingProduct.SKU =
                        row.SKU.Trim();

                    existingProduct.Name =
                        row.Name;

                    existingProduct.Price =
                        row.Price;

                    existingProduct.StockQuantity =
                        row.StockQuantity;

                    existingProduct.CategoryId =
                        category.Id;

                    existingProduct.UpdatedAt =
                        DateTime.UtcNow;

                    await _productRepository.UpdateAsync(
                        existingProduct);

                    updatedCount++;
                }
                else
                {
                    // ------------------------------------------------
                    // NEW PRODUCT
                    // ------------------------------------------------

                    var newProduct =
                        new Product
                        {
                            SKU = row.SKU.Trim(),
                            Name = row.Name,
                            Description = string.Empty,
                            Price = row.Price,
                            CategoryId = category.Id,
                            StockQuantity = row.StockQuantity,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                    await _productRepository.AddAsync(
                        newProduct);

                    createdCount++;
                }
            }

            // --------------------------------------------------------
            // COMMIT
            // --------------------------------------------------------

            await _unitOfWork.CommitTransactionAsync();

            result.UpdatedCount =
                updatedCount;

            result.CreatedCount =
                createdCount;

            result.FailedCount = 0;

            result.Errors.Clear();

            ClearProductCache();

            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();

            throw;
        }
    }

    private static ProductDto MapToDto(
        Product product)
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

    private void ClearProductCache(
        int? productId = null)
    {
        _cache.Remove(ProductsCacheKey);

        if (productId.HasValue)
        {
            _cache.Remove(
                $"{ProductCacheKeyPrefix}{productId.Value}");
        }
    }
}