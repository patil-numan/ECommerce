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

    private const string ProductsCacheKey =
        "products";

    private const string ProductCacheKeyPrefix =
        "product_";

    private static readonly string[] RequiredHeaders =
    {
        "SKU",
        "Name",
        "Price",
        "StockQuantity",
        "Category"
    };

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

    // ============================================================
    // GET ALL PRODUCTS
    // ============================================================

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var cachedProducts =
            _cache.Get(ProductsCacheKey)
                as IEnumerable<ProductDto>;

        if (cachedProducts is not null)
        {
            return cachedProducts;
        }

        var products =
            await _productRepository.GetAllAsync();

        var productDtos =
            products
                .Select(MapToDto)
                .ToList();

        _cache.Set(
            ProductsCacheKey,
            productDtos,
            TimeSpan.FromMinutes(5));

        return productDtos;
    }

    // ============================================================
    // GET PRODUCT BY ID
    // ============================================================

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var cacheKey =
            $"{ProductCacheKeyPrefix}{id}";

        var cachedProduct =
            _cache.Get(cacheKey)
                as ProductDto;

        if (cachedProduct is not null)
        {
            return cachedProduct;
        }

        var product =
            await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return null;
        }

        var productDto =
            MapToDto(product);

        _cache.Set(
            cacheKey,
            productDto,
            TimeSpan.FromMinutes(5));

        return productDto;
    }

    // ============================================================
    // CREATE PRODUCT
    // ============================================================

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

        var duplicateSku =
            products.Any(product =>
                string.Equals(
                    product.SKU,
                    sku,
                    StringComparison.OrdinalIgnoreCase));

        if (duplicateSku)
        {
            throw new ArgumentException(
                "A product with this SKU already exists.");
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
            await _productRepository.AddAsync(
                product);

        // Persist the new product to the database.
        await _unitOfWork.SaveChangesAsync();

        ClearProductCache();

        return MapToDto(createdProduct);
    }

    // ============================================================
    // UPDATE PRODUCT
    // ============================================================

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

        if (product is null)
        {
            return null;
        }

        var sku =
            dto.SKU.Trim();

        var products =
            await _productRepository.GetAllAsync();

        var duplicateSku =
            products.Any(existingProduct =>
                existingProduct.Id != id &&
                string.Equals(
                    existingProduct.SKU,
                    sku,
                    StringComparison.OrdinalIgnoreCase));

        if (duplicateSku)
        {
            throw new ArgumentException(
                "A product with this SKU already exists.");
        }

        product.SKU = sku;
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(
            product);

        // Persist the product changes to the database.
        await _unitOfWork.SaveChangesAsync();

        ClearProductCache(id);

        return MapToDto(product);
    }

    // ============================================================
    // DELETE PRODUCT
    // ============================================================

    public async Task<bool> DeleteAsync(int id)
    {
        var product =
            await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return false;
        }

        await _productRepository.DeleteAsync(id);

        // Persist the deletion to the database.
        await _unitOfWork.SaveChangesAsync();

        ClearProductCache(id);

        return true;
    }

    // ============================================================
    // BULK PRODUCT UPDATE / IMPORT
    // ============================================================

    public async Task<BulkProductUpdateResultDto>
        BulkUpdateAsync(Stream excelStream)
    {
        using var workbook =
            new XLWorkbook(excelStream);

        var worksheet =
            GetWorksheet(workbook);

        if (worksheet is null)
        {
            return CreateBulkErrorResult(
                "The Excel file does not contain a worksheet.");
        }

        var lastRowUsed =
            worksheet.LastRowUsed();

        var lastColumnUsed =
            worksheet.LastColumnUsed();

        if (lastRowUsed is null ||
            lastColumnUsed is null)
        {
            return CreateBulkErrorResult(
                "The Excel file is empty.");
        }

        if (lastColumnUsed.ColumnNumber() < 5)
        {
            return CreateBulkErrorResult(
                "The Excel file must contain the required columns.");
        }

        // --------------------------------------------------------
        // 1. Read headers
        // --------------------------------------------------------

        var headers =
            ReadHeaders(
                worksheet,
                lastColumnUsed.ColumnNumber());

        // --------------------------------------------------------
        // 2. Validate headers
        // --------------------------------------------------------

        var headerError =
            ValidateHeaders(headers);

        if (headerError is not null)
        {
            return CreateBulkErrorResult(
                headerError);
        }

        // --------------------------------------------------------
        // 3. Load existing database data
        // --------------------------------------------------------

        var products =
            await _productRepository.GetAllAsync();

        var categories =
            await _categoryRepository.GetAllAsync();

        var productsBySku =
            BuildProductLookup(products);

        var categoriesByName =
            BuildCategoryLookup(categories);

        // --------------------------------------------------------
        // 4. Read and validate Excel rows
        // --------------------------------------------------------

        var validationResult =
            ReadAndValidateRows(
                worksheet,
                lastRowUsed.RowNumber(),
                headers);

        if (validationResult.Errors.Count > 0)
        {
            validationResult.Result.FailedCount =
                validationResult.Errors.Count;

            validationResult.Result.Errors =
                validationResult.Errors;

            return validationResult.Result;
        }

        var validRows =
            validationResult.Rows;

        // --------------------------------------------------------
        // 5. Find categories that need to be created
        // --------------------------------------------------------

        var newCategories =
            FindNewCategories(
                validRows,
                categoriesByName);

        // --------------------------------------------------------
        // 6. Begin transaction
        // --------------------------------------------------------

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // ----------------------------------------------------
            // 7. Create missing categories
            // ----------------------------------------------------

            await CreateCategoriesAsync(
                newCategories,
                categoriesByName);

            // ----------------------------------------------------
            // 8. Process products
            // ----------------------------------------------------

            var counts =
                await ProcessBulkUpdateAsync(
                    validRows,
                    productsBySku,
                    categoriesByName);

            // ----------------------------------------------------
            // 9. Commit transaction
            // ----------------------------------------------------

            await _unitOfWork.CommitTransactionAsync();

            var result =
                new BulkProductUpdateResultDto
                {
                    UpdatedCount =
                        counts.UpdatedCount,

                    CreatedCount =
                        counts.CreatedCount,

                    FailedCount = 0
                };

            ClearProductCache();

            return result;
        }
        catch
        {
            // ----------------------------------------------------
            // Rollback if anything fails
            // ----------------------------------------------------

            await _unitOfWork.RollbackTransactionAsync();

            throw;
        }
    }

    // ============================================================
    // GET WORKSHEET
    // ============================================================

    private static IXLWorksheet? GetWorksheet(
        XLWorkbook workbook)
    {
        return workbook.Worksheets.FirstOrDefault();
    }

    // ============================================================
    // READ HEADERS
    // ============================================================

    private static Dictionary<string, int> ReadHeaders(
        IXLWorksheet worksheet,
        int lastColumn)
    {
        var headers =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        for (var column = 1;
             column <= lastColumn;
             column++)
        {
            var header =
                worksheet.Cell(1, column)
                    .GetString()
                    .Trim();

            if (!string.IsNullOrWhiteSpace(header))
            {
                headers[header] = column;
            }
        }

        return headers;
    }

    // ============================================================
    // VALIDATE HEADERS
    // ============================================================

    private static string? ValidateHeaders(
        Dictionary<string, int> headers)
    {
        foreach (var requiredHeader in RequiredHeaders)
        {
            if (!headers.ContainsKey(requiredHeader))
            {
                return
                    $"Required column '{requiredHeader}' is missing.";
            }
        }

        return null;
    }

    // ============================================================
    // BUILD PRODUCT LOOKUP
    // ============================================================

    private static Dictionary<string, Product>
        BuildProductLookup(
            IEnumerable<Product> products)
    {
        return products
            .Where(product =>
                !string.IsNullOrWhiteSpace(product.SKU))
            .GroupBy(
                product => product.SKU.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    // ============================================================
    // BUILD CATEGORY LOOKUP
    // ============================================================

    private static Dictionary<string, Category>
        BuildCategoryLookup(
            IEnumerable<Category> categories)
    {
        return categories
            .Where(category =>
                !string.IsNullOrWhiteSpace(category.Name))
            .GroupBy(
                category => category.Name.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    // ============================================================
    // READ AND VALIDATE ROWS
    // ============================================================

    private static (
        List<BulkProductRowDto> Rows,
        List<BulkProductUpdateErrorDto> Errors,
        BulkProductUpdateResultDto Result)
        ReadAndValidateRows(
            IXLWorksheet worksheet,
            int lastRow,
            Dictionary<string, int> headers)
    {
        var validRows =
            new List<BulkProductRowDto>();

        var errors =
            new List<BulkProductUpdateErrorDto>();

        var excelSkus =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var result =
            new BulkProductUpdateResultDto();

        for (var rowNumber = 2;
             rowNumber <= lastRow;
             rowNumber++)
        {
            var sku =
                worksheet.Cell(
                    rowNumber,
                    headers["SKU"])
                .GetString()
                .Trim();

            var name =
                worksheet.Cell(
                    rowNumber,
                    headers["Name"])
                .GetString()
                .Trim();

            var category =
                worksheet.Cell(
                    rowNumber,
                    headers["Category"])
                .GetString()
                .Trim();

            var priceCell =
                worksheet.Cell(
                    rowNumber,
                    headers["Price"]);

            var stockCell =
                worksheet.Cell(
                    rowNumber,
                    headers["StockQuantity"]);

            // ----------------------------------------------------
            // Skip completely empty rows
            // ----------------------------------------------------

            if (IsEmptyRow(
                    sku,
                    name,
                    category,
                    priceCell,
                    stockCell))
            {
                continue;
            }

            var row =
                CreateValidatedRow(
                    rowNumber,
                    sku,
                    name,
                    category,
                    priceCell,
                    stockCell,
                    excelSkus,
                    errors);

            if (row is not null)
            {
                validRows.Add(row);
            }
        }

        return (
            validRows,
            errors,
            result);
    }

    // ============================================================
    // CHECK EMPTY ROW
    // ============================================================

    private static bool IsEmptyRow(
        string sku,
        string name,
        string category,
        IXLCell priceCell,
        IXLCell stockCell)
    {
        return
            string.IsNullOrWhiteSpace(sku) &&
            string.IsNullOrWhiteSpace(name) &&
            string.IsNullOrWhiteSpace(category) &&
            priceCell.IsEmpty() &&
            stockCell.IsEmpty();
    }

    // ============================================================
    // CREATE VALIDATED ROW
    // ============================================================

    private static BulkProductRowDto? CreateValidatedRow(
        int rowNumber,
        string sku,
        string name,
        string category,
        IXLCell priceCell,
        IXLCell stockCell,
        HashSet<string> excelSkus,
        List<BulkProductUpdateErrorDto> errors)
    {
        var rowErrors =
            new List<string>();

        ValidateSku(
            sku,
            excelSkus,
            rowErrors);

        ValidateName(
            name,
            rowErrors);

        var price =
            ValidatePrice(
                priceCell,
                rowErrors);

        var stockQuantity =
            ValidateStockQuantity(
                stockCell,
                rowErrors);

        ValidateCategory(
            category,
            rowErrors);

        if (rowErrors.Count > 0)
        {
            errors.Add(
                new BulkProductUpdateErrorDto
                {
                    RowNumber = rowNumber,
                    SKU = sku,
                    Error = string.Join(
                        "; ",
                        rowErrors)
                });

            return null;
        }

        return new BulkProductRowDto
        {
            SKU = sku,
            Name = name,
            Price = price,
            StockQuantity = stockQuantity,
            Category = category
        };
    }

    // ============================================================
    // VALIDATE SKU
    // ============================================================

    private static void ValidateSku(
        string sku,
        HashSet<string> excelSkus,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            errors.Add(
                "SKU is required.");

            return;
        }

        if (!excelSkus.Add(sku))
        {
            errors.Add(
                "Duplicate SKU found in Excel file.");
        }
    }

    // ============================================================
    // VALIDATE NAME
    // ============================================================

    private static void ValidateName(
        string name,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(
                "Name is required.");
        }
    }

    // ============================================================
    // VALIDATE PRICE
    // ============================================================

    private static decimal ValidatePrice(
        IXLCell priceCell,
        List<string> errors)
    {
        var priceText =
            priceCell.GetString();

        if (!decimal.TryParse(
                priceText,
                out var price))
        {
            errors.Add(
                "Price must be a valid decimal number.");

            return 0;
        }

        if (price < 0)
        {
            errors.Add(
                "Price cannot be negative.");
        }

        return price;
    }

    // ============================================================
    // VALIDATE STOCK QUANTITY
    // ============================================================

    private static int ValidateStockQuantity(
        IXLCell stockCell,
        List<string> errors)
    {
        var stockText =
            stockCell.GetString();

        if (!int.TryParse(
                stockText,
                out var stockQuantity))
        {
            errors.Add(
                "StockQuantity must be a valid integer.");

            return 0;
        }

        if (stockQuantity < 0)
        {
            errors.Add(
                "StockQuantity cannot be negative.");
        }

        return stockQuantity;
    }

    // ============================================================
    // VALIDATE CATEGORY
    // ============================================================

    private static void ValidateCategory(
        string category,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            errors.Add(
                "Category is required.");
        }
    }

    // ============================================================
    // FIND NEW CATEGORIES
    // ============================================================

    private static Dictionary<string, Category>
        FindNewCategories(
            IEnumerable<BulkProductRowDto> rows,
            Dictionary<string, Category> categoriesByName)
    {
        var newCategories =
            new Dictionary<string, Category>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (categoriesByName.ContainsKey(
                    row.Category))
            {
                continue;
            }

            if (newCategories.ContainsKey(
                    row.Category))
            {
                continue;
            }

            newCategories[row.Category] =
                new Category
                {
                    Name = row.Category,
                    Description = string.Empty
                };
        }

        return newCategories;
    }

    // ============================================================
    // CREATE CATEGORIES
    // ============================================================

    private async Task CreateCategoriesAsync(
        Dictionary<string, Category> newCategories,
        Dictionary<string, Category> categoriesByName)
    {
        foreach (var categoryEntry in newCategories)
        {
            var category =
                categoryEntry.Value;

            var createdCategory =
                await _categoryRepository.AddAsync(
                    category);

            categoriesByName[
                categoryEntry.Key] =
                createdCategory;
        }
    }

    // ============================================================
    // PROCESS BULK UPDATE
    // ============================================================

    private async Task<(
        int UpdatedCount,
        int CreatedCount)>
        ProcessBulkUpdateAsync(
            List<BulkProductRowDto> validRows,
            Dictionary<string, Product> productsBySku,
            Dictionary<string, Category> categoriesByName)
    {
        var productsToUpdate =
            new List<Product>();

        var productsToCreate =
            new List<Product>();

        foreach (var row in validRows)
        {
            var category =
                categoriesByName[row.Category];

            if (productsBySku.TryGetValue(
                    row.SKU,
                    out var existingProduct))
            {
                PrepareProductForUpdate(
                    existingProduct,
                    row,
                    category);

                productsToUpdate.Add(
                    existingProduct);
            }
            else
            {
                var newProduct =
                    CreateProductFromRow(
                        row,
                        category);

                productsToCreate.Add(
                    newProduct);
            }
        }

        // --------------------------------------------------------
        // IMPORTANT:
        // Use UpdateRangeAsync for bulk updates.
        // This preserves the existing repository contract
        // and allows transaction failure tests to simulate
        // database failures correctly.
        // --------------------------------------------------------

        if (productsToUpdate.Count > 0)
        {
            await _productRepository.UpdateRangeAsync(
                productsToUpdate);
        }

        foreach (var product in productsToCreate)
        {
            await _productRepository.AddAsync(
                product);
        }

        return (
            productsToUpdate.Count,
            productsToCreate.Count);
    }

    // ============================================================
    // PREPARE PRODUCT FOR UPDATE
    // ============================================================

    private static void PrepareProductForUpdate(
        Product product,
        BulkProductRowDto row,
        Category category)
    {
        product.SKU =
            row.SKU;

        product.Name =
            row.Name;

        product.Price =
            row.Price;

        product.StockQuantity =
            row.StockQuantity;

        product.CategoryId =
            category.Id;

        product.UpdatedAt =
            DateTime.UtcNow;
    }

    // ============================================================
    // CREATE PRODUCT FROM EXCEL ROW
    // ============================================================

    private static Product CreateProductFromRow(
        BulkProductRowDto row,
        Category category)
    {
        var now =
            DateTime.UtcNow;

        return new Product
        {
            SKU = row.SKU,
            Name = row.Name,
            Description = string.Empty,
            Price = row.Price,
            CategoryId = category.Id,
            StockQuantity = row.StockQuantity,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    // ============================================================
    // CREATE BULK ERROR RESULT
    // ============================================================

    private static BulkProductUpdateResultDto
        CreateBulkErrorResult(
            string error)
    {
        return new BulkProductUpdateResultDto
        {
            FailedCount = 1,
            Errors =
            {
                new BulkProductUpdateErrorDto
                {
                    RowNumber = 0,
                    SKU = string.Empty,
                    Error = error
                }
            }
        };
    }

    // ============================================================
    // MAP PRODUCT TO DTO
    // ============================================================

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

    // ============================================================
    // CLEAR ALL PRODUCT CACHE
    // ============================================================

    private void ClearProductCache()
    {
        _cache.Remove(
            ProductsCacheKey);
    }

    // ============================================================
    // CLEAR SINGLE PRODUCT CACHE
    // ============================================================

    private void ClearProductCache(int id)
    {
        _cache.Remove(
            ProductsCacheKey);

        _cache.Remove(
            $"{ProductCacheKeyPrefix}{id}");
    }
}