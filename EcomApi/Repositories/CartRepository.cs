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
        return await GetByIdentifierAsync(sessionId);
    }

    public async Task<Cart?> GetByIdentifierAsync(string identifier)
    {
        if (identifier.StartsWith("user:"))
        {
            var userId = int.Parse(identifier.Substring(5));
            return await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();
        }

        var sessionId = identifier.StartsWith("session:") ? identifier.Substring(8) : identifier;
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    public async Task<Cart> CreateAsync(string sessionId)
    {
        var cart = new Cart();

        if (sessionId.StartsWith("user:"))
        {
            cart.UserId = int.Parse(sessionId.Substring(5));
        }
        else
        {
            cart.SessionId = sessionId.StartsWith("session:") ? sessionId.Substring(8) : sessionId;
        }

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task<Cart?> AddItemAsync(string sessionId, int productId, int quantity, int? productVariantId = null)
    {
        var cart = await GetByIdentifierAsync(sessionId) ?? await CreateAsync(sessionId);
        var product = await _context.Products.FindAsync(productId);

        if (product == null)
            return null;

        ProductVariant? variant = null;
        if (productVariantId.HasValue)
        {
            variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == productVariantId && v.ProductId == productId);
            if (variant == null || variant.Stock < quantity)
                return null;
        }
        else if (product.Stock < quantity)
        {
            return null;
        }

        var effectiveStock = variant?.Stock ?? product.Stock;
        var effectivePrice = variant?.Price ?? product.Price;

        var existingItem = cart.Items.FirstOrDefault(ci =>
            ci.ProductId == productId && ci.ProductVariantId == productVariantId);
        if (existingItem != null)
        {
            if (existingItem.Quantity + quantity > effectiveStock)
                return null;

            existingItem.Quantity += quantity;
            existingItem.UnitPrice = effectivePrice;
            existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
        }
        else
        {
            var newItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                ProductVariantId = productVariantId,
                VariantName = variant?.Name,
                Quantity = quantity,
                UnitPrice = effectivePrice,
                TotalPrice = quantity * effectivePrice
            };
            cart.Items.Add(newItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdentifierAsync(sessionId);
    }

    public async Task<Cart?> UpdateItemAsync(int cartItemId, int quantity)
    {
        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

        if (cartItem == null) return null;

        decimal effectivePrice;
        int effectiveStock;

        if (cartItem.ProductVariantId.HasValue)
        {
            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == cartItem.ProductVariantId);
            if (variant == null || variant.Stock < quantity)
                return null;
            effectivePrice = variant.Price;
            effectiveStock = variant.Stock;
        }
        else
        {
            var product = await _context.Products.FindAsync(cartItem.ProductId);
            if (product == null || product.Stock < quantity)
                return null;
            effectivePrice = product.Price;
            effectiveStock = product.Stock;
        }

        cartItem.Quantity = quantity;
        cartItem.UnitPrice = effectivePrice;
        cartItem.TotalPrice = quantity * cartItem.UnitPrice;
        cartItem.Cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var identifier = cartItem.Cart.UserId != null
            ? $"user:{cartItem.Cart.UserId}"
            : $"session:{cartItem.Cart.SessionId}";
        return await GetByIdentifierAsync(identifier);
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

        var identifier = cart.UserId != null
            ? $"user:{cart.UserId}"
            : $"session:{cart.SessionId}";
        return await GetByIdentifierAsync(identifier);
    }

    public async Task<Cart> ClearAsync(string sessionId)
    {
        var cart = await GetByIdentifierAsync(sessionId);
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

    public async Task MergeCartsAsync(string sourceIdentifier, string targetIdentifier)
    {
        var sourceCart = await GetByIdentifierAsync(sourceIdentifier);
        if (sourceCart == null || sourceCart.Items.Count == 0)
            return;

        var targetCart = await GetByIdentifierAsync(targetIdentifier) ?? await CreateAsync(targetIdentifier);

        foreach (var sourceItem in sourceCart.Items.ToList())
        {
            var existingItem = targetCart.Items.FirstOrDefault(ti =>
                ti.ProductId == sourceItem.ProductId && ti.ProductVariantId == sourceItem.ProductVariantId);
            if (existingItem != null)
            {
                existingItem.Quantity += sourceItem.Quantity;
                existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                targetCart.Items.Add(new CartItem
                {
                    CartId = targetCart.Id,
                    ProductId = sourceItem.ProductId,
                    Quantity = sourceItem.Quantity,
                    UnitPrice = sourceItem.UnitPrice,
                    TotalPrice = sourceItem.TotalPrice
                });
            }
        }

        targetCart.UpdatedAt = DateTime.UtcNow;
        _context.CartItems.RemoveRange(sourceCart.Items);
        _context.Carts.Remove(sourceCart);
        await _context.SaveChangesAsync();
    }
}
