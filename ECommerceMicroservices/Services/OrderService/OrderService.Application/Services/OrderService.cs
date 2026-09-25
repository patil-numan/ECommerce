using OrderService.Application.Clients;
using OrderService.Application.DTOs;
using OrderService.Application.Repositories;
using OrderService.Domain.Entities;

namespace OrderService.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductServiceClient _productServiceClient;
    private readonly IPaymentServiceClient _paymentServiceClient;

    public OrderService(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IProductServiceClient productServiceClient,
        IPaymentServiceClient paymentServiceClient)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _productServiceClient = productServiceClient;
        _paymentServiceClient = paymentServiceClient;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        if (dto.UserId <= 0)
            throw new ArgumentException(
                "UserId must be greater than zero.");

        if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
            throw new ArgumentException(
                "Payment method is required.");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new ArgumentException(
                "An order must contain at least one item.");

        var order = new Order
        {
            UserId = dto.UserId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        // First validate all products and stock.
        foreach (var item in dto.Items)
        {
            if (item.ProductId <= 0)
                throw new ArgumentException(
                    "ProductId must be greater than zero.");

            if (item.Quantity <= 0)
                throw new ArgumentException(
                    "Quantity must be greater than zero.");

            var product = await _productServiceClient
                .GetProductByIdAsync(item.ProductId);

            if (product == null)
            {
                throw new KeyNotFoundException(
                    $"Product with ID {item.ProductId} was not found.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for product '{product.Name}'.");
            }

            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            });
        }

        // Calculate total order amount.
        order.TotalAmount = order.OrderItems.Sum(
            item => item.UnitPrice * item.Quantity);

        // Reduce product stock through Product Service.
        foreach (var item in order.OrderItems)
        {
            var stockReduced = await _productServiceClient
                .ReduceStockAsync(
                    item.ProductId,
                    item.Quantity);

            if (!stockReduced)
            {
                throw new InvalidOperationException(
                    $"Unable to reserve stock for product '{item.ProductName}'.");
            }
        }

        // Save the order first.
        await _orderRepository.AddAsync(order);

        await _unitOfWork.SaveChangesAsync();

        // Process payment through Payment Service.
        var payment = await _paymentServiceClient.ProcessPaymentAsync(
            order.Id,
            order.UserId,
            order.TotalAmount,
            dto.PaymentMethod);

        if (payment == null)
        {
            throw new InvalidOperationException(
                "Payment could not be processed.");
        }

        if (payment.Status != "Completed")
        {
            throw new InvalidOperationException(
                "Payment was not completed.");
        }

        // Payment completed successfully.
        order.Status = OrderStatus.Paid;

        await _orderRepository.UpdateAsync(order);

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException(
                "Order ID must be greater than zero.");

        var order = await _orderRepository.GetByIdAsync(id);

        return order == null ? null : MapToDto(order);
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        var orders = await _orderRepository.GetAllAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetByUserIdAsync(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException(
                "User ID must be greater than zero.");

        var orders = await _orderRepository.GetByUserIdAsync(userId);

        return orders.Select(MapToDto).ToList();
    }

    public async Task CancelAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException(
                "Order ID must be greater than zero.");

        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            throw new KeyNotFoundException("Order not found.");

        if (order.Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException(
                "A delivered order cannot be cancelled.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "The order is already cancelled.");
        }

        order.Status = OrderStatus.Cancelled;

        await _orderRepository.UpdateAsync(order);

        await _unitOfWork.SaveChangesAsync();
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            Items = order.OrderItems.Select(item => new OrderItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                TotalPrice = item.UnitPrice * item.Quantity
            }).ToList()
        };
    }
}