using System.ComponentModel.DataAnnotations;

namespace UrbanX.Services.Catalog.Models;

public class CreateProductRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Required]
    public Guid MerchantId { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public string? Category { get; set; }
}

public class UpdateProductRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public string? Category { get; set; }

    public bool IsActive { get; set; }
}
