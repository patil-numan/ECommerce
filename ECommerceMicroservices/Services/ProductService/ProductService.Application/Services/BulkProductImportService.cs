using ClosedXML.Excel;
using ProductService.Application.DTOs;
using ProductService.Application.Repositories;
using ProductService.Domain.Entities;

namespace ProductService.Application.Services;

public class BulkProductImportService : IBulkProductImportService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkProductImportService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BulkProductUpdateResultDto> ImportAsync(
        Stream excelStream)
    {
        var result = new BulkProductUpdateResultDto();

        using var workbook = new XLWorkbook(excelStream);

        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            throw new ArgumentException("The Excel file contains no worksheet.");

        var headers = ReadHeaders(worksheet);

        ValidateRequiredHeaders(headers);

        var rows = ReadAndValidateRows(
            worksheet,
            headers,
            result);

        if (result.FailedCount > 0)
            return result;

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var products = await _productRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();

            var productsBySku = products
                .ToDictionary(
                    p => p.SKU,
                    StringComparer.OrdinalIgnoreCase);

            var categoriesByName = categories
                .ToDictionary(
                    c => c.Name,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (productsBySku.TryGetValue(row.SKU, out var existingProduct))
                {
                    existingProduct.Name = row.Name;
                    existingProduct.Description = row.Description;
                    existingProduct.Price = row.Price;
                    existingProduct.StockQuantity = row.StockQuantity;
                    existingProduct.CategoryId =
                        await GetOrCreateCategoryIdAsync(
                            row.Category,
                            categoriesByName);

                    existingProduct.UpdatedAt = DateTime.UtcNow;

                    await _productRepository.UpdateAsync(existingProduct);

                    result.UpdatedCount++;
                }
                else
                {
                    var categoryId = await GetOrCreateCategoryIdAsync(
                        row.Category,
                        categoriesByName);

                    var product = new Product
                    {
                        SKU = row.SKU,
                        Name = row.Name,
                        Description = row.Description,
                        Price = row.Price,
                        StockQuantity = row.StockQuantity,
                        CategoryId = categoryId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _productRepository.AddAsync(product);

                    productsBySku[row.SKU] = product;

                    result.CreatedCount++;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task<int> GetOrCreateCategoryIdAsync(
        string categoryName,
        Dictionary<string, Category> categoriesByName)
    {
        if (categoriesByName.TryGetValue(
                categoryName,
                out var existingCategory))
        {
            return existingCategory.Id;
        }

        var category = new Category
        {
            Name = categoryName
        };

        await _categoryRepository.AddAsync(category);

        await _unitOfWork.SaveChangesAsync();

        categoriesByName[categoryName] = category;

        return category.Id;
    }

    private static Dictionary<string, int> ReadHeaders(
        IXLWorksheet worksheet)
    {
        var headers = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        var headerRow = worksheet.FirstRowUsed();

        if (headerRow == null)
        {
            throw new ArgumentException("The Excel file contains no header row.");
        }

            foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();

            if (!string.IsNullOrWhiteSpace(header))
            {
                headers[header] = cell.Address.ColumnNumber;
            }
        }

        return headers;
    }

    private static void ValidateRequiredHeaders(
        Dictionary<string, int> headers)
    {
        var requiredHeaders = new[]
        {
            "SKU",
            "Name",
            "Description",
            "Price",
            "StockQuantity",
            "Category"
        };

        var missingHeaders = requiredHeaders
            .Where(header => !headers.ContainsKey(header))
            .ToList();

        if (missingHeaders.Count > 0)
        {
            throw new ArgumentException(
                $"Missing required headers: {string.Join(", ", missingHeaders)}");
        }
    }

    private static List<BulkProductRowDto> ReadAndValidateRows(
        IXLWorksheet worksheet,
        Dictionary<string, int> headers,
        BulkProductUpdateResultDto result)
    {
        var rows = new List<BulkProductRowDto>();

        var firstUsedRow = worksheet.FirstRowUsed();
        var lastUsedRow = worksheet.LastRowUsed();

        if (firstUsedRow == null || lastUsedRow == null)
        {
            return rows;
        }

        var firstDataRow = firstUsedRow.RowNumber() + 1;
        var lastDataRow = lastUsedRow.RowNumber();

        var existingSkus = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        for (var rowNumber = firstDataRow;
             rowNumber <= lastDataRow;
             rowNumber++)
        {
            var row = worksheet.Row(rowNumber);

            if (row.IsEmpty())
                continue;

            var sku = row
                .Cell(headers["SKU"])
                .GetString()
                .Trim();

            var name = row
                .Cell(headers["Name"])
                .GetString()
                .Trim();

            var description = row
                .Cell(headers["Description"])
                .GetString()
                .Trim();

            var category = row
                .Cell(headers["Category"])
                .GetString()
                .Trim();

            var rowHasError = false;

            if (string.IsNullOrWhiteSpace(sku))
            {
                AddError(
                    result,
                    rowNumber,
                    sku,
                    "SKU is required.");

                rowHasError = true;
            }
            else if (!existingSkus.Add(sku))
            {
                AddError(
                    result,
                    rowNumber,
                    sku,
                    "Duplicate SKU found in the Excel file.");

                rowHasError = true;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                AddError(
                    result,
                    rowNumber,
                    sku,
                    "Name is required.");

                rowHasError = true;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                AddError(
                    result,
                    rowNumber,
                    sku,
                    "Description is required.");

                rowHasError = true;
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                AddError(
                    result,
                    rowNumber,
                    sku,
                    "Category is required.");

                rowHasError = true;
            }

            var price = ReadDecimal(
                row.Cell(headers["Price"]),
                rowNumber,
                sku,
                result);

            if (price == null)
                rowHasError = true;

            var stockQuantity = ReadInt(
                row.Cell(headers["StockQuantity"]),
                rowNumber,
                sku,
                result);

            if (stockQuantity == null)
                rowHasError = true;

            if (!rowHasError)
            {
                rows.Add(new BulkProductRowDto
                {
                    SKU = sku,
                    Name = name,
                    Description = description,
                    Price = price!.Value,
                    StockQuantity = stockQuantity!.Value,
                    Category = category
                });
            }
        }

        return rows;
    }

    private static decimal? ReadDecimal(
        IXLCell cell,
        int rowNumber,
        string sku,
        BulkProductUpdateResultDto result)
    {
        if (cell.TryGetValue<decimal>(out var numericValue))
        {
            if (numericValue <= 0)
            {
                AddError(
                    result,
                    rowNumber,
                    sku,
                    "Price must be greater than zero.");

                return null;
            }

            return numericValue;
        }

        var text = cell.GetString().Trim();

        if (!decimal.TryParse(text, out var parsedValue))
        {
            AddError(
                result,
                rowNumber,
                sku,
                "Invalid price.");

            return null;
        }

        if (parsedValue <= 0)
        {
            AddError(
                result,
                rowNumber,
                sku,
                "Price must be greater than zero.");

            return null;
        }

        return parsedValue;
    }

    private static int? ReadInt(
        IXLCell cell,
        int rowNumber,
        string sku,
        BulkProductUpdateResultDto result)
    {
        if (cell.TryGetValue<int>(out var numericValue))
        {
            if (numericValue < 0)
            {
                AddError(
                    result,
                    rowNumber,
                    sku,
                    "Stock quantity cannot be negative.");

                return null;
            }

            return numericValue;
        }

        var text = cell.GetString().Trim();

        if (!int.TryParse(text, out var parsedValue))
        {
            AddError(
                result,
                rowNumber,
                sku,
                "Invalid stock quantity.");

            return null;
        }

        if (parsedValue < 0)
        {
            AddError(
                result,
                rowNumber,
                sku,
                "Stock quantity cannot be negative.");

            return null;
        }

        return parsedValue;
    }

    private static void AddError(
        BulkProductUpdateResultDto result,
        int rowNumber,
        string sku,
        string error)
    {
        result.FailedCount++;

        result.Errors.Add(
            new BulkProductUpdateErrorDto
            {
                RowNumber = rowNumber,
                SKU = sku,
                Error = error
            });
    }
}