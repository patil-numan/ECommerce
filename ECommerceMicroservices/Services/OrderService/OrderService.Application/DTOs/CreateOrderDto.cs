namespace OrderService.Application.DTOs;

public class CreateOrderDto
{
    public int UserId { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public List<CreateOrderItemDto> Items { get; set; } = new();
}