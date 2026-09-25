using PaymentService.Application.DTOs;
using PaymentService.Application.Repositories;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
    {
        if (dto.OrderId <= 0)
            throw new ArgumentException(
                "OrderId must be greater than zero.");

        if (dto.UserId <= 0)
            throw new ArgumentException(
                "UserId must be greater than zero.");

        if (dto.Amount <= 0)
            throw new ArgumentException(
                "Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
            throw new ArgumentException(
                "Payment method is required.");

        var existingPayment =
            await _paymentRepository.GetByOrderIdAsync(dto.OrderId);

        if (existingPayment != null)
        {
            throw new InvalidOperationException(
                "A payment already exists for this order.");
        }

        var payment = new Payment
        {
            OrderId = dto.OrderId,
            UserId = dto.UserId,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            Status = PaymentStatus.Pending,
            TransactionId = GenerateTransactionId(),
            PaymentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Simulate payment processing.
        payment.Status = PaymentStatus.Completed;

        await _paymentRepository.AddAsync(payment);

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(payment);
    }

    public async Task<PaymentDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException(
                "Payment ID must be greater than zero.");

        var payment =
            await _paymentRepository.GetByIdAsync(id);

        return payment == null
            ? null
            : MapToDto(payment);
    }

    public async Task<PaymentDto?> GetByOrderIdAsync(int orderId)
    {
        if (orderId <= 0)
            throw new ArgumentException(
                "Order ID must be greater than zero.");

        var payment =
            await _paymentRepository.GetByOrderIdAsync(orderId);

        return payment == null
            ? null
            : MapToDto(payment);
    }

    public async Task<List<PaymentDto>> GetAllAsync()
    {
        var payments =
            await _paymentRepository.GetAllAsync();

        return payments
            .Select(MapToDto)
            .ToList();
    }

    public async Task RefundAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException(
                "Payment ID must be greater than zero.");

        var payment =
            await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
        {
            throw new KeyNotFoundException(
                "Payment not found.");
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException(
                "Only completed payments can be refunded.");
        }

        payment.Status = PaymentStatus.Refunded;
        payment.UpdatedAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment);

        await _unitOfWork.SaveChangesAsync();
    }

    private static string GenerateTransactionId()
    {
        return $"TXN-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            PaymentMethod = payment.PaymentMethod,
            TransactionId = payment.TransactionId,
            PaymentDate = payment.PaymentDate,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}