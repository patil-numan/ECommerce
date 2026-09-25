using PaymentService.Application.DTOs;

namespace PaymentService.Application.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(CreatePaymentDto dto);

    Task<PaymentDto?> GetByIdAsync(int id);

    Task<PaymentDto?> GetByOrderIdAsync(int orderId);

    Task<List<PaymentDto>> GetAllAsync();

    Task RefundAsync(int id);
}