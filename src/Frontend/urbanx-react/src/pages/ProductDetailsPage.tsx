import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { catalogService } from '../services/api';
import type { Product } from '../types';
import { useCart } from '../context/useCart';
import { Star, Truck, Shield, RefreshCcw, ShoppingBag, ArrowLeft, Plus, Minus } from 'lucide-react';

const ProductDetailsPage = () => {
    const { id } = useParams<{ id: string }>();
    const { addToCart } = useCart();
    const [product, setProduct] = useState<Product | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [quantity, setQuantity] = useState(1);
    const [activeTab, setActiveTab] = useState('description');

    useEffect(() => {
        const fetchProduct = async () => {
            if (!id) return;
            setLoading(true);
            try {
                const data = await catalogService.getProductById(id) as Product;
                setProduct(data);
                setError(null);
            } catch (err) {
                console.error('Failed to fetch product details:', err);
                // Fallback mock data matching the Product interface
                setProduct({
                    id: id || '1',
                    name: 'Premium Wireless Headphones',
                    price: 299,
                    category: 'headphones',
                    imageUrl: 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&q=80',
                    description: 'Elevate your listening experience with our flagship Premium Wireless Headphones. Engineered for audiophiles, these headphones deliver precision sound, robust bass, and crystalline highs. Featuring advanced active noise cancellation (ANC), they let you immerse yourself in music, podcasts, or calls without distraction. The lightweight design and plush memory foam ear cushions ensure comfort during extended sessions. With up to 40 hours of battery life and fast-charging capabilities, they are the perfect companion for travel, work, or home relaxation.',
                    merchantId: 'm1',
                    inventoryCount: 10
                });
            } finally {
                setLoading(false);
            }
        };

        fetchProduct();
        window.scrollTo(0, 0);
    }, [id]);

    const handleAddToCart = () => {
        if (product) {
            addToCart(product, quantity);
        }
    };

    if (loading) return <div className="container mx-auto px-6 py-20 text-center text-gray-400">Loading product details...</div>;
    if (error || !product) {
        return (
            <div className="container mx-auto px-6 py-20 text-center">
                <h2 className="text-2xl font-serif font-bold mb-4">Opps!</h2>
                <p className="text-gray-500 mb-6">{error || 'Product not found.'}</p>
                <Link to="/catalog" className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-gray-950 disabled:pointer-events-none disabled:opacity-50">Back to Catalog</Link>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-6 py-12">
            <Link to="/catalog" className="inline-flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8 group">
                <ArrowLeft size={16} className="group-hover:-translate-x-1 transition-transform" /> Back to Catalog
            </Link>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-12 lg:gap-20">
                {/* Image Section */}
                <div className="relative">
                    <div className="sticky top-24 aspect-square bg-gray-50 rounded-2xl overflow-hidden border border-gray-100">
                        <img src={product.imageUrl} alt={product.name} className="w-full h-full object-cover" />
                    </div>
                </div>

                {/* Info Section */}
                <div>
                    <span className="block text-xs font-bold uppercase tracking-widest text-secondary mb-3">{product.category}</span>
                    <h1 className="text-3xl md:text-4xl font-serif font-bold text-gray-900 mb-4">{product.name}</h1>

                    <div className="flex items-center gap-4 mb-6">
                        <div className="flex text-secondary gap-0.5">
                            {[1, 2, 3, 4, 5].map((s) => (
                                <Star key={s} size={16} fill={s <= 4 ? "currentColor" : "none"} stroke="currentColor" />
                            ))}
                        </div>
                        <span className="text-sm text-gray-400">(48 Reviews)</span>
                    </div>

                    <div className="text-3xl font-bold text-primary mb-8">${product.price.toLocaleString()}</div>

                    <p className="text-gray-600 leading-relaxed mb-8 border-b border-gray-100 pb-8">
                        {product.description.substring(0, 150)}...
                    </p>

                    <div className="flex flex-col sm:flex-row gap-4 mb-10">
                        <div className="flex items-center border border-gray-200 rounded-md">
                            <button
                                onClick={() => setQuantity(Math.max(1, quantity - 1))}
                                className="w-10 h-10 flex items-center justify-center text-gray-500 hover:bg-gray-50 transition-colors"
                            >
                                <Minus size={16} />
                            </button>
                            <span className="w-12 text-center font-semibold text-gray-900">{quantity}</span>
                            <button
                                onClick={() => setQuantity(quantity + 1)}
                                className="w-10 h-10 flex items-center justify-center text-gray-500 hover:bg-gray-50 transition-colors"
                            >
                                <Plus size={16} />
                            </button>
                        </div>
                        <button
                            className="flex-1 flex items-center justify-center gap-2 btn-action-black h-12 rounded-md font-medium uppercase tracking-wide transition-colors shadow-lg shadow-gray-200"
                            onClick={handleAddToCart}
                        >
                            <ShoppingBag size={20} /> Add to Cart
                        </button>
                    </div>

                    <div className="grid grid-cols-2 gap-6 mb-12">
                        <div className="flex gap-3">
                            <Truck size={24} className="text-gray-400 shrink-0" />
                            <div>
                                <span className="block text-sm font-semibold text-gray-900">Free Shipping</span>
                                <p className="text-xs text-gray-500">On orders over $100</p>
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <Shield size={24} className="text-gray-400 shrink-0" />
                            <div>
                                <span className="block text-sm font-semibold text-gray-900">2-Year Warranty</span>
                                <p className="text-xs text-gray-500">Reliability guaranteed</p>
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <RefreshCcw size={24} className="text-gray-400 shrink-0" />
                            <div>
                                <span className="block text-sm font-semibold text-gray-900">30-Day Returns</span>
                                <p className="text-xs text-gray-500">Easy exchange policy</p>
                            </div>
                        </div>
                    </div>

                    {/* Tabs */}
                    <div className="mt-8">
                        <div className="flex gap-8 border-b border-gray-100 mb-6">
                            <button
                                className={`pb-4 text-sm font-semibold tracking-wide transition-colors relative ${activeTab === 'description' ? 'text-primary' : 'text-gray-400 hover:text-gray-600'
                                    }`}
                                onClick={() => setActiveTab('description')}
                            >
                                Description
                                {activeTab === 'description' && (
                                    <span className="absolute bottom-0 left-0 w-full h-0.5 bg-secondary"></span>
                                )}
                            </button>
                            <button
                                className={`pb-4 text-sm font-semibold tracking-wide transition-colors relative ${activeTab === 'specs' ? 'text-primary' : 'text-gray-400 hover:text-gray-600'
                                    }`}
                                onClick={() => setActiveTab('specs')}
                            >
                                Specifications
                                {activeTab === 'specs' && (
                                    <span className="absolute bottom-0 left-0 w-full h-0.5 bg-secondary"></span>
                                )}
                            </button>
                        </div>

                        <div className="text-gray-600 leading-relaxed text-sm">
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
