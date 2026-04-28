using System.Net;
using System.Net.Http.Json;

namespace UrbanX.Management.Web.Services;

public class CatalogApiClient
{
    private readonly HttpClient _http;

    public CatalogApiClient(HttpClient http)
    {
        _http = http;
    }

    // ── Categories ────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var url = $"/api/categories?includeInactive={includeInactive.ToString().ToLowerInvariant()}";
        var result = await _http.GetFromJsonAsync<List<CategoryDto>>(url, ct);
        return result ?? [];
    }

    public async Task<CategoryDto?> GetCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/api/categories/{id}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: ct);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/categories", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: ct))!;
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/api/categories/{id}", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: ct))!;
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/api/categories/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }

    // ── Products ──────────────────────────────────────────────────────────────
    public async Task<ProductListResponse> GetProductsAsync(string? search, string? category, int page, int pageSize, CancellationToken ct = default)
    {
        var url = $"/api/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(category)) url += $"&category={Uri.EscapeDataString(category)}";
        var resp = await _http.GetFromJsonAsync<ProductListResponse>(url, ct);
        return resp ?? new ProductListResponse([], 0, page, pageSize);
    }

    public async Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/api/products/{id}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/api/products/{id}", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct))!;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/products", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct))!;
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/api/products/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
