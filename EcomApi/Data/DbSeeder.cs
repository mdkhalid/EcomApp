using EcomApi.Models;
using Microsoft.AspNetCore.Identity;

namespace EcomApi.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        SeedUsers(context);
        SeedCategories(context);
        SeedBanners(context);
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

    private static void SeedCategories(ApplicationDbContext context)
    {
        if (context.Categories.Any()) return;

        var categories = new List<Category>
        {
            new() { Name = "Electronics", Icon = "devices" },
            new() { Name = "Clothing", Icon = "checkroom" },
            new() { Name = "Footwear", Icon = "sports_and_outdoors" },
            new() { Name = "Home", Icon = "home" },
            new() { Name = "Accessories", Icon = "work" },
            new() { Name = "Beauty", Icon = "spa" },
            new() { Name = "Sports", Icon = "sports_esports" },
            new() { Name = "Books", Icon = "menu_book" },
            new() { Name = "Grocery", Icon = "shopping_bag" },
            new() { Name = "Toys", Icon = "toys" }
        };

        context.Categories.AddRange(categories);
        context.SaveChanges();
    }

    private static void SeedBanners(ApplicationDbContext context)
    {
        if (context.Banners.Any()) return;

        var banners = new List<Banner>
        {
            new()
            {
                Title = "Big Billion Days",
                Subtitle = "Up to 70% off on Electronics",
                BgGradient = "linear-gradient(135deg, #2874F0, #1a5dc8)",
                Icon = "devices",
                StartDate = DateTime.UtcNow.AddDays(-30),
                DurationDays = 365,
                SortOrder = 1,
                IsActive = true
            },
            new()
            {
                Title = "Fashion Festival",
                Subtitle = "Min 50% off on Top Brands",
                BgGradient = "linear-gradient(135deg, #E91E63, #c2185b)",
                Icon = "checkroom",
                StartDate = DateTime.UtcNow.AddDays(-30),
                DurationDays = 365,
                SortOrder = 2,
                IsActive = true
            },
            new()
            {
                Title = "Home Makeover Sale",
                Subtitle = "Flat 40% off on Furniture & Home",
                BgGradient = "linear-gradient(135deg, #FF6F00, #e65100)",
                Icon = "home",
                StartDate = DateTime.UtcNow.AddDays(-30),
                DurationDays = 365,
                SortOrder = 3,
                IsActive = true
            },
            new()
            {
                Title = "Beauty & Grooming",
                Subtitle = "Up to 30% off on Beauty Products",
                BgGradient = "linear-gradient(135deg, #9C27B0, #7b1fa2)",
                Icon = "spa",
                StartDate = DateTime.UtcNow.AddDays(-30),
                DurationDays = 365,
                SortOrder = 4,
                IsActive = true
            },
            new()
            {
                Title = "Sports & Fitness",
                Subtitle = "Extra 25% off on Sports Gear",
                BgGradient = "linear-gradient(135deg, #009688, #00695c)",
                Icon = "sports_esports",
                StartDate = DateTime.UtcNow.AddDays(-30),
                DurationDays = 365,
                SortOrder = 5,
                IsActive = true
            }
        };

        context.Banners.AddRange(banners);
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
