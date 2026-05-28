using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly IReviewRepository _reviewRepository;

    public ProductsController(IProductRepository repository, IReviewRepository reviewRepository)
    {
        _repository = repository;
        _reviewRepository = reviewRepository;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultDto<ProductDto>>> GetAll([FromQuery] SearchFilterDto filter)
    {
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 12;

        var searchResult = await _repository.SearchProductsAsync(filter);

        var productDtos = searchResult.Items.Adapt<List<ProductDto>>();

        // Get ratings for all products
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

        // Get filter metadata
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

    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> UploadImage(int id, IFormFile file)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
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

        product.ImageUrl = $"/uploads/{fileName}";
        product.UpdatedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateAsync(product);
        return Ok(updated.Adapt<ProductDto>());
    }
}
