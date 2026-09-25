using Microsoft.EntityFrameworkCore;
using OrderService.Application.Clients;
using OrderService.Application.Repositories;
using OrderService.Application.Services;
using OrderService.Infrastructure.Clients;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Health Checks
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("OrderDatabase")));

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application services
builder.Services.AddScoped<
    IOrderService,
    OrderService.Application.Services.OrderService>();

// Product Service HTTP client
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(
    client =>
    {
        var productServiceUrl =
            builder.Configuration["Services:ProductServiceUrl"];

        if (string.IsNullOrWhiteSpace(productServiceUrl))
        {
            throw new InvalidOperationException(
                "Product Service URL is not configured.");
        }

        client.BaseAddress = new Uri(productServiceUrl);
    });

// Payment Service HTTP client
builder.Services.AddHttpClient<
    IPaymentServiceClient,
    PaymentServiceClient>(
    client =>
    {
        var paymentServiceUrl =
            builder.Configuration["Services:PaymentServiceUrl"];

        if (string.IsNullOrWhiteSpace(paymentServiceUrl))
        {
            throw new InvalidOperationException(
                "Payment Service URL is not configured.");
        }

        client.BaseAddress = new Uri(paymentServiceUrl);
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();