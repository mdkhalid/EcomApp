using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 5;

        var (items, totalCount) = await _repository.GetAllAsync(pageNumber, pageSize);

        return Ok(new
        {
            items = items.Adapt<List<ProductDto>>(),
            totalCount,
            pageNumber,
            pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();
        return Ok(product.Adapt<ProductDto>());
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto)
    {
        var product = createDto.Adapt<Product>();
        product.CreatedAt = DateTime.UtcNow;
        var created = await _repository.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.Adapt<ProductDto>());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto updateDto)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();
        updateDto.Adapt(product);
        var updated = await _repository.UpdateAsync(product);
        return Ok(updated.Adapt<ProductDto>());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}