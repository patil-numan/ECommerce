using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.SKU)
                .IsRequired();

            entity.HasIndex(p => p.SKU)
                .IsUnique();

            entity.Property(p => p.Name)
                .IsRequired();

            entity.Property(p => p.Description)
                .IsRequired();

            entity.Property(p => p.Price)
                .HasPrecision(18, 2);

            entity.Property(p => p.StockQuantity)
                .IsRequired();

            entity.HasOne<Category>()
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired();
        });
    }
}