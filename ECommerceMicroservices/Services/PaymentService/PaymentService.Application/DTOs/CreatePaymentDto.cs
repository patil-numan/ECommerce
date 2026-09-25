namespace PaymentService.Application.DTOs;

public class CreatePaymentDto
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
}