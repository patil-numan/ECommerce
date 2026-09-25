namespace OrderService.Application.Clients;

public interface IProductServiceClient
{
    Task<ProductInfo?> GetProductByIdAsync(int productId);

    Task<bool> ReduceStockAsync(int productId, int quantity);
}

public class ProductInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}