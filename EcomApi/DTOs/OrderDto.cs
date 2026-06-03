using System.ComponentModel.DataAnnotations;
using EcomApi.Models;

namespace EcomApi.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingZip { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderStatusHistoryDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOrderDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ShippingName { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ShippingCity { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string ShippingZip { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(255)]
    public string? CustomerEmail { get; set; }

    [Phone]
    [StringLength(20)]
    public string? CustomerPhone { get; set; }

    [StringLength(50)]
    public string? CouponCode { get; set; }
}

public class UpdateOrderStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }
}

public class UpdateOrderTrackingDto
{
    [Required]
    [StringLength(100)]
    public string TrackingNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Carrier { get; set; } = string.Empty;

    public DateTime? EstimatedDeliveryDate { get; set; }
}

public class ShippingAddressDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
}
