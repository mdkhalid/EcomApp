using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repository;

    public CategoriesController(ICategoryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        var categories = await _repository.GetAllAsync();
        return Ok(categories.Adapt<List<CategoryDto>>());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
            return NotFound();
        return Ok(category.Adapt<CategoryDto>());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto createDto)
    {
        var category = createDto.Adapt<Category>();
        var created = await _repository.AddAsync(category);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.Adapt<CategoryDto>());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] UpdateCategoryDto updateDto)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
            return NotFound();
        updateDto.Adapt(category);
        var updated = await _repository.UpdateAsync(category);
        return Ok(updated.Adapt<CategoryDto>());
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
}
