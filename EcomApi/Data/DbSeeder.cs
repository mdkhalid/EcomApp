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
        SeedAddresses(context);
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
                Role = UserRoles.Admin,
                FirstName = "System",
                LastName = "Administrator",
                Phone = "+1-555-0100",
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 15),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123");
            context.Users.Add(admin);
        }
        else
        {
            var existingAdmin = context.Users.First(u => u.Email == "admin@ecom.com");
            if (existingAdmin.Gender == null)
            {
                existingAdmin.Phone ??= "+1-555-0100";
                existingAdmin.Gender = "Male";
                existingAdmin.DateOfBirth = new DateTime(1990, 1, 15);
            }
        }

        if (!context.Users.Any(u => u.Email == "demo@ecom.com"))
        {
            var demoUser = new User
            {
                Email = "demo@ecom.com",
                Username = "demo",
                Role = UserRoles.Customer,
                FirstName = "Demo",
                LastName = "User",
                Phone = "+1-555-0200",
                Gender = "Female",
                DateOfBirth = new DateTime(1995, 6, 20),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            demoUser.PasswordHash = passwordHasher.HashPassword(demoUser, "Demo@123");
            context.Users.Add(demoUser);
        }
        else
        {
            var existingDemo = context.Users.First(u => u.Email == "demo@ecom.com");
            if (existingDemo.Gender == null)
            {
                existingDemo.Phone ??= "+1-555-0200";
                existingDemo.Gender = "Female";
                existingDemo.DateOfBirth = new DateTime(1995, 6, 20);
            }
        }

        context.SaveChanges();
    }

    private static void SeedCategories(ApplicationDbContext context)
    {
        var existingNames = context.Categories.Select(c => c.Name).ToHashSet();

        var categories = new List<Category>
        {
            new() { Name = "Electronics", Icon = "devices" },
            new() { Name = "Clothing", Icon = "checkroom" },
            new() { Name = "Footwear", Icon = "directions_walk" },
            new() { Name = "Home", Icon = "home" },
            new() { Name = "Accessories", Icon = "work" },
            new() { Name = "Beauty", Icon = "spa" },
            new() { Name = "Sports", Icon = "sports_esports" },
            new() { Name = "Books", Icon = "menu_book" },
            new() { Name = "Grocery", Icon = "shopping_bag" },
            new() { Name = "Toys", Icon = "toys" },
            new() { Name = "Medicine", Icon = "medical_services" },
            new() { Name = "Stationery", Icon = "edit_note" }
        };

        var newCategories = categories.Where(c => !existingNames.Contains(c.Name)).ToList();
        if (newCategories.Count > 0)
        {
            context.Categories.AddRange(newCategories);
            context.SaveChanges();
        }

        // Fix invalid icons on existing categories
        var iconMap = categories.ToDictionary(c => c.Name, c => c.Icon);
        var existingCategories = context.Categories.Where(c => iconMap.Keys.Contains(c.Name)).ToList();
        foreach (var cat in existingCategories)
        {
            if (iconMap.TryGetValue(cat.Name, out var correctIcon) && cat.Icon != correctIcon)
            {
                cat.Icon = correctIcon;
            }
        }
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
        var existingNames = context.Products.Select(p => p.Name).ToHashSet();

        var products = new List<Product>
        {
            // Electronics
            new() { Name = "Camera", Description = "Digital camera with high resolution", Price = 499.99m, Stock = 15, Category = "Electronics", ImageUrl = "/uploads/Camera.jpg" },
            new() { Name = "Keyboard", Description = "Mechanical keyboard", Price = 89.99m, Stock = 25, Category = "Electronics", ImageUrl = "/uploads/Keyboard.jpg" },
            new() { Name = "Gaming Keyboard", Description = "RGB gaming keyboard", Price = 129.99m, Stock = 20, Category = "Electronics", ImageUrl = "/uploads/Keyboard1.jpg" },
            new() { Name = "MacBook Pro", Description = "Apple MacBook Pro laptop", Price = 1999.99m, Stock = 10, Category = "Electronics", ImageUrl = "/uploads/Macbook.jpg" },
            new() { Name = "MacBook Air", Description = "Apple MacBook Air laptop", Price = 1499.99m, Stock = 12, Category = "Electronics", ImageUrl = "/uploads/Macbook1.jpg" },
            new() { Name = "Headphones", Description = "Over-ear headphones", Price = 79.99m, Stock = 30, Category = "Electronics", ImageUrl = "/uploads/headphone.jpg" },
            new() { Name = "Wireless Earbuds", Description = "Bluetooth wireless earbuds", Price = 49.99m, Stock = 50, Category = "Electronics", ImageUrl = "/uploads/headphone1.jpg" },
            new() { Name = "VR Headset", Description = "Virtual reality headset", Price = 299.99m, Stock = 10, Category = "Electronics", ImageUrl = "/uploads/goggle.jpg" },

            // Clothing
            new() { Name = "T-Shirt", Description = "Cotton casual t-shirt", Price = 19.99m, Stock = 100, Category = "Clothing", ImageUrl = "/uploads/tshirt.jpg" },
            new() { Name = "Polo T-Shirt", Description = "Premium polo t-shirt", Price = 29.99m, Stock = 80, Category = "Clothing", ImageUrl = "/uploads/tshirt1.jpg" },
            new() { Name = "Designer T-Shirt", Description = "Designer brand t-shirt", Price = 39.99m, Stock = 60, Category = "Clothing", ImageUrl = "/uploads/tshirt2.jpg" },
            new() { Name = "Winter Gloves", Description = "Warm winter gloves", Price = 24.99m, Stock = 40, Category = "Clothing", ImageUrl = "/uploads/Gloves.jpg" },

            // Footwear
            new() { Name = "Running Shoes", Description = "Comfortable running shoes", Price = 79.99m, Stock = 25, Category = "Footwear", ImageUrl = "/uploads/shoes.jpg" },
            new() { Name = "Casual Sneakers", Description = "Stylish casual sneakers", Price = 69.99m, Stock = 30, Category = "Footwear", ImageUrl = "/uploads/shoes1.jpg" },
            new() { Name = "Sports Shoes", Description = "High-performance sports shoes", Price = 89.99m, Stock = 20, Category = "Footwear", ImageUrl = "/uploads/shoes2.jpg" },

            // Home
            new() { Name = "Coffee", Description = "Premium coffee beans", Price = 14.99m, Stock = 50, Category = "Home", ImageUrl = "/uploads/Coffee.jpg" },
            new() { Name = "Watering Can", Description = "Garden watering can", Price = 12.99m, Stock = 35, Category = "Home", ImageUrl = "/uploads/Watering_Can.jpg" },
            new() { Name = "Garden Manure", Description = "Organic garden manure", Price = 9.99m, Stock = 100, Category = "Home", ImageUrl = "/uploads/Manure.jpeg" },

            // Accessories
            new() { Name = "Backpack", Description = "Travel backpack", Price = 59.99m, Stock = 35, Category = "Accessories", ImageUrl = "/uploads/Bag.jpg" },
            new() { Name = "Tote Bag", Description = "Canvas tote bag", Price = 19.99m, Stock = 50, Category = "Accessories", ImageUrl = "/uploads/Jhola.jpg" },
            new() { Name = "Analog Watch", Description = "Classic analog watch", Price = 149.99m, Stock = 15, Category = "Accessories", ImageUrl = "/uploads/watcht.jpg" },
            new() { Name = "Digital Watch", Description = "Digital sports watch", Price = 79.99m, Stock = 25, Category = "Accessories", ImageUrl = "/uploads/watcht1.jpg" },
            new() { Name = "Smart Watch", Description = "Smart watch with fitness tracking", Price = 199.99m, Stock = 12, Category = "Accessories", ImageUrl = "/uploads/watcht2.jpg" },
            new() { Name = "Handbag", Description = "Women's handbag", Price = 49.99m, Stock = 20, Category = "Accessories", ImageUrl = "/uploads/Perse.jpg" },

            // Beauty
            new() { Name = "Baby Cream", Description = "Gentle baby cream", Price = 8.99m, Stock = 60, Category = "Beauty", ImageUrl = "/uploads/Baby_Cream.jpg" },
            new() { Name = "Baby Powder", Description = "Baby powder", Price = 5.99m, Stock = 80, Category = "Beauty", ImageUrl = "/uploads/Baby_Powder.jpg" },
            new() { Name = "Baby Shampoo", Description = "Tear-free baby shampoo", Price = 7.99m, Stock = 50, Category = "Beauty", ImageUrl = "/uploads/Baby_Shampoo.jpg" },
            new() { Name = "Baby Wipes", Description = "Soft baby wipes", Price = 4.99m, Stock = 100, Category = "Beauty", ImageUrl = "/uploads/Baby_Wipes.jpg" },
            new() { Name = "Baby Care Set", Description = "Complete baby care set", Price = 24.99m, Stock = 30, Category = "Beauty", ImageUrl = "/uploads/Baby_care_products.jpg" },
            new() { Name = "Diapers", Description = "Baby diapers pack", Price = 19.99m, Stock = 100, Category = "Beauty", ImageUrl = "/uploads/Diapers.jpg" },
            new() { Name = "Beard Oil", Description = "Men's beard oil", Price = 12.99m, Stock = 40, Category = "Beauty", ImageUrl = "/uploads/Beard_Oil.jpg" },
            new() { Name = "Face Cream", Description = "Moisturizing face cream", Price = 14.99m, Stock = 45, Category = "Beauty", ImageUrl = "/uploads/Face_Cream.jpg" },
            new() { Name = "Baby Oil", Description = "Johnson's baby oil", Price = 6.99m, Stock = 70, Category = "Beauty", ImageUrl = "/uploads/Johnson's_Baby_Oil.jpg" },
            new() { Name = "Face Wash", Description = "Lakme face wash", Price = 9.99m, Stock = 55, Category = "Beauty", ImageUrl = "/uploads/Lakme_Facewash.jpg" },
            new() { Name = "Lip Balm", Description = "Moisturizing lip balm", Price = 3.99m, Stock = 90, Category = "Beauty", ImageUrl = "/uploads/Lip_Balm.jpg" },
            new() { Name = "Men's Face Wash", Description = "Men's face wash", Price = 11.99m, Stock = 45, Category = "Beauty", ImageUrl = "/uploads/Men's_Face_Wash.jpg" },
            new() { Name = "Moisturizer", Description = "Body moisturizer", Price = 13.99m, Stock = 50, Category = "Beauty", ImageUrl = "/uploads/Moisturizer.jpg" },

            // Grocery
            new() { Name = "Fresh Apples", Description = "Fresh red apples (1kg)", Price = 4.99m, Stock = 100, Category = "Grocery", ImageUrl = "/uploads/Apple.jpg" },
            new() { Name = "Coca Cola", Description = "Coca Cola 500ml", Price = 1.99m, Stock = 200, Category = "Grocery", ImageUrl = "/uploads/Coke.jpg" },
            new() { Name = "Orange Juice", Description = "Fresh orange juice 1L", Price = 3.99m, Stock = 80, Category = "Grocery", ImageUrl = "/uploads/OrangeDrink.jpg" },
            new() { Name = "Sprite", Description = "Sprite 500ml", Price = 1.99m, Stock = 200, Category = "Grocery", ImageUrl = "/uploads/Sprite.jpg" },
            new() { Name = "Atta", Description = "Whole wheat flour 5kg", Price = 8.99m, Stock = 60, Category = "Grocery", ImageUrl = "/uploads/atta.jpg" },
            new() { Name = "Maggi Noodles", Description = "Maggi instant noodles", Price = 0.99m, Stock = 300, Category = "Grocery", ImageUrl = "/uploads/maggi.jpg" },
            new() { Name = "Fresh Oranges", Description = "Fresh oranges (1kg)", Price = 3.99m, Stock = 120, Category = "Grocery", ImageUrl = "/uploads/Oranges.jpg" },
            new() { Name = "Potato Chips", Description = "Crispy potato chips", Price = 2.49m, Stock = 150, Category = "Grocery", ImageUrl = "/uploads/chip.png" },

            // Medicine
            new() { Name = "Cough Syrup", Description = "CUFRIL-D cough syrup", Price = 5.99m, Stock = 50, Category = "Medicine", ImageUrl = "/uploads/CUFRIL-D_cough_syrup.jpg" },
            new() { Name = "Cetirizine", Description = "Cetirizine allergy tablets", Price = 3.99m, Stock = 100, Category = "Medicine", ImageUrl = "/uploads/Cetirizine.jpg" },
            new() { Name = "Cold Medicine", Description = "Cheston Cold tablets", Price = 4.99m, Stock = 80, Category = "Medicine", ImageUrl = "/uploads/Cheston_Cold.jpg" },
            new() { Name = "Dolo 650", Description = "Dolo 650 paracetamol", Price = 2.99m, Stock = 150, Category = "Medicine", ImageUrl = "/uploads/Dolo.jpg" },
            new() { Name = "Gelusil", Description = "Gelusil antacid", Price = 3.49m, Stock = 60, Category = "Medicine", ImageUrl = "/uploads/Gelusil.jfif" },
            new() { Name = "Gelusil Plus", Description = "Gelusil Plus antacid", Price = 4.49m, Stock = 55, Category = "Medicine", ImageUrl = "/uploads/Gelusil_1.jpg" },
            new() { Name = "Medicine Kit", Description = "First aid medicine kit", Price = 19.99m, Stock = 30, Category = "Medicine", ImageUrl = "/uploads/Medicines.jpg" },
            new() { Name = "Metolar XR 50", Description = "Metolar XR 50 tablets", Price = 8.99m, Stock = 40, Category = "Medicine", ImageUrl = "/uploads/Metolar_XR_50.jpg" },

            // Stationery
            new() { Name = "Pencil Set", Description = "Pencil set (12 pcs)", Price = 3.99m, Stock = 100, Category = "Stationery", ImageUrl = "/uploads/Pencils.jpg" },
            new() { Name = "Pen Set", Description = "Ball pen set (10 pcs)", Price = 4.99m, Stock = 80, Category = "Stationery", ImageUrl = "/uploads/Pens.jpg" },
            new() { Name = "Push Pins", Description = "Colored push pins (50 pcs)", Price = 2.49m, Stock = 120, Category = "Stationery", ImageUrl = "/uploads/Pins.jpg" },
            new() { Name = "Paper Puncher", Description = "Heavy duty paper puncher", Price = 7.99m, Stock = 40, Category = "Stationery", ImageUrl = "/uploads/Puncher.jpg" },
            new() { Name = "Stapler", Description = "Desktop stapler", Price = 5.99m, Stock = 50, Category = "Stationery", ImageUrl = "/uploads/Stapler.jpg" },
            new() { Name = "Sticky Notes", Description = "Sticky notes pack", Price = 2.99m, Stock = 100, Category = "Stationery", ImageUrl = "/uploads/Sticky_Notes.jpg" }
        };

        var newProducts = products.Where(p => !existingNames.Contains(p.Name)).ToList();
        if (newProducts.Count > 0)
        {
            context.Products.AddRange(newProducts);
            context.SaveChanges();
        }

        // Ensure all existing products are active
        var inactiveProducts = context.Products.Where(p => !p.IsActive).ToList();
        if (inactiveProducts.Count > 0)
        {
            foreach (var product in inactiveProducts)
            {
                product.IsActive = true;
            }
            context.SaveChanges();
        }

        // Fix broken image URLs (wrong extensions)
        var imageUrlFixes = new Dictionary<string, string>
        {
            { "/uploads/headphone.jpt", "/uploads/headphone.jpg" },
            { "/uploads/headphone1.jpt", "/uploads/headphone1.jpg" },
            { "/uploads/goggle.jpt", "/uploads/goggle.jpg" },
            { "/uploads/Coke.jpt", "/uploads/Coke.jpg" }
        };

        foreach (var (wrongUrl, correctUrl) in imageUrlFixes)
        {
            var productsToFix = context.Products.Where(p => p.ImageUrl == wrongUrl).ToList();
            foreach (var product in productsToFix)
            {
                product.ImageUrl = correctUrl;
            }
        }
        context.SaveChanges();
    }

    private static void SeedAddresses(ApplicationDbContext context)
    {
        if (context.Addresses.Any()) return;

        var demoUser = context.Users.FirstOrDefault(u => u.Email == "demo@ecom.com");
        if (demoUser == null) return;

        var addresses = new List<Address>
        {
            new()
            {
                UserId = demoUser.Id,
                Label = "Home",
                Street = "123 Main Street, Apt 4B",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "United States",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = demoUser.Id,
                Label = "Work",
                Street = "456 Business Ave, Suite 200",
                City = "New York",
                State = "NY",
                ZipCode = "10018",
                Country = "United States",
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Addresses.AddRange(addresses);
        context.SaveChanges();
    }
}
