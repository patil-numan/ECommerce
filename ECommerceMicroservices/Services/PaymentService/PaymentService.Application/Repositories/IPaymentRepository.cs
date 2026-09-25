using PaymentService.Domain.Entities;

namespace PaymentService.Application.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> GetAllAsync();

    Task<Payment?> GetByIdAsync(int id);

    Task<Payment?> GetByOrderIdAsync(int orderId);

    Task AddAsync(Payment payment);

    Task UpdateAsync(Payment payment);

    Task DeleteAsync(Payment payment);
}