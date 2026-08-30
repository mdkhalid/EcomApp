using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using EcomApi.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly IReviewRepository _reviewRepository;
    private readonly ApplicationDbContext _context;
    private readonly ISettingsProvider _settings;
    private readonly IServiceProvider _serviceProvider;

    public ProductsController(
        IProductRepository repository,
        IReviewRepository reviewRepository,
        ApplicationDbContext context,
        ISettingsProvider settings,
        IServiceProvider serviceProvider)
    {
        _repository = repository;
        _reviewRepository = reviewRepository;
        _context = context;
        _settings = settings;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultDto<ProductDto>>> GetAll([FromQuery] SearchFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 12;

        var searchResult = await _repository.SearchProductsAsync(filter);

        var productDtos = searchResult.Items.Adapt<List<ProductDto>>();

        var productIds = searchResult.Items.Select(p => p.Id).ToList();
        var ratings = await _reviewRepository.GetRatingsForProductsAsync(productIds);

        var galleryImages = await _context.ProductImages
            .Where(i => productIds.Contains(i.ProductId))
            .OrderBy(i => i.SortOrder)
            .Select(i => new { i.ProductId, i.Id, i.ImageUrl, i.SortOrder })
            .ToListAsync();

        var imagesByProduct = galleryImages
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new ProductImageDto { Id = x.Id, ImageUrl = x.ImageUrl, SortOrder = x.SortOrder }).ToList());

        for (int i = 0; i < productDtos.Count; i++)
        {
            if (ratings.ContainsKey(productIds[i]))
            {
                productDtos[i].AverageRating = ratings[productIds[i]].AverageRating;
                productDtos[i].TotalReviews = ratings[productIds[i]].TotalReviews;
            }
            if (imagesByProduct.TryGetValue(productIds[i], out var productImages))
            {
                productDtos[i].Images = productImages;
            }
        }

        var filterMetadata = await _repository.GetFilterMetadataAsync(filter);

        return Ok(new SearchResultDto<ProductDto>
        {
            Items = productDtos,
            TotalCount = searchResult.TotalCount,
            PageNumber = searchResult.PageNumber,
            PageSize = searchResult.PageSize,
            Filters = filterMetadata
        });
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<SearchSuggestionDto>> GetSuggestions([FromQuery] string query, CancellationToken cancellationToken = default)
    {
        var suggestions = await _repository.GetSearchSuggestionsAsync(query);
        return Ok(new SearchSuggestionDto
        {
            Suggestions = suggestions,
            PopularCategories = await _repository.GetBrandsAsync()
        });
    }

    [HttpGet("filters")]
    public async Task<ActionResult<FilterMetadataDto>> GetFilters([FromQuery] SearchFilterDto filter, CancellationToken cancellationToken = default)
    {
        var metadata = await _repository.GetFilterMetadataAsync(filter);
        return Ok(metadata);
    }

    [HttpGet("brands")]
    public async Task<ActionResult<List<string>>> GetBrands(CancellationToken cancellationToken = default)
    {
        var brands = await _repository.GetBrandsAsync();
        return Ok(brands);
    }

    [HttpGet("price-range")]
    public async Task<ActionResult<PriceRangeDto>> GetPriceRange([FromQuery] string? category = null, CancellationToken cancellationToken = default)
    {
        var range = await _repository.GetPriceRangeAsync(category);
        return Ok(range);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        var dto = product.Adapt<ProductDto>();
        var (avgRating, totalReviews) = await _reviewRepository.GetProductRatingAsync(id);
        dto.AverageRating = avgRating;
        dto.TotalReviews = totalReviews;

        var images = await _context.ProductImages
            .Where(i => i.ProductId == id)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductImageDto { Id = i.Id, ImageUrl = i.ImageUrl, SortOrder = i.SortOrder })
            .ToListAsync();
        dto.Images = images;

        var variants = await _context.ProductVariants
            .Where(v => v.ProductId == id)
            .OrderBy(v => v.SortOrder)
            .Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Name = v.Name,
                Price = v.Price,
                Stock = v.Stock,
                ImageUrl = v.ImageUrl,
                SortOrder = v.SortOrder
            })
            .ToListAsync();
        dto.Variants = variants;

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto, CancellationToken cancellationToken = default)
    {
        var product = createDto.Adapt<Product>();
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        var created = await _repository.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.Adapt<ProductDto>());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto updateDto, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        var oldStock = product.Stock;
        updateDto.Adapt(product);
        product.UpdatedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateAsync(product);

        // Trigger stock alerts if stock went from 0 to >0
        if (oldStock == 0 && product.Stock > 0)
        {
            await NotifyStockAlertsAsync(product.Id, null, cancellationToken);
        }

        return Ok(updated.Adapt<ProductDto>());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }

    [HttpGet("{id}/variants")]
    public async Task<ActionResult<List<ProductVariantDto>>> GetVariants(int id, CancellationToken cancellationToken = default)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == id))
            return NotFound();
        var variants = await _context.ProductVariants
            .Where(v => v.ProductId == id)
            .OrderBy(v => v.SortOrder)
            .Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Name = v.Name,
                Price = v.Price,
                Stock = v.Stock,
                ImageUrl = v.ImageUrl,
                SortOrder = v.SortOrder
            })
            .ToListAsync();
        return Ok(variants);
    }

    [HttpPost("{id}/variants")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(int id, [FromBody] CreateProductVariantDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == id))
            return NotFound();
        var variant = dto.Adapt<ProductVariant>();
        variant.ProductId = id;
        variant.CreatedAt = DateTime.UtcNow;
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetVariants), new { id }, variant.Adapt<ProductVariantDto>());
    }

    [HttpPut("{id}/variants/{variantId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(int id, int variantId, [FromBody] CreateProductVariantDto dto, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == id);
        if (variant == null)
            return NotFound();

        var oldStock = variant.Stock;
        dto.Adapt(variant);
        await _context.SaveChangesAsync();

        // Trigger stock alerts if stock went from 0 to >0
        if (oldStock == 0 && variant.Stock > 0)
        {
            await NotifyStockAlertsAsync(id, variantId, cancellationToken);
        }

        return Ok(variant.Adapt<ProductVariantDto>());
    }

    [HttpDelete("{id}/variants/{variantId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteVariant(int id, int variantId, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == id);
        if (variant == null)
            return NotFound();
        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> UploadImage(int id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Invalid file type. Allowed: jpg, jpeg, png, webp." });
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "File size exceeds 5MB limit." });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var maxSort = await _context.ProductImages
            .Where(i => i.ProductId == id)
            .MaxAsync(i => (int?)i.SortOrder) ?? 0;

        var productImage = new ProductImage
        {
            ProductId = id,
            ImageUrl = $"/uploads/{fileName}",
            SortOrder = maxSort + 1
        };
        _context.ProductImages.Add(productImage);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id });
    }

    [HttpPut("{id}/images/{imageId}/sort")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateImageSort(int id, int imageId, [FromBody] int sortOrder, CancellationToken cancellationToken = default)
    {
        var image = await _context.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);
        if (image == null)
            return NotFound();
        image.SortOrder = sortOrder;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Sort order updated" });
    }

    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteImage(int id, int imageId, CancellationToken cancellationToken = default)
    {
        var image = await _context.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);
        if (image == null)
            return NotFound();

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/stock-alerts")]
    [Authorize]
    public async Task<ActionResult<StockAlertDto>> CreateStockAlert(int id, [FromBody] CreateStockAlertDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ProductId != id)
            return BadRequest(new { error = "ProductId mismatch" });

        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { error = "Product not found" });

        if (dto.VariantId.HasValue)
        {
            var variant = await _context.ProductVariants.FindAsync(dto.VariantId.Value);
            if (variant == null || variant.ProductId != id)
                return NotFound(new { error = "Variant not found" });
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId == 0)
            return Unauthorized();

        // Check if already subscribed
        var existing = await _context.StockAlerts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ProductId == id && s.VariantId == dto.VariantId, cancellationToken);

        if (existing != null)
            return Conflict(new { error = "Already subscribed to stock alerts for this product" });

        // Only create alert if product/variant is out of stock
        bool isOutOfStock = dto.VariantId.HasValue
            ? (await _context.ProductVariants.FindAsync(dto.VariantId.Value))?.Stock == 0
            : product.Stock == 0;

        if (!isOutOfStock)
            return BadRequest(new { error = "Product is currently in stock" });

        var alert = new StockAlert
        {
            UserId = userId,
            ProductId = id,
            VariantId = dto.VariantId,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockAlerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = alert.Id }, new StockAlertDto
        {
            Id = alert.Id,
            ProductId = alert.ProductId,
            VariantId = alert.VariantId,
            CreatedAt = alert.CreatedAt,
            NotifiedAt = alert.NotifiedAt
        });
    }

    [HttpDelete("{id}/stock-alerts")]
    [Authorize]
    public async Task<ActionResult> DeleteStockAlert(int id, [FromQuery] int? variantId, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId == 0)
            return Unauthorized();

        var alert = await _context.StockAlerts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ProductId == id && s.VariantId == variantId, cancellationToken);

        if (alert == null)
            return NotFound();

        _context.StockAlerts.Remove(alert);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("{id}/stock-alerts")]
    [Authorize]
    public async Task<ActionResult<List<StockAlertDto>>> GetMyStockAlerts(int id, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId == 0)
            return Unauthorized();

        var alerts = await _context.StockAlerts
            .Where(s => s.UserId == userId && s.ProductId == id)
            .Select(s => new StockAlertDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                VariantId = s.VariantId,
                CreatedAt = s.CreatedAt,
                NotifiedAt = s.NotifiedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(alerts);
    }

    private async Task NotifyStockAlertsAsync(int productId, int? variantId, CancellationToken cancellationToken)
    {
        var alerts = await _context.StockAlerts
            .Where(s => s.ProductId == productId && s.VariantId == variantId && s.NotifiedAt == null)
            .ToListAsync(cancellationToken);

        if (alerts.Count == 0)
            return;

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return;

        string productUrl = $"{_settings.GetBaseUrl()}/products/{productId}";
        if (variantId.HasValue)
            productUrl += $"?variant={variantId.Value}";

        var notificationQueue = _serviceProvider.GetRequiredService<INotificationQueue>();
        var settings = _serviceProvider.GetRequiredService<ISettingsProvider>();

        foreach (var alert in alerts)
        {
            if (alert.User == null)
            {
                alert.User = await _context.Users.FindAsync(alert.UserId);
                if (alert.User == null) continue;
            }

            var unsubscribeToken = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes($"{alert.Id}-{alert.UserId}-{DateTime.UtcNow:yyyyMMdd}")));
            var unsubscribeLink = $"{_settings.GetBaseUrl()}/api/products/{productId}/stock-alerts/unsubscribe?token={unsubscribeToken}";

            var message = new NotificationMessage
            {
                Type = NotificationType.BackInStock,
                Email = alert.User.Email,
                Subject = $"Back in Stock: {product.Name}",
                HtmlBody = EmailTemplates.BackInStock(
                    alert.User.FirstName ?? alert.User.Username,
                    product.Name,
                    productUrl,
                    unsubscribeLink)
            };

            await notificationQueue.EnqueueAsync(message, cancellationToken);
            alert.NotifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
