namespace ProductService.Application.DTOs;

public class BulkProductRowDto
{
    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string Category { get; set; } = string.Empty;
}