using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace UrbanX.Services.Catalog.Search;

public interface IProductSearchService
{
    Task<ProductSearchResult> SearchAsync(string? search, string? category, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductSearchResult> GetByMerchantAsync(Guid merchantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task IndexAsync(ProductDocument document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class ElasticsearchProductSearchService : IProductSearchService
{
    private const string IndexName = "catalog-products";

    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchProductSearchService> _logger;

    public ElasticsearchProductSearchService(ElasticsearchClient client, ILogger<ElasticsearchProductSearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ProductSearchResult> SearchAsync(string? search, string? category, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var must = new List<Query>
        {
            new TermQuery { Field = "isActive", Value = true }
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            must.Add(new MultiMatchQuery
            {
                Fields = Fields.FromStrings(["name", "description"]),
                Query = search
            });
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            must.Add(new TermQuery { Field = "category.keyword", Value = category });
        }

        var response = await _client.SearchAsync<ProductDocument>(s => s
            .Indices(IndexName)
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q.Bool(b => b.Must(must.ToArray()))),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Elasticsearch search returned invalid response: {DebugInfo}", response.DebugInformation);
            return new ProductSearchResult { Products = [], Total = 0, Page = page, PageSize = pageSize };
        }

        return new ProductSearchResult
        {
            Products = response.Documents.ToList().AsReadOnly(),
            Total = response.Total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductSearchResult> GetByMerchantAsync(Guid merchantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var must = new List<Query>
        {
            new TermQuery { Field = "isActive", Value = true },
            new TermQuery { Field = "merchantId", Value = merchantId.ToString() }
        };

        var response = await _client.SearchAsync<ProductDocument>(s => s
            .Indices(IndexName)
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q.Bool(b => b.Must(must.ToArray()))),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Elasticsearch merchant search returned invalid response: {DebugInfo}", response.DebugInformation);
            return new ProductSearchResult { Products = [], Total = 0, Page = page, PageSize = pageSize };
        }

        return new ProductSearchResult
        {
            Products = response.Documents.ToList().AsReadOnly(),
            Total = response.Total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync<ProductDocument>(id.ToString(), idx => idx.Index(IndexName), cancellationToken);

        if (!response.IsValidResponse || !response.Found)
        {
            return null;
        }

        return response.Source;
    }

    public async Task IndexAsync(ProductDocument document, CancellationToken cancellationToken = default)
    {
        var response = await _client.IndexAsync(document, idx => idx
            .Index(IndexName)
            .Id(document.Id.ToString()),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Failed to index product {ProductId}: {DebugInfo}", document.Id, response.DebugInformation);
            throw new InvalidOperationException($"Failed to index product {document.Id} in Elasticsearch.");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync(IndexName, id.ToString(), cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Failed to delete product {ProductId} from index: {DebugInfo}", id, response.DebugInformation);
            throw new InvalidOperationException($"Failed to delete product {id} from Elasticsearch index.");
        }
    }
}
