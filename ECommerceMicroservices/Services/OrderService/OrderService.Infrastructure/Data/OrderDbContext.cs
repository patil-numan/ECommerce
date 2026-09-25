using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using System.Reflection.Emit;

namespace OrderService.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.UserId)
                .IsRequired();

            entity.Property(o => o.OrderDate)
                .IsRequired();

            entity.Property(o => o.Status)
                .IsRequired();

            entity.Property(o => o.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);

            entity.Property(oi => oi.ProductId)
                .IsRequired();

            entity.Property(oi => oi.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(oi => oi.Quantity)
                .IsRequired();
        });
    }
}