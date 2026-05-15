using EcomApi.DTOs;
using EcomApi.Models;
using Mapster;

namespace EcomApi.Mapping;

public static class MappingConfig
{
    public static void ConfigureMapping()
    {
        TypeAdapterConfig<Product, ProductDto>.NewConfig().Compile();
        TypeAdapterConfig<Product, CreateProductDto>.NewConfig().Compile();
        TypeAdapterConfig<CreateProductDto, Product>.NewConfig()
            .Map(dest => dest.Id, _ => 0)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Compile();
        TypeAdapterConfig<Product, UpdateProductDto>.NewConfig().Compile();
        TypeAdapterConfig<UpdateProductDto, Product>.NewConfig()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Compile();
    }
}