using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IImportJobRepository
{
    Task<ImportJob> AddAsync(ImportJob importJob);

    Task<ImportJob?> GetByIdAsync(int id);

    Task UpdateAsync(ImportJob importJob);
}