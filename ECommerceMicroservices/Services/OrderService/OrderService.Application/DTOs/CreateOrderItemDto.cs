namespace OrderService.Application.DTOs;

public class CreateOrderItemDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}