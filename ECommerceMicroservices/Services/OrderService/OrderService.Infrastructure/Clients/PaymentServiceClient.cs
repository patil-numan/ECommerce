using System.Net;
using System.Net.Http.Json;
using OrderService.Application.Clients;

namespace OrderService.Infrastructure.Clients;

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;

    public PaymentServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentResult?> ProcessPaymentAsync(
        int orderId,
        int userId,
        decimal amount,
        string paymentMethod)
    {
        var request = new
        {
            orderId,
            userId,
            amount,
            paymentMethod
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/Payments",
            request);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<PaymentResult>();
    }
}