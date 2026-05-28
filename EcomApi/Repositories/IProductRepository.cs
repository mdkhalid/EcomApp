using EcomApi.DTOs;
using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IProductRepository
{
    Task<SearchResultDto<Product>> SearchProductsAsync(SearchFilterDto filter);
    Task<List<string>> GetSearchSuggestionsAsync(string query);
    Task<FilterMetadataDto> GetFilterMetadataAsync(SearchFilterDto filter);
    Task<List<string>> GetBrandsAsync();
    Task<PriceRangeDto> GetPriceRangeAsync(string? category = null);
    Task<Product?> GetByIdAsync(int id);
    Task<Product> AddAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
}