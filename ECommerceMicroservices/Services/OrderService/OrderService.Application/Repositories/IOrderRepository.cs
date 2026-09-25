using OrderService.Domain.Entities;

namespace OrderService.Application.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();

    Task<Order?> GetByIdAsync(int id);

    Task<List<Order>> GetByUserIdAsync(int userId);

    Task AddAsync(Order order);

    Task UpdateAsync(Order order);

    Task DeleteAsync(Order order);
}