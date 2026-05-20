using EcomApi.Models;
using Microsoft.AspNetCore.Identity;

namespace EcomApi.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        SeedUsers(context);
        SeedProducts(context);
    }

    private static void SeedUsers(ApplicationDbContext context)
    {
        var passwordHasher = new PasswordHasher<User>();

        if (!context.Users.Any(u => u.Email == "admin@ecom.com"))
        {
            var admin = new User
            {
                Email = "admin@ecom.com",
                Username = "admin",
                Role = "Admin",
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123");
            context.Users.Add(admin);
        }

        if (!context.Users.Any(u => u.Email == "demo@ecom.com"))
        {
            var demoUser = new User
            {
                Email = "demo@ecom.com",
                Username = "demo",
                Role = "Customer",
                FirstName = "Demo",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            demoUser.PasswordHash = passwordHasher.HashPassword(demoUser, "Demo@123");
            context.Users.Add(demoUser);
        }

        context.SaveChanges();
    }

    private static void SeedProducts(ApplicationDbContext context)
    {
        if (context.Products.Any()) return;

        var products = new List<Product>
        {
            new() { Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Stock = 10, Category = "Electronics", ImageUrl = "/uploads/75efe378-6825-4bbc-82ea-7ddaa482e16e.jpg" },
            new() { Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 50, Category = "Electronics", ImageUrl = "/uploads/3c00ec1a-f234-447f-9720-5f7a12bd7d98.jpg" },
            new() { Name = "Keyboard", Description = "Mechanical keyboard", Price = 89.99m, Stock = 25, Category = "Electronics", ImageUrl = "/uploads/c808a945-ae10-4e0c-801c-6595c9e1a38b.jpg" },
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
