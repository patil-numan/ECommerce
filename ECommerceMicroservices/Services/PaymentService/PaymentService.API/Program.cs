using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Repositories;
using PaymentService.Infrastructure.Data;
using PaymentService.Infrastructure.Repositories;
using PaymentService.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Health Checks
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "PaymentDatabase")));

builder.Services.AddScoped<
    IPaymentRepository,
    PaymentRepository>();

builder.Services.AddScoped<
    IUnitOfWork,
    UnitOfWork>();

builder.Services.AddScoped<
    IPaymentService,
    PaymentService.Application.Services.PaymentService>();

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