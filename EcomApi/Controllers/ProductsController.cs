using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
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

    public ProductsController(IProductRepository repository, IReviewRepository reviewRepository, ApplicationDbContext context)
    {
        _repository = repository;
        _reviewRepository = reviewRepository;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultDto<ProductDto>>> GetAll([FromQuery] SearchFilterDto filter)
    {
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 12;

        var searchResult = await _repository.SearchProductsAsync(filter);

        var productDtos = searchResult.Items.Adapt<List<ProductDto>>();

        var productIds = searchResult.Items.Select(p => p.Id).ToList();
        var ratings = await _reviewRepository.GetRatingsForProductsAsync(productIds);

        for (int i = 0; i < productDtos.Count; i++)
        {
            if (ratings.ContainsKey(productIds[i]))
            {
                productDtos[i].AverageRating = ratings[productIds[i]].AverageRating;
                productDtos[i].TotalReviews = ratings[productIds[i]].TotalReviews;
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
    public async Task<ActionResult<SearchSuggestionDto>> GetSuggestions([FromQuery] string query)
    {
        var suggestions = await _repository.GetSearchSuggestionsAsync(query);
        return Ok(new SearchSuggestionDto
        {
            Suggestions = suggestions,
            PopularCategories = await _repository.GetBrandsAsync()
        });
    }

    [HttpGet("filters")]
    public async Task<ActionResult<FilterMetadataDto>> GetFilters([FromQuery] SearchFilterDto filter)
    {
        var metadata = await _repository.GetFilterMetadataAsync(filter);
        return Ok(metadata);
    }

    [HttpGet("brands")]
    public async Task<ActionResult<List<string>>> GetBrands()
    {
        var brands = await _repository.GetBrandsAsync();
        return Ok(brands);
    }

    [HttpGet("price-range")]
    public async Task<ActionResult<PriceRangeDto>> GetPriceRange([FromQuery] string? category = null)
    {
        var range = await _repository.GetPriceRangeAsync(category);
        return Ok(range);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
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
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto)
    {
        var product = createDto.Adapt<Product>();
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        var created = await _repository.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.Adapt<ProductDto>());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto updateDto)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();
        updateDto.Adapt(product);
        product.UpdatedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateAsync(product);
        return Ok(updated.Adapt<ProductDto>());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }

    [HttpGet("{id}/variants")]
    public async Task<ActionResult<List<ProductVariantDto>>> GetVariants(int id)
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
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(int id, [FromBody] CreateProductVariantDto dto)
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
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(int id, int variantId, [FromBody] CreateProductVariantDto dto)
    {
        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == id);
        if (variant == null)
            return NotFound();
        dto.Adapt(variant);
        await _context.SaveChangesAsync();
        return Ok(variant.Adapt<ProductVariantDto>());
    }

    [HttpDelete("{id}/variants/{variantId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteVariant(int id, int variantId)
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
    public async Task<ActionResult<ProductDto>> UploadImage(int id, IFormFile file)
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

        if (product.ImageUrl == null)
        {
            product.ImageUrl = productImage.ImageUrl;
            await _repository.UpdateAsync(product);
        }

        return CreatedAtAction(nameof(GetById), new { id });
    }

    [HttpPut("{id}/images/{imageId}/sort")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateImageSort(int id, int imageId, [FromBody] int sortOrder)
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
    public async Task<ActionResult> DeleteImage(int id, int imageId)
    {
        var image = await _context.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);
        if (image == null)
            return NotFound();

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync();

        var product = await _repository.GetByIdAsync(id);
        if (product != null && product.ImageUrl == image.ImageUrl)
        {
            var nextImage = await _context.ProductImages
                .Where(i => i.ProductId == id)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefaultAsync();
            product.ImageUrl = nextImage?.ImageUrl;
            await _repository.UpdateAsync(product);
        }

        return NoContent();
    }
}
