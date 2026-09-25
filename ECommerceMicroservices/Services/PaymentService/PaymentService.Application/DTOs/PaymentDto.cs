namespace PaymentService.Application.DTOs;

public class PaymentDto
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}