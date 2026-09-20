using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IBulkProductImportService
{
    Task ProcessAsync(
        ImportJob importJob,
        CancellationToken cancellationToken);
}