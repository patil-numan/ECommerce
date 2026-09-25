using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs;
using PaymentService.Application.Services;

namespace PaymentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(
        CreatePaymentDto dto)
    {
        try
        {
            var payment =
                await _paymentService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = payment.Id },
                payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetAll()
    {
        var payments =
            await _paymentService.GetAllAsync();

        return Ok(payments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> GetById(
        int id)
    {
        try
        {
            var payment =
                await _paymentService.GetByIdAsync(id);

            if (payment == null)
            {
                return NotFound(new
                {
                    message = "Payment not found."
                });
            }

            return Ok(payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<PaymentDto>> GetByOrderId(
        int orderId)
    {
        try
        {
            var payment =
                await _paymentService.GetByOrderIdAsync(orderId);

            if (payment == null)
            {
                return NotFound(new
                {
                    message = "Payment not found for this order."
                });
            }

            return Ok(payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> Refund(int id)
    {
        try
        {
            await _paymentService.RefundAsync(id);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }
}