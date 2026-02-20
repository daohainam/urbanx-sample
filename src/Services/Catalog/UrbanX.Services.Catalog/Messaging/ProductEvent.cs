namespace UrbanX.Services.Catalog.Messaging;

public enum ProductEventType
{
    Created,
    Updated,
    Deleted
}

public class ProductEvent
{
    public Guid ProductId { get; set; }
    public ProductEventType EventType { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid MerchantId { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime OccurredAt { get; set; }
}
