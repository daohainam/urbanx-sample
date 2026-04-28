import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Filter, Search, X, ShoppingBag } from 'lucide-react';
import { catalogService } from '../services/api';
import ProductCard from '../components/product/ProductCard';
import { ProductCardSkeleton } from '../components/ui/Skeleton';
import { ErrorState } from '../components/ui/ErrorState';
import { EmptyState } from '../components/ui/EmptyState';
import { queryKeys } from '../lib/queryKeys';

const categories = [
    { name: 'All', slug: '' },
    { name: 'Headphones', slug: 'headphones' },
    { name: 'Watches', slug: 'watches' },
    { name: 'Home', slug: 'home' },
    { name: 'Electronics', slug: 'electronics' },
];

const CatalogPage = () => {
    const [searchParams, setSearchParams] = useSearchParams();
    const category = searchParams.get('category') || '';
    const search = searchParams.get('search') || '';

    const { data: products = [], isPending, isError, error, refetch, isFetching } = useQuery({
        queryKey: queryKeys.products.list({ category: category || undefined, search: search || undefined }),
        queryFn: ({ signal }) => catalogService.getProducts(category || undefined, search || undefined, signal),
    });

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

    const clearFilters = () => setSearchParams({});

    return (
        <div className="container mx-auto px-6 py-12 min-h-[80vh]">
            <title>{category ? `${category[0].toUpperCase()}${category.slice(1)} — UrbanX` : 'Shop all — UrbanX'}</title>
            <meta name="description" content="Browse our curated catalog of premium goods." />
            <div className="flex flex-col md:flex-row justify-between items-start md:items-end mb-12 border-b border-gray-100 pb-8 gap-6">
                <div>
                    <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Shop All</h1>
                    <p className="text-gray-500 text-sm">
                        {isPending ? 'Loading products…' : `${products.length} Products Found`}
                    </p>
                </div>
                <div className="w-full md:w-auto">
                    <label htmlFor="catalog-search" className="sr-only">Search products</label>
                    <div className="flex items-center bg-gray-50 border border-gray-200 px-4 py-2 rounded-md w-full md:w-[300px] focus-within:border-gray-400 focus-within:ring-1 focus-within:ring-gray-400 transition-all">
                        <Search size={18} className="text-gray-400 mr-2" aria-hidden="true" />
                        <input
                            id="catalog-search"
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
                        <h2 className="text-xs font-bold uppercase tracking-widest text-gray-900 mb-6 flex items-center gap-2">
                            <Filter size={14} aria-hidden="true" /> Categories
                        </h2>
                        <div className="flex flex-col gap-2">
                            {categories.map((cat) => (
                                <button
                                    key={cat.slug}
                                    type="button"
                                    aria-pressed={category === cat.slug}
                                    className={`text-left py-2 text-sm transition-all duration-200 ${
                                        category === cat.slug
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
                            type="button"
                            className="flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-gray-400 hover:text-red-600 transition-colors mt-4 self-start"
                            onClick={clearFilters}
                        >
                            <X size={14} aria-hidden="true" /> Clear all filters
                        </button>
                    )}
                </aside>

                <div className="md:hidden mb-6">
                    <div className="flex gap-2 overflow-x-auto pb-2 no-scrollbar">
                        {categories.map((cat) => (
                            <button
                                key={cat.slug}
                                type="button"
                                aria-pressed={category === cat.slug}
                                className={`whitespace-nowrap px-4 py-2 rounded-full text-sm font-medium border ${
                                    category === cat.slug
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

                <main aria-busy={isFetching}>
                    {isPending ? (
                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-x-8 gap-y-12" aria-label="Loading products">
                            {Array.from({ length: 6 }).map((_, i) => (
                                <ProductCardSkeleton key={i} />
                            ))}
                        </div>
                    ) : isError ? (
                        <ErrorState
                            error={error}
                            onRetry={() => refetch()}
                            title="Couldn't load products"
                        />
                    ) : products.length === 0 ? (
                        <EmptyState
                            icon={<ShoppingBag size={48} />}
                            title="No products found"
                            description="Try removing filters or searching for something else."
                            action={
                                (category || search) ? (
                                    <button
                                        type="button"
                                        onClick={clearFilters}
                                        className="text-secondary hover:underline text-sm font-medium"
                                    >
                                        Clear filters
                                    </button>
                                ) : undefined
                            }
                        />
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
