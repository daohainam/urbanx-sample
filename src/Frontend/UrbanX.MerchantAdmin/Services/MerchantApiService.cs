using System.Net.Http.Json;
using UrbanX.MerchantAdmin.Models;

namespace UrbanX.MerchantAdmin.Services;

public class MerchantApiService
{
    private readonly HttpClient _httpClient;

    public MerchantApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Merchant
    public async Task<Merchant?> GetMerchantAsync(Guid merchantId)
    {
        return await _httpClient.GetFromJsonAsync<Merchant>($"api/merchants/{merchantId}");
    }

    public async Task<Merchant?> CreateMerchantAsync(Merchant merchant)
    {
        var response = await _httpClient.PostAsJsonAsync("api/merchants", merchant);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Merchant>();
    }

    // Products
    public async Task<List<MerchantProduct>> GetProductsAsync(Guid merchantId)
    {
        return await _httpClient.GetFromJsonAsync<List<MerchantProduct>>($"api/merchants/{merchantId}/products") 
            ?? new List<MerchantProduct>();
    }

    public async Task<MerchantProduct?> CreateProductAsync(Guid merchantId, MerchantProduct product)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/merchants/{merchantId}/products", product);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MerchantProduct>();
    }

    public async Task<MerchantProduct?> UpdateProductAsync(Guid merchantId, Guid productId, MerchantProduct product)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/merchants/{merchantId}/products/{productId}", product);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MerchantProduct>();
    }

    public async Task DeleteProductAsync(Guid merchantId, Guid productId)
    {
        var response = await _httpClient.DeleteAsync($"api/merchants/{merchantId}/products/{productId}");
        response.EnsureSuccessStatusCode();
    }

    // Categories
    public async Task<List<MerchantCategory>> GetCategoriesAsync(Guid merchantId)
    {
        return await _httpClient.GetFromJsonAsync<List<MerchantCategory>>($"api/merchants/{merchantId}/categories") 
            ?? new List<MerchantCategory>();
    }

    public async Task<MerchantCategory?> CreateCategoryAsync(Guid merchantId, MerchantCategory category)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/merchants/{merchantId}/categories", category);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MerchantCategory>();
    }

    public async Task<MerchantCategory?> UpdateCategoryAsync(Guid merchantId, Guid categoryId, MerchantCategory category)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/merchants/{merchantId}/categories/{categoryId}", category);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MerchantCategory>();
    }

    public async Task DeleteCategoryAsync(Guid merchantId, Guid categoryId)
    {
        var response = await _httpClient.DeleteAsync($"api/merchants/{merchantId}/categories/{categoryId}");
        response.EnsureSuccessStatusCode();
    }

    // Dashboard stats
    public async Task<DashboardStats> GetDashboardStatsAsync(Guid merchantId)
    {
        var products = await GetProductsAsync(merchantId);
        var categories = await GetCategoriesAsync(merchantId);
        
        return new DashboardStats
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => p.IsActive),
            TotalCategories = categories.Count,
            PendingOrders = 0 // Placeholder - would need to integrate with Order service
        };
    }
}
