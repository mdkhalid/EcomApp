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

        TypeAdapterConfig<Cart, CartDto>.NewConfig()
            .Map(dest => dest.Items, src => src.Items)
            .Compile();

        TypeAdapterConfig<CartItem, CartItemDto>.NewConfig()
            .Map(dest => dest.ProductName, src => src.Product.Name)
            .Map(dest => dest.ProductImage, src => src.Product.ImageUrl)
            .Compile();

        TypeAdapterConfig<Order, OrderDto>.NewConfig()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Items, src => src.Items)
            .Compile();

        TypeAdapterConfig<OrderItem, OrderItemDto>.NewConfig().Compile();

        TypeAdapterConfig<Review, ReviewDto>.NewConfig()
            .Map(dest => dest.UserName, src => src.User.Username)
            .Compile();

        TypeAdapterConfig<Category, CategoryDto>.NewConfig().Compile();
        TypeAdapterConfig<CreateCategoryDto, Category>.NewConfig()
            .Map(dest => dest.Id, _ => 0)
            .Compile();
        TypeAdapterConfig<UpdateCategoryDto, Category>.NewConfig().Compile();

        TypeAdapterConfig<Banner, BannerDto>.NewConfig().Compile();
        TypeAdapterConfig<CreateBannerDto, Banner>.NewConfig()
            .Map(dest => dest.Id, _ => 0)
            .Compile();
        TypeAdapterConfig<UpdateBannerDto, Banner>.NewConfig().Compile();

        TypeAdapterConfig<Address, AddressDto>.NewConfig().Compile();
        TypeAdapterConfig<CreateAddressDto, Address>.NewConfig()
            .Map(dest => dest.Id, _ => 0)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Compile();
    }
}