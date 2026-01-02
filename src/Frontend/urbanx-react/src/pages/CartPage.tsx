
import { useCart } from '../context/useCart';
import { ShoppingBag, X, Plus, Minus, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';

const CartPage = () => {
    const { items, removeFromCart, updateQuantity, totalPrice, totalItems, selectedItems, toggleItemSelection } = useCart();

    if (items.length === 0) {
        return (
            <div className="container mx-auto px-6 py-32 text-center">
                <ShoppingBag size={64} className="mx-auto text-gray-200 mb-6" />
                <h1 className="text-2xl font-serif font-bold text-gray-900 mb-3">Your cart is empty</h1>
                <p className="text-gray-500 mb-8">Looks like you haven't added anything to your cart yet.</p>
                <Link to="/catalog" className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900">
                    Start Shopping
                </Link>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-6 py-12">
            <h1 className="text-3xl font-serif font-bold text-gray-900 mb-8">Shopping Cart ({totalItems})</h1>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
                <div className="lg:col-span-2">
                    {/* Header - Desktop Only */}
                    <div className="hidden md:grid grid-cols-[24px_auto_1fr_120px_100px] gap-6 pb-4 border-b border-gray-200 text-xs font-bold uppercase tracking-wider text-gray-500">
                        <span></span>
                        <span>Product</span>
                        <span></span>
                        <span className="text-center">Quantity</span>
                        <span className="text-right">Total</span>
                    </div>




                    <div className="flex flex-col space-y-6 md:space-y-0 mt-4 md:mt-0">
                        {items.map(item => (
                            <div
                                key={item.id}
                                className={`relative flex flex-col md:grid md:grid-cols-[24px_80px_1fr_120px_100px] gap-4 md:gap-6 items-start md:items-center py-6 border-b border-gray-100 ${selectedItems.has(item.id) ? 'bg-yellow-50/20 -mx-4 px-4 rounded-lg' : ''}`}
                            >
                                {/* Mobile Header: Checkbox + Image + Basic Info */}
                                <div className="flex w-full gap-4 md:contents">
                                    <div className="flex items-start pt-2 md:pt-0">
                                        <input
                                            type="checkbox"
                                            checked={selectedItems.has(item.id)}
                                            onChange={() => toggleItemSelection(item.id)}
                                            className="w-5 h-5 text-primary border-gray-300 rounded focus:ring-primary cursor-pointer accent-primary"
                                        />
                                    </div>

                                    <div className="w-20 h-20 bg-gray-100 rounded-md overflow-hidden flex-shrink-0 md:w-full md:h-full md:aspect-square">
                                        <img src={item.imageUrl} alt={item.name} className="w-full h-full object-cover" />
                                    </div>

                                    <div className="flex-1 min-w-0 md:flex md:flex-col md:justify-between md:h-full md:py-1">
                                        <div>
                                            <Link to={`/product/${item.id}`} className="font-serif font-medium text-base md:text-lg text-gray-900 hover:text-secondary transition-colors line-clamp-2 md:line-clamp-1">{item.name}</Link>
                                            <div className="text-sm text-gray-500 mt-1 md:hidden">${item.price.toLocaleString()}</div>
                                        </div>
                                        <button
                                            className="text-xs text-red-500 hover:text-red-700 flex items-center gap-1 mt-auto w-fit uppercase font-medium tracking-wide opacity-70 hover:opacity-100 transition-opacity md:hidden pt-2"
                                            onClick={() => removeFromCart(item.id)}
                                        >
                                            <X size={12} /> Remove
                                        </button>
                                        <div className="hidden md:block text-sm text-gray-500 mt-1">${item.price.toLocaleString()}</div>
                                        <button
                                            className="hidden md:flex text-xs text-red-500 hover:text-red-700 items-center gap-1 mt-auto w-fit uppercase font-medium tracking-wide opacity-70 hover:opacity-100 transition-opacity"
                                            onClick={() => removeFromCart(item.id)}
                                        >
                                            <X size={12} /> Remove
                                        </button>
                                    </div>
                                </div>

                                {/* Mobile Bottom Row: Quantity + Line Total */}
                                <div className="flex items-center justify-between w-full pl-9 md:pl-0 md:contents">
                                    <div className="flex items-center border border-gray-200 rounded-md bg-white">
                                        <button onClick={() => updateQuantity(item.id, item.quantity - 1)} className="p-2 hover:bg-gray-50 text-gray-600"><Minus size={14} /></button>
                                        <span className="w-8 text-center text-sm font-semibold text-gray-900">{item.quantity}</span>
                                        <button onClick={() => updateQuantity(item.id, item.quantity + 1)} className="p-2 hover:bg-gray-50 text-gray-600"><Plus size={14} /></button>
                                    </div>
                                    <div className="text-lg font-bold text-gray-900 md:text-right">
                                        ${(item.price * item.quantity).toLocaleString()}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Sidebar */}
                <div className="lg:col-span-1">
                    <div className="bg-gray-50 p-6 md:p-8 rounded-lg sticky top-24">
                        <h3 className="text-xl font-serif font-bold text-gray-900 mb-6">Order Summary</h3>

                        <div className="flex justify-between text-sm text-gray-600 mb-4">
                            <span>Subtotal ({selectedItems.size} items)</span>
                            <span>${totalPrice.toLocaleString()}</span>
                        </div>
                        <div className="flex justify-between text-sm text-gray-600 mb-6">
                            <span>Shipping</span>
                            <span>Calculated at checkout</span>
                        </div>

                        <div className="flex justify-between items-center border-t border-gray-200 pt-6 mb-8">
                            <span className="font-bold text-gray-900">Estimated Total</span>
                            <span className="text-2xl font-bold text-primary">${totalPrice.toLocaleString()}</span>
                        </div>

                        <Link
                            to={selectedItems.size > 0 ? "/checkout" : "#"}
                            className={`flex w-full items-center justify-center gap-2 rounded-md bg-primary px-8 py-3 text-sm font-medium text-white shadow-sm transition-colors hover:bg-gray-900 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-gray-950 ${selectedItems.size === 0
                                ? 'opacity-50 cursor-not-allowed pointer-events-none bg-gray-400'
                                : ''
                                }`}
                        >
                            Checkout Selected <ArrowRight size={18} />
                        </Link>

                        {selectedItems.size === 0 && (
                            <p className="text-xs text-red-500 mt-3 text-center">Please select at least one item to checkout.</p>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default CartPage;
