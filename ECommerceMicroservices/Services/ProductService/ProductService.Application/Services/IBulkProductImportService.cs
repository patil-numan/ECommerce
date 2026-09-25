using ProductService.Application.DTOs;

namespace ProductService.Application.Services;

public interface IBulkProductImportService
{
    Task<BulkProductUpdateResultDto> ImportAsync(Stream excelStream);
}