using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Category).HasMaxLength(100);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.Items).WithOne(e => e.Cart).HasForeignKey(e => e.CartId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ShippingName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ShippingAddress).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ShippingCity).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ShippingZip).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(e => e.Items).WithOne(e => e.Order).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ProductImage).HasMaxLength(500);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
        });
    }
}