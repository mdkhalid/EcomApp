using EcomApi.Models;

namespace EcomApi.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Products.Any()) return;

        var products = new List<Product>
        {
            new() { Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Stock = 10, Category = "Electronics" },
            new() { Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 50, Category = "Electronics" },
            new() { Name = "Keyboard", Description = "Mechanical keyboard", Price = 89.99m, Stock = 25, Category = "Electronics" },
            new() { Name = "T-Shirt", Description = "Cotton t-shirt", Price = 19.99m, Stock = 100, Category = "Clothing" },
            new() { Name = "Jeans", Description = "Denim jeans", Price = 49.99m, Stock = 30, Category = "Clothing" },
            new() { Name = "Sneakers", Description = "Running shoes", Price = 79.99m, Stock = 20, Category = "Footwear" },
            new() { Name = "Coffee Maker", Description = "Automatic coffee maker", Price = 129.99m, Stock = 15, Category = "Home" },
            new() { Name = "Desk Lamp", Description = "LED desk lamp", Price = 34.99m, Stock = 40, Category = "Home" },
            new() { Name = "Backpack", Description = "Travel backpack", Price = 59.99m, Stock = 35, Category = "Accessories" },
            new() { Name = "Watch", Description = "Smart watch", Price = 199.99m, Stock = 12, Category = "Accessories" }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}