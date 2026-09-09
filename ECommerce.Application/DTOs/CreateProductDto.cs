namespace ECommerce.Application.DTOs;

public class CreateProductDto
{
    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int CategoryId { get; set; }

    public int StockQuantity { get; set; }
}