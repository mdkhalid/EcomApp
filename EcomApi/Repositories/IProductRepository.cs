using EcomApi.DTOs;
using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IProductRepository
{
    Task<SearchResultDto<Product>> SearchProductsAsync(SearchFilterDto filter, CancellationToken cancellationToken = default);
    Task<List<string>> GetSearchSuggestionsAsync(string query, CancellationToken cancellationToken = default);
    Task<FilterMetadataDto> GetFilterMetadataAsync(SearchFilterDto filter, CancellationToken cancellationToken = default);
    Task<List<string>> GetBrandsAsync(CancellationToken cancellationToken = default);
    Task<PriceRangeDto> GetPriceRangeAsync(string? category = null, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
