using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class ImportJobRepository : IImportJobRepository
{
    private readonly ApplicationDbContext _context;

    public ImportJobRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ImportJob> AddAsync(
        ImportJob importJob)
    {
        await _context.ImportJobs.AddAsync(
            importJob);

        await _context.SaveChangesAsync();

        return importJob;
    }

    public async Task<ImportJob?> GetByIdAsync(
        int id)
    {
        return await _context.ImportJobs
            .FirstOrDefaultAsync(
                job => job.Id == id);
    }

    public async Task UpdateAsync(
        ImportJob importJob)
    {
        _context.ImportJobs.Update(
            importJob);

        await _context.SaveChangesAsync();
    }
}