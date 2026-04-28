import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Star, Truck, Shield, RefreshCcw, ShoppingBag, ArrowLeft, Plus, Minus } from 'lucide-react';
import { catalogService } from '../services/api';
import { useCart } from '../context/useCart';
import { Skeleton } from '../components/ui/Skeleton';
import { ErrorState } from '../components/ui/ErrorState';
import { queryKeys } from '../lib/queryKeys';

const ProductDetailsPage = () => {
    const { id } = useParams<{ id: string }>();
    const { addToCart } = useCart();
    const [quantity, setQuantity] = useState(1);
    const [activeTab, setActiveTab] = useState<'description' | 'specs'>('description');

    const { data: product, isPending, isError, error, refetch } = useQuery({
        queryKey: id ? queryKeys.products.detail(id) : ['products', 'detail', 'missing'],
        queryFn: ({ signal }) => catalogService.getProductById(id as string, signal),
        enabled: Boolean(id),
    });

    useEffect(() => {
        window.scrollTo(0, 0);
    }, [id]);

    if (!id) {
        return (
            <div className="container mx-auto px-6 py-20 text-center">
                <ErrorState error={new Error('Missing product id')} title="Product not found" />
                <div className="mt-6">
                    <Link to="/catalog" className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900">
                        Back to Catalog
                    </Link>
                </div>
            </div>
        );
    }

    if (isPending) {
        return (
            <div className="container mx-auto px-6 py-12">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-12 lg:gap-20">
                    <Skeleton className="aspect-square rounded-2xl" aria-label="Loading product image" />
                    <div className="space-y-4">
                        <Skeleton className="h-4 w-24" />
                        <Skeleton className="h-10 w-3/4" />
                        <Skeleton className="h-6 w-32" />
                        <Skeleton className="h-8 w-40" />
                        <Skeleton className="h-32 w-full" />
                        <Skeleton className="h-12 w-full" />
                    </div>
                </div>
            </div>
        );
    }

    if (isError || !product) {
        return (
            <div className="container mx-auto px-6 py-20">
                <ErrorState
                    error={error}
                    onRetry={() => refetch()}
                    title="Couldn't load this product"
                />
                <div className="text-center mt-6">
                    <Link to="/catalog" className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900">
                        Back to Catalog
                    </Link>
                </div>
            </div>
        );
    }

    const handleAddToCart = () => addToCart(product, quantity);
    const descriptionPreview =
        product.description.length > 150 ? `${product.description.substring(0, 150)}…` : product.description;

    return (
        <div className="container mx-auto px-6 py-12">
            <title>{`${product.name} — UrbanX`}</title>
            <meta name="description" content={product.description.slice(0, 160)} />
            <meta property="og:title" content={product.name} />
            <meta property="og:image" content={product.imageUrl} />
            <Link to="/catalog" className="inline-flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8 group">
                <ArrowLeft size={16} className="group-hover:-translate-x-1 transition-transform" /> Back to Catalog
            </Link>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-12 lg:gap-20">
                <div className="relative">
                    <div className="sticky top-24 aspect-square bg-gray-50 rounded-2xl overflow-hidden border border-gray-100">
                        <img src={product.imageUrl} alt={product.name} className="w-full h-full object-cover" />
                    </div>
                </div>

                <div>
                    <span className="block text-xs font-bold uppercase tracking-widest text-secondary mb-3">{product.category}</span>
                    <h1 className="text-3xl md:text-4xl font-serif font-bold text-gray-900 mb-4">{product.name}</h1>

                    <div className="flex items-center gap-4 mb-6">
                        <div className="flex text-secondary gap-0.5" aria-label="4 out of 5 stars">
                            {[1, 2, 3, 4, 5].map((s) => (
                                <Star key={s} size={16} fill={s <= 4 ? 'currentColor' : 'none'} stroke="currentColor" aria-hidden="true" />
                            ))}
                        </div>
                        <span className="text-sm text-gray-400">(48 Reviews)</span>
                    </div>

                    <div className="text-3xl font-bold text-primary mb-8">${product.price.toLocaleString()}</div>

                    <p className="text-gray-600 leading-relaxed mb-8 border-b border-gray-100 pb-8">
                        {descriptionPreview}
                    </p>

                    <div className="flex flex-col sm:flex-row gap-4 mb-10">
                        <div className="flex items-center border border-gray-200 rounded-md" role="group" aria-label="Quantity">
                            <button
                                type="button"
                                aria-label="Decrease quantity"
                                onClick={() => setQuantity(Math.max(1, quantity - 1))}
                                className="w-10 h-10 flex items-center justify-center text-gray-500 hover:bg-gray-50 transition-colors"
                            >
                                <Minus size={16} aria-hidden="true" />
                            </button>
                            <span className="w-12 text-center font-semibold text-gray-900" aria-live="polite">{quantity}</span>
                            <button
                                type="button"
                                aria-label="Increase quantity"
                                onClick={() => setQuantity(quantity + 1)}
                                className="w-10 h-10 flex items-center justify-center text-gray-500 hover:bg-gray-50 transition-colors"
                            >
                                <Plus size={16} aria-hidden="true" />
                            </button>
                        </div>
                        <button
                            type="button"
                            className="flex-1 flex items-center justify-center gap-2 btn-action-black h-12 rounded-md font-medium uppercase tracking-wide transition-colors shadow-lg shadow-gray-200"
                            onClick={handleAddToCart}
                        >
                            <ShoppingBag size={20} aria-hidden="true" /> Add to Cart
                        </button>
                    </div>

                    <div className="grid grid-cols-2 gap-6 mb-12">
                        <div className="flex gap-3">
                            <Truck size={24} className="text-gray-400 shrink-0" aria-hidden="true" />
                            <div>
                                <span className="block text-sm font-semibold text-gray-900">Free Shipping</span>
                                <p className="text-xs text-gray-500">On orders over $100</p>
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <Shield size={24} className="text-gray-400 shrink-0" aria-hidden="true" />
                            <div>
                                <span className="block text-sm font-semibold text-gray-900">2-Year Warranty</span>
                                <p className="text-xs text-gray-500">Reliability guaranteed</p>
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <RefreshCcw size={24} className="text-gray-400 shrink-0" aria-hidden="true" />
                            <div>
                                <span className="block text-sm font-semibold text-gray-900">30-Day Returns</span>
                                <p className="text-xs text-gray-500">Easy exchange policy</p>
                            </div>
                        </div>
                    </div>

                    <div className="mt-8">
                        <div className="flex gap-8 border-b border-gray-100 mb-6" role="tablist">
                            <button
                                type="button"
                                role="tab"
                                aria-selected={activeTab === 'description'}
                                className={`pb-4 text-sm font-semibold tracking-wide transition-colors relative ${activeTab === 'description' ? 'text-primary' : 'text-gray-400 hover:text-gray-600'}`}
                                onClick={() => setActiveTab('description')}
                            >
                                Description
                                {activeTab === 'description' && (
                                    <span className="absolute bottom-0 left-0 w-full h-0.5 bg-secondary" aria-hidden="true"></span>
                                )}
                            </button>
                            <button
                                type="button"
                                role="tab"
                                aria-selected={activeTab === 'specs'}
                                className={`pb-4 text-sm font-semibold tracking-wide transition-colors relative ${activeTab === 'specs' ? 'text-primary' : 'text-gray-400 hover:text-gray-600'}`}
                                onClick={() => setActiveTab('specs')}
                            >
                                Specifications
                                {activeTab === 'specs' && (
                                    <span className="absolute bottom-0 left-0 w-full h-0.5 bg-secondary" aria-hidden="true"></span>
                                )}
                            </button>
                        </div>

                        <div className="text-gray-600 leading-relaxed text-sm" role="tabpanel">
                            {activeTab === 'description' ? (
                                <p>{product.description}</p>
                            ) : (
                                <table className="w-full max-w-sm text-left">
                                    <tbody>
                                        <tr className="border-b border-gray-100">
                                            <td className="py-2 text-gray-500 font-medium">Weight</td>
                                            <td className="py-2 text-gray-900">250g</td>
                                        </tr>
                                        <tr className="border-b border-gray-100">
                                            <td className="py-2 text-gray-500 font-medium">Dimensions</td>
                                            <td className="py-2 text-gray-900">18 x 15 x 8 cm</td>
                                        </tr>
                                        <tr className="border-b border-gray-100">
                                            <td className="py-2 text-gray-500 font-medium">Material</td>
                                            <td className="py-2 text-gray-900">Premium Aluminum</td>
                                        </tr>
                                        <tr className="border-b border-gray-100">
                                            <td className="py-2 text-gray-500 font-medium">Connectivity</td>
                                            <td className="py-2 text-gray-900">Bluetooth 5.2</td>
                                        </tr>
                                    </tbody>
                                </table>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ProductDetailsPage;
