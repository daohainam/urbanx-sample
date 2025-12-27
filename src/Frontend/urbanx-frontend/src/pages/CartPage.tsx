import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
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
    return <div className="flex justify-center items-center h-screen">Loading...</div>;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-4xl font-bold mb-8">Shopping Cart</h1>

      {cart && cart.items.length > 0 ? (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2">
            {cart.items.map((item) => (
              <div key={item.id} className="bg-white rounded-lg shadow-md p-6 mb-4 flex items-center justify-between">
                <div className="flex-1">
                  <h3 className="text-lg font-semibold mb-2">{item.productName}</h3>
                  <p className="text-gray-600">Quantity: {item.quantity}</p>
                  <p className="text-blue-600 font-bold">${item.unitPrice.toFixed(2)} each</p>
                </div>
                <div className="text-right">
                  <p className="text-2xl font-bold mb-4">${(item.unitPrice * item.quantity).toFixed(2)}</p>
                  <button
                    onClick={() => handleRemoveItem(item.id)}
                    className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition"
                  >
                    Remove
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="lg:col-span-1">
            <div className="bg-white rounded-lg shadow-md p-6 sticky top-4">
              <h2 className="text-2xl font-bold mb-4">Order Summary</h2>
              <div className="border-t border-gray-200 pt-4 mb-4">
                <div className="flex justify-between mb-2">
                  <span>Subtotal:</span>
                  <span>${calculateTotal().toFixed(2)}</span>
                </div>
                <div className="flex justify-between mb-2">
                  <span>Shipping:</span>
                  <span>Free</span>
                </div>
                <div className="flex justify-between font-bold text-xl border-t border-gray-200 pt-4">
                  <span>Total:</span>
                  <span className="text-blue-600">${calculateTotal().toFixed(2)}</span>
                </div>
              </div>
              <button
                onClick={handleCheckout}
                className="w-full px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-semibold"
              >
                Proceed to Checkout
              </button>
            </div>
          </div>
        </div>
      ) : (
        <div className="text-center py-12">
          <p className="text-gray-500 text-xl mb-4">Your cart is empty</p>
          <button
            onClick={() => navigate('/')}
            className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
          >
            Continue Shopping
          </button>
        </div>
      )}
    </div>
  );
}
