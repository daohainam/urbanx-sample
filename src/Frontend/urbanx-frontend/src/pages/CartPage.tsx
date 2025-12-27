import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Trash2, ShoppingBag, ArrowRight } from 'lucide-react';
import { api } from '../lib/api';
import type { Cart } from '../types';

export default function CartPage() {
  const navigate = useNavigate();
  const [cart, setCart] = useState<Cart | null>(null);
  const [loading, setLoading] = useState(true);
  
  // Use a default customer ID for demo
  const customerId = '00000000-0000-0000-0000-000000000001';

  useEffect(() => {
    loadCart();
  }, []);

  const loadCart = async () => {
    try {
      setLoading(true);
      const data = await api.getCart(customerId);
      setCart(data);
    } catch (error) {
      console.error('Failed to load cart:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveItem = async (itemId: string) => {
    try {
      await api.removeFromCart(customerId, itemId);
      loadCart();
    } catch (error) {
      console.error('Failed to remove item:', error);
      alert('Failed to remove item');
    }
  };

  const calculateTotal = () => {
    if (!cart) return 0;
    return cart.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
  };

  const handleCheckout = () => {
    navigate('/checkout');
  };

  if (loading) {
    return (
      <div className="flex flex-col justify-center items-center h-screen">
        <div className="spinner mb-4"></div>
        <p className="text-neutral-600">Loading cart...</p>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-6 py-10 animate-fade-in">
      <div className="mb-10">
        <h1 className="text-5xl font-bold mb-3 gradient-text">Shopping Cart</h1>
        <p className="text-neutral-600 text-lg">Review your items before checkout</p>
      </div>

      {cart && cart.items.length > 0 ? (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-4">
            {cart.items.map((item) => (
              <div key={item.id} className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6 flex items-center gap-6 group">
                <div className="flex-1">
                  <h3 className="text-xl font-semibold mb-2 text-neutral-800">{item.productName}</h3>
                  <div className="flex items-center gap-4 text-neutral-600">
                    <span className="text-sm">Quantity: <span className="font-semibold text-neutral-800">{item.quantity}</span></span>
                    <span className="text-lg font-bold text-primary-600">${item.unitPrice.toFixed(2)} <span className="text-sm text-neutral-500 font-normal">each</span></span>
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-3xl font-bold gradient-text mb-4">${(item.unitPrice * item.quantity).toFixed(2)}</p>
                  <button
                    onClick={() => handleRemoveItem(item.id)}
                    className="inline-flex items-center justify-center gap-2 rounded-xl px-5 py-2.5 text-sm font-medium transition-all duration-300 border border-accent-300 bg-white text-accent-600 hover:bg-accent-50 hover:border-accent-400 shadow-sm hover:shadow-md hover:scale-[1.02] active:scale-[0.98] group-hover:shadow-md"
                  >
                    <Trash2 className="w-4 h-4" />
                    Remove
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="lg:col-span-1">
            <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6 sticky top-24">
              <h2 className="text-2xl font-bold mb-6 text-neutral-800">Order Summary</h2>
              <div className="space-y-4 mb-6">
                <div className="flex justify-between text-neutral-600">
                  <span>Subtotal:</span>
                  <span className="font-semibold text-neutral-800">${calculateTotal().toFixed(2)}</span>
                </div>
                <div className="flex justify-between text-neutral-600">
                  <span>Shipping:</span>
                  <span className="font-semibold text-primary-600">Free</span>
                </div>
                <div className="border-t border-neutral-200 pt-4 flex justify-between">
                  <span className="text-xl font-bold text-neutral-800">Total:</span>
                  <span className="text-3xl font-bold gradient-text">${calculateTotal().toFixed(2)}</span>
                </div>
              </div>
              <button
                onClick={handleCheckout}
                className="inline-flex items-center justify-center gap-2 rounded-xl px-5 py-3 text-base font-semibold transition-all duration-300 bg-gradient-to-r from-primary-500 to-primary-600 text-white hover:from-primary-600 hover:to-primary-700 shadow-md hover:shadow-lg hover:scale-[1.02] active:scale-[0.98] border-0 w-full"
              >
                Proceed to Checkout
                <ArrowRight className="w-5 h-5" />
              </button>
            </div>
          </div>
        </div>
      ) : (
        <div className="text-center py-20">
          <div className="inline-flex items-center justify-center w-24 h-24 rounded-full bg-gradient-to-br from-neutral-100 to-neutral-200 mb-6">
            <ShoppingBag className="w-12 h-12 text-neutral-400" />
          </div>
          <p className="text-neutral-500 text-2xl font-semibold mb-3">Your cart is empty</p>
          <p className="text-neutral-400 mb-6">Discover amazing products to add to your cart</p>
          <button
            onClick={() => navigate('/')}
            className="inline-flex items-center justify-center gap-2 rounded-xl px-8 py-2.5 text-base font-medium transition-all duration-300 bg-gradient-to-r from-primary-500 to-primary-600 text-white hover:from-primary-600 hover:to-primary-700 shadow-md hover:shadow-lg hover:scale-[1.02] active:scale-[0.98] border-0"
          >
            Continue Shopping
          </button>
        </div>
      )}
    </div>
  );
}
