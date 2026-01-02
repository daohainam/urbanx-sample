import { ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import ProductCard from '../components/product/ProductCard';

const categories = [
    { id: '1', name: 'Electronics', slug: 'electronics', imageUrl: 'https://images.unsplash.com/photo-1498049794561-7780e7231661?auto=format&fit=crop&q=80&w=800' },
    { id: '2', name: 'Fashion', slug: 'fashion', imageUrl: 'https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&q=80&w=800' },
    { id: '3', name: 'Home & Living', slug: 'home', imageUrl: 'https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&q=80&w=800' },
];

const featuredProducts = [
    { id: 'p1', name: 'Premium Wireless Headphones', price: 299, category: 'Electronics', imageUrl: 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&q=80&w=800', description: '', merchantId: 'm1', inventoryCount: 10 },
    { id: 'p2', name: 'Leather Weekend Bag', price: 180, category: 'Fashion', imageUrl: 'https://images.unsplash.com/photo-1547949003-9792a18a2601?auto=format&fit=crop&q=80&w=800', description: '', merchantId: 'm2', inventoryCount: 5 },
    { id: 'p3', name: 'Minimalist Ceramic Vase', price: 45, category: 'Home', imageUrl: 'https://images.unsplash.com/photo-1581783898377-1c85bf937427?auto=format&fit=crop&q=80&w=800', description: '', merchantId: 'm3', inventoryCount: 20 },
    { id: 'p4', name: 'Smart Watch Series X', price: 399, category: 'Electronics', imageUrl: 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&q=80&w=800', description: '', merchantId: 'm1', inventoryCount: 15 },
];

const HomePage = () => {
    return (
        <div className="animate-fade-in">
            {/* Hero Section */}
            <section className="relative h-[80vh] w-full bg-gray-900 text-white overflow-hidden">
                <div
                    className="absolute inset-0 bg-cover bg-center"
                    style={{
                        backgroundImage: `url('https://images.unsplash.com/photo-1441986300917-64674bd600d8?auto=format&fit=crop&q=80&w=2000')`
                    }}
                >
                    <div className="absolute inset-0 bg-black/40"></div>
                </div>

                <div className="relative h-full container mx-auto px-6 flex flex-col justify-center items-center text-center z-10">
                    <h1 className="text-5xl md:text-7xl font-serif font-bold mb-6 leading-tight tracking-tight">
                        Modern Commerce, <br />Redefined.
                    </h1>
                    <p className="text-xl md:text-2xl text-gray-200 mb-10 max-w-2xl font-light">
                        Discover a curated collection of premium products from top merchants.
                    </p>
                    <div className="flex gap-4">
                        <Link to="/catalog" className="btn-action-white px-8 py-3 rounded text-sm font-medium uppercase tracking-widest transition-colors duration-200">
                            Shop Now
                        </Link>
                        <button className="border border-white text-white hover:bg-white hover:text-black px-8 py-3 rounded text-sm font-medium uppercase tracking-widest transition-colors duration-200">
                            Explore More
                        </button>
                    </div>
                </div>
            </section>

            {/* Categories Section */}
            <section className="py-24 bg-white">
                <div className="container mx-auto px-6">
                    <div className="flex justify-between items-end mb-12 border-b border-gray-100 pb-4">
                        <h2 className="text-3xl font-serif font-semibold text-gray-900">Shop by Category</h2>
                        <Link to="/catalog" className="text-sm font-medium text-gray-900 hover:text-gray-600 flex items-center gap-2 uppercase tracking-wider transition-colors">
                            View All <ArrowRight size={16} />
                        </Link>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                        {categories.map(cat => (
                            <Link key={cat.id} to={`/catalog?category=${cat.slug}`} className="group block relative h-[500px] overflow-hidden rounded-sm bg-gray-100">
                                <div
                                    className="absolute inset-0 bg-cover bg-center transition-transform duration-700 group-hover:scale-105"
                                    style={{ backgroundImage: `url(${cat.imageUrl})` }}
                                >
                                    <div className="absolute inset-0 bg-black/20 group-hover:bg-black/30 transition-colors duration-500"></div>
                                </div>
                                <div className="absolute bottom-0 left-0 w-full p-8 text-white z-10">
                                    <h3 className="text-3xl font-serif mb-2">{cat.name}</h3>
                                    <span className="inline-block text-xs font-bold uppercase tracking-[0.15em] border-b border-white/50 pb-1 group-hover:border-white transition-colors">
                                        Browse Selection
                                    </span>
                                </div>
                            </Link>
                        ))}
                    </div>
                </div>
            </section>

            {/* Featured Arrivals */}
            <section className="py-24 bg-gray-50">
                <div className="container mx-auto px-6">
                    <div className="flex justify-between items-end mb-12">
                        <h2 className="text-3xl font-serif font-semibold text-gray-900">Featured Arrivals</h2>
                        <Link to="/catalog" className="text-sm font-medium text-gray-900 hover:text-gray-600 flex items-center gap-2 uppercase tracking-wider transition-colors">
                            See All Products <ArrowRight size={16} />
                        </Link>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-x-8 gap-y-12">
                        {featuredProducts.map(product => (
                            <ProductCard key={product.id} product={product} />
                        ))}
                    </div>
                </div>
            </section>

            {/* Promo Banner */}
            <section className="py-24 container mx-auto px-6">
                <div className="relative bg-gray-900 rounded overflow-hidden flex flex-col md:flex-row min-h-[500px]">
                    <div className="flex-1 p-12 lg:p-24 flex flex-col justify-center items-start text-white z-10">
                        <span className="text-yellow-500 text-xs font-bold uppercase tracking-[0.2em] mb-6">Summer Collection</span>
                        <h2 className="text-4xl md:text-5xl font-serif font-bold mb-6 leading-tight">Exclusive Merchant Deals</h2>
                        <p className="text-gray-400 text-lg mb-10 max-w-md leading-relaxed">
                            Up to 40% off on selected artisan products. Handcrafted excellence delivered to your doorstep.
                        </p>
                        <Link to="/catalog" className="bg-yellow-600 hover:bg-yellow-700 text-white px-8 py-3 rounded text-sm font-medium uppercase tracking-widest transition-colors duration-200">
                            Claim Discount
                        </Link>
                    </div>
                    <div
                        className="flex-1 min-h-[300px] md:min-h-auto bg-cover bg-center"
                        style={{ backgroundImage: `url('https://images.unsplash.com/photo-1490481651871-ab68de25d43d?auto=format&fit=crop&q=80&w=1200')` }}
                    ></div>
                </div>
            </section>
        </div>
    );
};

export default HomePage;
