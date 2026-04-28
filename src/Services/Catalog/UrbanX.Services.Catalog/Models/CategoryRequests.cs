using System.ComponentModel.DataAnnotations;

namespace UrbanX.Services.Catalog.Models;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(120)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(120)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
