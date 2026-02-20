namespace UrbanX.Services.Catalog.Search;

public class ProductDocument
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid MerchantId { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class ProductSearchResult
{
    public required IReadOnlyList<ProductDocument> Products { get; set; }
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
