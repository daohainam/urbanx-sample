import React from 'react';
import { Link } from 'react-router-dom';
import { Plus } from 'lucide-react';
import type { Product } from '../../types';
import { useCart } from '../../context/useCart';

interface ProductCardProps {
    product: Product;
}

const ProductCard: React.FC<ProductCardProps> = ({ product }) => {
    const { addToCart } = useCart();

    const imageSrc =
        product.imageUrl ||
        'https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&q=80&w=800';

    return (
        <div className="group flex flex-col">
            <Link to={`/product/${product.id}`} className="block relative w-full aspect-[3/4] overflow-hidden bg-gray-100 rounded-sm mb-4">
                <img
                    src={imageSrc}
                    alt={product.name}
                    loading="lazy"
                    decoding="async"
                    className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
                />
                <div className="absolute inset-0 bg-black/0 group-hover:bg-black/10 transition-colors duration-300"></div>

                <div className="absolute bottom-4 right-4 translate-y-4 opacity-0 group-hover:translate-y-0 group-hover:opacity-100 transition-all duration-300">
                    <button
                        className="bg-white text-black hover:bg-black hover:text-white p-3 rounded-full shadow-lg transition-colors duration-200 flex items-center justify-center transform active:scale-95"
                        onClick={(e) => {
                            e.preventDefault();
                            addToCart(product);
                        }}
                        aria-label="Quick add to cart"
                    >
                        <Plus size={20} />
                    </button>
                </div>
            </Link>

            <div className="flex flex-col">
                <div className="text-xs text-gray-500 uppercase tracking-wider mb-1">{product.category}</div>
                <Link to={`/product/${product.id}`} className="text-base font-medium text-gray-900 hover:text-gray-600 transition-colors mb-1 line-clamp-1">
                    {product.name}
                </Link>
                <div className="text-sm font-semibold text-gray-900">
                    ${product.price.toLocaleString()}
                </div>
            </div>
        </div>
    );
};

export default ProductCard;
