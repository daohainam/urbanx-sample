using UrbanX.Frontend.Client.Models;

namespace UrbanX.Frontend.Client.Services;

public sealed class CartState
{
    private readonly List<CartItem> _items = new();

    public event Action? Changed;

    public IReadOnlyList<CartItem> Items => _items;

    public int TotalItems => _items.Sum(i => i.Quantity);

    public decimal TotalPrice => _items.Sum(i => i.Quantity * i.Product.Price);

    public void Add(Product product, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        var existing = _items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _items.Add(new CartItem(product, quantity));
        }

        Changed?.Invoke();
    }

    public void Remove(Guid productId)
    {
        var idx = _items.FindIndex(i => i.Product.Id == productId);
        if (idx >= 0)
        {
            _items.RemoveAt(idx);
            Changed?.Invoke();
        }
    }

    public void SetQuantity(Guid productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.Product.Id == productId);
        if (item is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            Remove(productId);
            return;
        }

        item.Quantity = quantity;
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        Changed?.Invoke();
    }

    public sealed class CartItem
    {
        public CartItem(Product product, int quantity)
        {
            Product = product;
            Quantity = quantity;
        }

        public Product Product { get; }
        public int Quantity { get; set; }
    }
}
