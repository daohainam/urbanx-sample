namespace UrbanX.Management.Web.Services;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateCategoryRequest(string Name, string? Slug, string? Description);
public record UpdateCategoryRequest(string Name, string? Slug, string? Description, bool IsActive);

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    Guid MerchantId,
    int StockQuantity,
    string? ImageUrl,
    Guid? CategoryId,
    string? Category,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ProductListResponse(IReadOnlyList<ProductDto> Products, long Total, int Page, int PageSize);

public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    Guid MerchantId,
    int StockQuantity,
    string? ImageUrl,
    Guid? CategoryId);

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    Guid? CategoryId,
    bool IsActive);
