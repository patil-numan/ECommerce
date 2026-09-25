using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.Services;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll()
    {
        var products = await _productService.GetAllAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductDto dto)
    {
        try
        {
            var product = await _productService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductDto dto)
    {
        try
        {
            var updated = await _productService.UpdateAsync(id, dto);

            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPut("{id:int}/stock")]
    public async Task<IActionResult> ReduceStock(
        int id,
        [FromBody] ReduceStockRequest request)
    {
        try
        {
            var reduced = await _productService
                .ReduceStockAsync(id, request.Quantity);

            if (!reduced)
                return NotFound(new
                {
                    message = "Product not found."
                });

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _productService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpPost("bulk-import")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BulkProductUpdateResultDto>> BulkImport(
        IFormFile file,
        [FromServices] IBulkProductImportService bulkImportService)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "An Excel file is required."
            });
        }

        var extension = Path.GetExtension(file.FileName);

        if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Only .xlsx files are supported."
            });
        }

        try
        {
            await using var stream = file.OpenReadStream();

            var result = await bulkImportService.ImportAsync(stream);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "An error occurred while processing the Excel file."
                });
        }
    }
}

public class ReduceStockRequest
{
    public int Quantity { get; set; }
}