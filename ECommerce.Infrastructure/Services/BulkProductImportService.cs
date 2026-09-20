using ClosedXML.Excel;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services;

public class BulkProductImportService : IBulkProductImportService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IImportJobRepository _importJobRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<BulkProductImportService> _logger;

    private readonly int _batchSize;

    public BulkProductImportService(
        IFileStorageService fileStorageService,
        IImportJobRepository importJobRepository,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMemoryCache memoryCache,
        ILogger<BulkProductImportService> logger,
        IConfiguration configuration)
    {
        _fileStorageService = fileStorageService;
        _importJobRepository = importJobRepository;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _memoryCache = memoryCache;
        _logger = logger;

        _batchSize =
            configuration.GetValue<int>(
                "BulkImport:BatchSize");

        if (_batchSize <= 0)
        {
            _batchSize = 100;
        }
    }

    public async Task ProcessAsync(
        ImportJob importJob,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Starting import job {ImportJobId}.",
                importJob.Id);

            importJob.Status = "Processing";
            importJob.ErrorMessage = null;

            await _importJobRepository.UpdateAsync(importJob);

            await using var fileStream =
                await _fileStorageService.OpenFileAsync(
                    importJob.FilePath);

            using var workbook =
                new XLWorkbook(fileStream);

            var worksheet =
                GetWorksheet(workbook);

            var headers =
                ReadHeaders(worksheet);

            ValidateHeaders(headers);

            var validRows =
                ReadAndValidateRows(worksheet);

            importJob.TotalRows =
                validRows.Count;

            await _importJobRepository.UpdateAsync(
                importJob);

            cancellationToken.ThrowIfCancellationRequested();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var products =
                    await _productRepository.GetAllAsync();

                var categories =
                    await _categoryRepository.GetAllAsync();

                var productsBySku =
                    BuildProductLookup(products);

                var categoriesByName =
                    BuildCategoryLookup(categories);

                var newCategories =
                    FindNewCategories(
                        validRows,
                        categoriesByName);

                await CreateCategoriesAsync(
                    newCategories,
                    categoriesByName);

                // Save newly created categories first so their
                // database-generated IDs are available when
                // creating products.
                await _unitOfWork.SaveChangesAsync();

                for (
                    var batchStart = 0;
                    batchStart < validRows.Count;
                    batchStart += _batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch =
                        validRows
                            .Skip(batchStart)
                            .Take(_batchSize)
                            .ToList();

                    await ProcessBatchAsync(
                        batch,
                        productsBySku,
                        categoriesByName,
                        importJob,
                        cancellationToken);

                    await _unitOfWork.SaveChangesAsync();

                    await _importJobRepository.UpdateAsync(
                        importJob);

                    _logger.LogInformation(
                        "Import job {ImportJobId}: processed {ProcessedRows}/{TotalRows} rows.",
                        importJob.Id,
                        importJob.ProcessedRows,
                        importJob.TotalRows);
                }

                await _unitOfWork.CommitTransactionAsync();

                // The database now contains the imported products.
                // Remove the cached product list so the next API
                // request retrieves fresh data.
                ClearProductCache();

                importJob.Status = "Completed";
                importJob.CompletedAt = DateTime.UtcNow;

                await _importJobRepository.UpdateAsync(
                    importJob);

                _logger.LogInformation(
                    "Import job {ImportJobId} completed successfully.",
                    importJob.Id);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();

                throw;
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            importJob.Status = "Failed";
            importJob.ErrorMessage =
                "Import was cancelled.";

            await _importJobRepository.UpdateAsync(
                importJob);

            _logger.LogWarning(
                "Import job {ImportJobId} was cancelled.",
                importJob.Id);

            throw;
        }
        catch (Exception exception)
        {
            importJob.Status = "Failed";
            importJob.ErrorMessage =
                exception.Message;

            await _importJobRepository.UpdateAsync(
                importJob);

            _logger.LogError(
                exception,
                "Import job {ImportJobId} failed.",
                importJob.Id);

            throw;
        }
        finally
        {
            await CleanupImportFileAsync(importJob);
        }
    }

    // =========================================================
    // CACHE
    // =========================================================

    private void ClearProductCache()
    {
        _memoryCache.Remove("products");

        _logger.LogInformation(
            "Product list cache cleared after successful bulk import.");
    }

    // =========================================================
    // FILE CLEANUP
    // =========================================================

    private async Task CleanupImportFileAsync(
        ImportJob importJob)
    {
        if (string.IsNullOrWhiteSpace(
                importJob.FilePath))
        {
            return;
        }

        try
        {
            await _fileStorageService.DeleteFileAsync(
                importJob.FilePath);

            _logger.LogInformation(
                "Temporary import file deleted for job {ImportJobId}.",
                importJob.Id);
        }
        catch (Exception exception)
        {
            // File cleanup failure should not change the
            // already determined import result.
            _logger.LogWarning(
                exception,
                "Failed to delete temporary import file for job {ImportJobId}.",
                importJob.Id);
        }
    }

    // =========================================================
    // BATCH PROCESSING
    // =========================================================

    private async Task ProcessBatchAsync(
        List<BulkProductRowDto> batch,
        Dictionary<string, Product> productsBySku,
        Dictionary<string, Category> categoriesByName,
        ImportJob importJob,
        CancellationToken cancellationToken)
    {
        var productsToUpdate =
            new List<Product>();

        var productsToCreate =
            new List<Product>();

        foreach (var row in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

                importJob.UpdatedRows++;
            }
            else
            {
                var newProduct =
                    CreateProductFromRow(
                        row,
                        category);

                productsToCreate.Add(
                    newProduct);

                productsBySku[row.SKU] =
                    newProduct;

                importJob.CreatedRows++;
            }

            importJob.ProcessedRows++;
        }

        if (productsToUpdate.Count > 0)
        {
            await _productRepository.UpdateRangeAsync(
                productsToUpdate);
        }

        if (productsToCreate.Count > 0)
        {
            await _productRepository.AddRangeAsync(
                productsToCreate);
        }
    }

    // =========================================================
    // WORKSHEET
    // =========================================================

    private static IXLWorksheet GetWorksheet(
        XLWorkbook workbook)
    {
        if (workbook.Worksheets.Count == 0)
        {
            throw new InvalidOperationException(
                "The Excel file does not contain any worksheets.");
        }

        return workbook.Worksheets.First();
    }

    // =========================================================
    // HEADER READING
    // =========================================================

    private static List<string> ReadHeaders(
        IXLWorksheet worksheet)
    {
        var headers =
            new List<string>();

        var firstRow =
            worksheet.FirstRowUsed();

        if (firstRow is null)
        {
            return headers;
        }

        foreach (
            var cell in firstRow.CellsUsed())
        {
            headers.Add(
                cell.GetString().Trim());
        }

        return headers;
    }

    private static void ValidateHeaders(
        List<string> headers)
    {
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
            if (!headers.Any(
                    header =>
                        header.Equals(
                            requiredHeader,
                            StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Missing required Excel column: {requiredHeader}");
            }
        }
    }

    // =========================================================
    // LOOKUPS
    // =========================================================

    private static Dictionary<string, Product>
        BuildProductLookup(
            IEnumerable<Product> products)
    {
        return products.ToDictionary(
            product => product.SKU,
            product => product,
            StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, Category>
        BuildCategoryLookup(
            IEnumerable<Category> categories)
    {
        return categories.ToDictionary(
            category => category.Name,
            category => category,
            StringComparer.OrdinalIgnoreCase);
    }

    // =========================================================
    // READ + VALIDATE ROWS
    // =========================================================

    private static List<BulkProductRowDto>
        ReadAndValidateRows(
            IXLWorksheet worksheet)
    {
        var validRows =
            new List<BulkProductRowDto>();

        var seenSkus =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var firstRow =
            worksheet.FirstRowUsed();

        if (firstRow is null)
        {
            throw new InvalidOperationException(
                "The Excel file is empty.");
        }

        var lastRow =
            worksheet.LastRowUsed()?.RowNumber()
            ?? firstRow.RowNumber();

        for (
            var rowNumber = firstRow.RowNumber() + 1;
            rowNumber <= lastRow;
            rowNumber++)
        {
            var row =
                worksheet.Row(rowNumber);

            if (IsEmptyRow(row))
            {
                continue;
            }

            var validatedRow =
                CreateValidatedRow(
                    row,
                    rowNumber);

            if (!seenSkus.Add(
                    validatedRow.SKU))
            {
                throw new InvalidOperationException(
                    $"Duplicate SKU found at row {rowNumber}: {validatedRow.SKU}");
            }

            validRows.Add(
                validatedRow);
        }

        return validRows;
    }

    private static bool IsEmptyRow(
        IXLRow row)
    {
        return row.CellsUsed().All(
            cell =>
                string.IsNullOrWhiteSpace(
                    cell.GetString()));
    }

    private static BulkProductRowDto
        CreateValidatedRow(
            IXLRow row,
            int rowNumber)
    {
        var skuCell =
            row.Cell(1);

        var nameCell =
            row.Cell(2);

        var priceCell =
            row.Cell(3);

        var stockCell =
            row.Cell(4);

        var categoryCell =
            row.Cell(5);

        var sku =
            ValidateSku(
                skuCell.GetString(),
                rowNumber);

        var name =
            ValidateName(
                nameCell.GetString(),
                rowNumber);

        var price =
            ValidatePrice(
                priceCell.GetString(),
                rowNumber);

        var stockQuantity =
            ValidateStockQuantity(
                stockCell.GetString(),
                rowNumber);

        var category =
            ValidateCategory(
                categoryCell.GetString(),
                rowNumber);

        return new BulkProductRowDto
        {
            SKU = sku,
            Name = name,
            Price = price,
            StockQuantity = stockQuantity,
            Category = category
        };
    }

    // =========================================================
    // FIELD VALIDATION
    // =========================================================

    private static string ValidateSku(
        string value,
        int rowNumber)
    {
        var sku =
            value.Trim();

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new InvalidOperationException(
                $"SKU is required at row {rowNumber}.");
        }

        return sku;
    }

    private static string ValidateName(
        string value,
        int rowNumber)
    {
        var name =
            value.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                $"Name is required at row {rowNumber}.");
        }

        return name;
    }

    private static decimal ValidatePrice(
        string value,
        int rowNumber)
    {
        var priceText =
            value.Trim();

        if (!decimal.TryParse(
                priceText,
                out var price))
        {
            throw new InvalidOperationException(
                $"Invalid price at row {rowNumber}.");
        }

        if (price < 0)
        {
            throw new InvalidOperationException(
                $"Price cannot be negative at row {rowNumber}.");
        }

        return price;
    }

    private static int ValidateStockQuantity(
        string value,
        int rowNumber)
    {
        var stockText =
            value.Trim();

        if (!int.TryParse(
                stockText,
                out var stockQuantity))
        {
            throw new InvalidOperationException(
                $"Invalid stock quantity at row {rowNumber}.");
        }

        if (stockQuantity < 0)
        {
            throw new InvalidOperationException(
                $"Stock quantity cannot be negative at row {rowNumber}.");
        }

        return stockQuantity;
    }

    private static string ValidateCategory(
        string value,
        int rowNumber)
    {
        var category =
            value.Trim();

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException(
                $"Category is required at row {rowNumber}.");
        }

        return category;
    }

    // =========================================================
    // CATEGORY PROCESSING
    // =========================================================

    private static List<string> FindNewCategories(
        List<BulkProductRowDto> rows,
        Dictionary<string, Category> categoriesByName)
    {
        return rows
            .Select(row => row.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(
                category =>
                    !categoriesByName.ContainsKey(category))
            .ToList();
    }

    private async Task CreateCategoriesAsync(
        List<string> newCategories,
        Dictionary<string, Category> categoriesByName)
    {
        foreach (var categoryName in newCategories)
        {
            var category =
                new Category
                {
                    Name = categoryName
                };

            await _categoryRepository.AddAsync(
                category);

            categoriesByName[categoryName] =
                category;
        }
    }

    // =========================================================
    // PRODUCT PROCESSING
    // =========================================================

    private static void PrepareProductForUpdate(
        Product product,
        BulkProductRowDto row,
        Category category)
    {
        product.Name =
            row.Name;

        product.Price =
            row.Price;

        product.StockQuantity =
            row.StockQuantity;

        product.CategoryId =
            category.Id;
    }

    private static Product CreateProductFromRow(
        BulkProductRowDto row,
        Category category)
    {
        return new Product
        {
            SKU = row.SKU,
            Name = row.Name,
            Price = row.Price,
            StockQuantity = row.StockQuantity,
            CategoryId = category.Id
        };
    }
}