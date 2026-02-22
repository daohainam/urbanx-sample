import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { catalogService } from '../services/api';
import type { Product } from '../types';
import ProductCard from '../components/product/ProductCard';
import { Filter, Search, X } from 'lucide-react';

const categories = [
    { name: 'All', slug: '' },
    { name: 'Headphones', slug: 'headphones' },
    { name: 'Watches', slug: 'watches' },
    { name: 'Home', slug: 'home' },
    { name: 'Electronics', slug: 'electronics' },
];

const CatalogPage = () => {
    const [searchParams, setSearchParams] = useSearchParams();
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const category = searchParams.get('category') || '';
    const search = searchParams.get('search') || '';

    useEffect(() => {
        const fetchProducts = async () => {
            setLoading(true);
            try {
                const data = await catalogService.getProducts(category, search) as Product[];
                setProducts(data);
                setError(null);
            } catch (err) {
                console.error('Failed to fetch products:', err);
                setError('Failed to load products. Please try again later.');
                // Fallback mock data matching the Product interface
                const mockProducts: Product[] = [
                    { id: '1', name: 'Premium Wireless Headphones', price: 299, category: 'headphones', imageUrl: 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&q=80', description: 'Experience studio-quality sound with our premium wireless headphones.', merchantId: 'm1', stockQuantity: 10 },
                    { id: '2', name: 'Minimalist Ceramic Vase', price: 45, category: 'home', imageUrl: 'https://images.unsplash.com/photo-1581783898377-1c85bf937427?w=500&q=80', description: 'A sleek, modern vase for any contemporary home.', merchantId: 'm1', stockQuantity: 5 },
                    { id: '3', name: 'Smart Fitness Tracker', price: 129, category: 'electronics', imageUrl: 'https://images.unsplash.com/photo-1575311373937-040b8e1fd5b6?w=500&q=80', description: 'Track your fitness goals with precision and style.', merchantId: 'm2', stockQuantity: 20 },
                    { id: '4', name: 'Luxury Leather Watch', price: 199, category: 'watches', imageUrl: 'https://images.unsplash.com/photo-1524592094714-0f0654e20314?w=500&q=80', description: 'A timeless piece of craftsmanship for your wrist.', merchantId: 'm2', stockQuantity: 8 },
                    { id: '5', name: 'Portable Bluetooth Speaker', price: 89, category: 'electronics', imageUrl: 'https://images.unsplash.com/photo-1608156639585-34a0a56ee6c9?w=500&q=80', description: 'Room-filling sound in a compact, portable design.', merchantId: 'm1', stockQuantity: 15 },
                    { id: '6', name: 'Organic Cotton Throw', price: 65, category: 'home', imageUrl: 'https://images.unsplash.com/photo-1580302200322-22443015403e?w=500&q=80', description: 'Soft, breathable throw made from 100% organic cotton.', merchantId: 'm3', stockQuantity: 12 },
                ];
                setProducts(mockProducts.filter(p => !category || p.category === category));
            } finally {
                setLoading(false);
            }
        };

        fetchProducts();
    }, [category, search]);

    const handleCategoryChange = (slug: string) => {
        if (slug) {
            searchParams.set('category', slug);
        } else {
            searchParams.delete('category');
        }
        setSearchParams(searchParams);
    };

    const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value;
        if (value) {
            searchParams.set('search', value);
        } else {
            searchParams.delete('search');
        }
        setSearchParams(searchParams);
    };

    const clearFilters = () => {
        setSearchParams({});
    };

    return (
        <div className="container mx-auto px-6 py-12 min-h-[80vh]">
            <div className="flex flex-col md:flex-row justify-between items-start md:items-end mb-12 border-b border-gray-100 pb-8 gap-6">
                <div>
                    <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Shop All</h1>
                    <p className="text-gray-500 text-sm">{products.length} Products Found</p>
                </div>
                <div className="w-full md:w-auto">
                    <div className="flex items-center bg-gray-50 border border-gray-200 px-4 py-2 rounded-md w-full md:w-[300px] focus-within:border-gray-400 focus-within:ring-1 focus-within:ring-gray-400 transition-all">
                        <Search size={18} className="text-gray-400 mr-2" />
                        <input
                            type="text"
                            placeholder="Search products..."
                            value={search}
                            onChange={handleSearchChange}
                            className="bg-transparent border-none outline-none text-sm w-full placeholder:text-gray-400"
                        />
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-[240px_1fr] gap-12">
                <aside className="hidden md:flex flex-col gap-8">
                    <div>
                        <h3 className="text-xs font-bold uppercase tracking-widest text-gray-900 mb-6 flex items-center gap-2">
                            <Filter size={14} /> Categories
                        </h3>
                        <div className="flex flex-col gap-2">
                            {categories.map((cat) => (
                                <button
                                    key={cat.slug}
                                    className={`text-left py-2 text-sm transition-all duration-200 ${category === cat.slug
                                            ? 'text-secondary font-semibold pl-2 border-l-2 border-secondary'
                                            : 'text-gray-500 hover:text-primary hover:pl-2'
                                        }`}
                                    onClick={() => handleCategoryChange(cat.slug)}
                                >
                                    {cat.name}
                                </button>
                            ))}
                        </div>
                    </div>

                    {(category || search) && (
                        <button
                            className="flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-gray-400 hover:text-red-600 transition-colors mt-4 self-start"
                            onClick={clearFilters}
                        >
                            <X size={14} /> Clear all filters
                        </button>
                    )}
                </aside>

                {/* Mobile Filter Toggle (Visible only on mobile) */}
                <div className="md:hidden mb-6">
                    <div className="flex gap-2 overflow-x-auto pb-2 no-scrollbar">
                        {categories.map((cat) => (
                            <button
                                key={cat.slug}
                                className={`whitespace-nowrap px-4 py-2 rounded-full text-sm font-medium border ${category === cat.slug
                                        ? 'bg-primary text-white border-primary'
                                        : 'bg-white text-gray-700 border-gray-200'
                                    }`}
                                onClick={() => handleCategoryChange(cat.slug)}
                            >
                                {cat.name}
                            </button>
                        ))}
                    </div>
                </div>

                <main>
                    {loading ? (
                        <div className="py-20 text-center text-gray-400">Loading products...</div>
                    ) : error && products.length === 0 ? (
                        <div className="py-20 text-center text-red-500 bg-red-50 rounded-lg">{error}</div>
                    ) : products.length === 0 ? (
                        <div className="py-20 text-center text-gray-500 bg-gray-50 rounded-lg">
                            <p className="mb-2">No products found matching your criteria.</p>
                            <button onClick={clearFilters} className="text-secondary hover:underline text-sm font-medium">Clear filters</button>
                        </div>
                    ) : (
                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-x-8 gap-y-12">
                            {products.map((product) => (
                                <ProductCard key={product.id} product={product} />
                            ))}
                        </div>
                    )}
                </main>
            </div>
        </div>
    );
};

export default CatalogPage;
