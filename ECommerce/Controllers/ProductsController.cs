using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IImportJobRepository _importJobRepository;
    private readonly IImportJobQueue _importJobQueue;

    public ProductsController(
        IProductService productService,
        IFileStorageService fileStorageService,
        IImportJobRepository importJobRepository,
        IImportJobQueue importJobQueue)
    {
        _productService = productService;
        _fileStorageService = fileStorageService;
        _importJobRepository = importJobRepository;
        _importJobQueue = importJobQueue;
    }

    // =========================================================
    // CUSTOMER / GENERAL READ OPERATIONS
    // =========================================================

    // GET: api/Products
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllProducts()
    {
        var products =
            await _productService.GetAllAsync();

        return Ok(products);
    }

    // GET: api/Products/5
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product =
            await _productService.GetByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    // =========================================================
    // ADMIN OPERATIONS
    // =========================================================

    // POST: api/Products
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct(
        CreateProductDto dto)
    {
        try
        {
            var product =
                await _productService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = product.Id },
                product);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // PUT: api/Products/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(
        int id,
        UpdateProductDto dto)
    {
        try
        {
            var product =
                await _productService.UpdateAsync(
                    id,
                    dto);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // DELETE: api/Products/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var deleted =
            await _productService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    // =========================================================
    // BULK PRODUCT UPDATE FROM EXCEL - ADMIN
    // =========================================================

    // POST: api/Products/bulk-update
    [HttpPost("bulk-update")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkUpdateProducts(
    IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please upload an Excel file."
            });
        }

        var extension =
            Path.GetExtension(file.FileName);

        if (!extension.Equals(
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Only .xlsx Excel files are supported."
            });
        }

        try
        {
            await using var stream =
                file.OpenReadStream();

            var filePath =
                await _fileStorageService.SaveFileAsync(
                    stream,
                    file.FileName);

            var importJob = new ImportJob
            {
                FileName = file.FileName,
                FilePath = filePath,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _importJobRepository.AddAsync(importJob);

            await _importJobQueue.QueueAsync(importJob.Id);

            return Accepted(new
            {
                message =
                    "Excel file uploaded successfully. Import has been queued.",
                jobId = importJob.Id
            });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "An error occurred while creating the import job."
                });
        }
    }

    [HttpGet("import-jobs/{jobId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetImportJobStatus(
    int jobId)
    {
        var importJob =
            await _importJobRepository.GetByIdAsync(jobId);

        if (importJob is null)
        {
            return NotFound(new
            {
                message = "Import job not found."
            });
        }

        var result =
            new ImportJobStatusDto
            {
                JobId = importJob.Id,
                FileName = importJob.FileName,
                Status = importJob.Status,
                TotalRows = importJob.TotalRows,
                ProcessedRows = importJob.ProcessedRows,
                UpdatedRows = importJob.UpdatedRows,
                CreatedRows = importJob.CreatedRows,
                FailedRows = importJob.FailedRows,
                ErrorMessage = importJob.ErrorMessage,
                CreatedAt = importJob.CreatedAt,
                CompletedAt = importJob.CompletedAt
            };

        return Ok(result);
    }
}