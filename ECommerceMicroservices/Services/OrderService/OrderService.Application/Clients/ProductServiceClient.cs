using System.Net;
using System.Net.Http.Json;
using OrderService.Application.Clients;

namespace OrderService.Infrastructure.Clients;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;

    public ProductServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductInfo?> GetProductByIdAsync(int productId)
    {
        var response = await _httpClient.GetAsync(
            $"api/Products/{productId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ProductInfo>();
    }

    public async Task<bool> ReduceStockAsync(
        int productId,
        int quantity)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/Products/{productId}/stock",
            new
            {
                quantity
            });

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        return true;
    }
}