using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using System.Reflection.Emit;

namespace PaymentService.Infrastructure.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.OrderId)
                .IsRequired();

            entity.Property(p => p.UserId)
                .IsRequired();

            entity.Property(p => p.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(p => p.Status)
                .IsRequired();

            entity.Property(p => p.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.TransactionId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.PaymentDate)
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .IsRequired();

            entity.HasIndex(p => p.OrderId);
            entity.HasIndex(p => p.TransactionId)
                .IsUnique();
        });
    }
}