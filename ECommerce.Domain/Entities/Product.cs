namespace ECommerce.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    // Unique business identifier used for Excel bulk updates.
    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}