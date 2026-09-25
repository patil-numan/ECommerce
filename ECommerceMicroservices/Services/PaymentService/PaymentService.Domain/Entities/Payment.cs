namespace PaymentService.Domain.Entities;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}