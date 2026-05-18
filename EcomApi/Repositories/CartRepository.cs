using EcomApi.Models;
using EcomApi.DTOs;
using EcomApi.Data;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _context;

    public CartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetBySessionIdAsync(string sessionId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    public async Task<Cart> CreateAsync(string sessionId)
    {
        var cart = new Cart { SessionId = sessionId };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task<Cart?> AddItemAsync(string sessionId, int productId, int quantity)
    {
        var cart = await GetBySessionIdAsync(sessionId) ?? await CreateAsync(sessionId);
        var product = await _context.Products.FindAsync(productId);

        if (product == null || product.Stock < quantity)
            return null;

        var existingItem = cart.Items.FirstOrDefault(ci => ci.ProductId == productId);
        if (existingItem != null)
        {
            if (existingItem.Quantity + quantity > product.Stock)
                return null;

            existingItem.Quantity += quantity;
            existingItem.UnitPrice = product.Price;
            existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
        }
        else
        {
            var newItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price,
                TotalPrice = quantity * product.Price
            };
            cart.Items.Add(newItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetBySessionIdAsync(sessionId);
    }

    public async Task<Cart?> UpdateItemAsync(int cartItemId, int quantity)
    {
        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

        if (cartItem == null) return null;

        var product = await _context.Products.FindAsync(cartItem.ProductId);
        if (product == null || product.Stock < quantity)
            return null;

        cartItem.Quantity = quantity;
        cartItem.UnitPrice = product.Price;
        cartItem.TotalPrice = quantity * cartItem.UnitPrice;
        cartItem.Cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetBySessionIdAsync(cartItem.Cart.SessionId);
    }

    public async Task<Cart?> RemoveItemAsync(int cartItemId)
    {
        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

        if (cartItem == null) return null;

        var cart = cartItem.Cart;
        _context.CartItems.Remove(cartItem);
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetBySessionIdAsync(cart.SessionId);
    }

    public async Task<Cart> ClearAsync(string sessionId)
    {
        var cart = await GetBySessionIdAsync(sessionId);
        if (cart != null)
        {
            _context.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return cart ?? await CreateAsync(sessionId);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Carts.AnyAsync(c => c.Id == id);
    }
}