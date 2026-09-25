namespace OrderService.Application.Clients;

public interface IPaymentServiceClient
{
    Task<PaymentResult?> ProcessPaymentAsync(
        int orderId,
        int userId,
        decimal amount,
        string paymentMethod);
}

public class PaymentResult
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;
}