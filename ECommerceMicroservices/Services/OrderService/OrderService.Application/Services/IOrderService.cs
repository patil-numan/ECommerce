using OrderService.Application.DTOs;

namespace OrderService.Application.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderDto dto);

    Task<OrderDto?> GetByIdAsync(int id);

    Task<List<OrderDto>> GetAllAsync();

    Task<List<OrderDto>> GetByUserIdAsync(int userId);

    Task CancelAsync(int id);
}