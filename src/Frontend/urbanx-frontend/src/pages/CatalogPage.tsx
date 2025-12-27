import { useEffect, useState } from 'react';
import { Search, ShoppingBag } from 'lucide-react';
import { api } from '../lib/api';
import type { Product } from '../types';

export default function CatalogPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadProducts();
  }, []);

  const loadProducts = async () => {
    try {
      setLoading(true);
      const data = await api.getProducts(search);
      setProducts(data.products || []);
    } catch (error) {
      console.error('Failed to load products:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    loadProducts();
  };

  const handleAddToCart = async (product: Product) => {
    try {
      // Use a default customer ID for demo
      const customerId = '00000000-0000-0000-0000-000000000001';
      await api.addToCart(customerId, {
        productId: product.id,
        productName: product.name,
        quantity: 1,
        unitPrice: product.price,
        merchantId: product.merchantId,
      });
      alert('Added to cart!');
    } catch (error) {
      console.error('Failed to add to cart:', error);
      alert('Failed to add to cart');
    }
  };

  if (loading) {
    return (
      <div className="flex flex-col justify-center items-center h-screen">
        <div className="spinner mb-4"></div>
        <p className="text-neutral-600">Loading products...</p>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-6 py-10 animate-fade-in">
      <div className="mb-10">
        <h1 className="text-5xl font-bold mb-3 gradient-text">Discover Amazing Products</h1>
        <p className="text-neutral-600 text-lg">Find exactly what you're looking for</p>
      </div>
      
      <form onSubmit={handleSearch} className="mb-10">
        <div className="flex gap-3 max-w-2xl">
          <div className="flex-1 relative">
            <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-400" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search for products..."
              className="input pl-12 shadow-soft"
            />
          </div>
          <button
            type="submit"
            className="btn btn-primary px-8 font-semibold"
          >
            Search
          </button>
        </div>
      </form>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {products.map((product) => (
          <div key={product.id} className="card-elevated overflow-hidden group">
            <div className="h-56 bg-gradient-to-br from-neutral-100 to-neutral-200 flex items-center justify-center overflow-hidden relative">
              {product.imageUrl ? (
                <img 
                  src={product.imageUrl} 
                  alt={product.name} 
                  className="h-full w-full object-cover group-hover:scale-110 transition-transform duration-500" 
                />
              ) : (
                <span className="text-neutral-400 font-medium">No image</span>
              )}
              {product.stockQuantity < 10 && product.stockQuantity > 0 && (
                <div className="absolute top-3 right-3 badge bg-accent-500 text-white shadow-medium">
                  Only {product.stockQuantity} left
                </div>
              )}
            </div>
            <div className="p-5">
              <h3 className="text-lg font-semibold mb-2 text-neutral-800 line-clamp-1">{product.name}</h3>
              <p className="text-neutral-600 text-sm mb-4 line-clamp-2 min-h-[2.5rem]">{product.description}</p>
              <div className="flex justify-between items-center mb-4">
                <span className="text-3xl font-bold gradient-text">${product.price.toFixed(2)}</span>
                <span className="text-sm text-neutral-500 font-medium">{product.stockQuantity} in stock</span>
              </div>
              <button
                onClick={() => handleAddToCart(product)}
                disabled={product.stockQuantity === 0}
                className="btn btn-primary w-full disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100"
              >
                <ShoppingBag className="w-4 h-4" />
                {product.stockQuantity > 0 ? 'Add to Cart' : 'Out of Stock'}
              </button>
            </div>
          </div>
        ))}
      </div>

      {products.length === 0 && (
        <div className="text-center py-20">
          <div className="inline-flex items-center justify-center w-20 h-20 rounded-full bg-neutral-100 mb-4">
            <Search className="w-10 h-10 text-neutral-400" />
          </div>
          <p className="text-neutral-500 text-xl font-medium">No products found</p>
          <p className="text-neutral-400 mt-2">Try a different search term</p>
        </div>
      )}
    </div>
  );
}
