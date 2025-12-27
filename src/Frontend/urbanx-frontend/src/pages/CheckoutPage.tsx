import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { CreditCard, MapPin, Check } from 'lucide-react';
import { api } from '../lib/api';
import type { Cart } from '../types';

export default function CheckoutPage() {
  const navigate = useNavigate();
  const [cart, setCart] = useState<Cart | null>(null);
  const [shippingAddress, setShippingAddress] = useState('');
  const [loading, setLoading] = useState(false);
  
  const customerId = '00000000-0000-0000-0000-000000000001';

  useEffect(() => {
    loadCart();
  }, []);

  const loadCart = async () => {
    try {
      const data = await api.getCart(customerId);
      setCart(data);
    } catch (error) {
      console.error('Failed to load cart:', error);
    }
  };

  const calculateTotal = () => {
    if (!cart) return 0;
    return cart.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!cart || cart.items.length === 0) {
      alert('Your cart is empty');
      return;
    }

    if (!shippingAddress.trim()) {
      alert('Please enter a shipping address');
      return;
    }

    setLoading(true);
    try {
      // Create order
      const order = await api.createOrder({
        customerId,
        totalAmount: calculateTotal(),
        items: cart.items.map(item => ({
          productId: item.productId,
          productName: item.productName,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          merchantId: item.merchantId,
        })),
        shippingAddress,
      });

      // Process payment
      await api.processPayment({
        orderId: order.id,
        amount: calculateTotal(),
        method: 1, // CreditCard
      });

      alert('Order placed successfully!');
      navigate(`/tracking/${order.id}`);
    } catch (error) {
      console.error('Failed to place order:', error);
      alert('Failed to place order. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mx-auto px-6 py-10 animate-fade-in">
      <div className="mb-10">
        <h1 className="text-5xl font-bold mb-3 gradient-text">Checkout</h1>
        <p className="text-neutral-600 text-lg">Complete your order</p>
      </div>

      <form onSubmit={handleSubmit} className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-6">
          <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6">
            <div className="flex items-center gap-3 mb-6">
              <div className="w-10 h-10 rounded-full bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center">
                <MapPin className="w-5 h-5 text-white" />
              </div>
              <h2 className="text-2xl font-bold text-neutral-800">Shipping Information</h2>
            </div>
            <div>
              <label className="block text-neutral-700 mb-2 font-medium">Shipping Address</label>
              <textarea
                value={shippingAddress}
                onChange={(e) => setShippingAddress(e.target.value)}
                placeholder="Enter your complete shipping address"
                className="w-full rounded-xl border border-neutral-300 px-4 py-2.5 focus:outline-none focus:ring-2 focus:ring-primary-400 focus:border-primary-400 transition-all duration-200 bg-white placeholder:text-neutral-400 shadow-sm"
                rows={4}
                required
              />
            </div>
          </div>

          <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6">
            <div className="flex items-center gap-3 mb-6">
              <div className="w-10 h-10 rounded-full bg-gradient-to-br from-accent-500 to-accent-600 flex items-center justify-center">
                <CreditCard className="w-5 h-5 text-white" />
              </div>
              <h2 className="text-2xl font-bold text-neutral-800">Payment Method</h2>
            </div>
            <div className="mb-4">
              <label className="flex items-center p-4 border-2 border-primary-300 bg-primary-50/50 rounded-xl cursor-pointer">
                <input type="radio" name="payment" value="card" defaultChecked className="mr-3 w-4 h-4 text-primary-600" />
                <span className="font-medium text-neutral-800">Credit/Debit Card</span>
              </label>
            </div>
            <p className="text-sm text-neutral-500 flex items-center gap-2">
              <Check className="w-4 h-4 text-primary-600" />
              Payment will be processed securely
            </p>
          </div>
        </div>

        <div className="lg:col-span-1">
          <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6 sticky top-24">
            <h2 className="text-2xl font-bold mb-6 text-neutral-800">Order Summary</h2>
            {cart && cart.items.length > 0 ? (
              <>
                <div className="mb-6 max-h-64 overflow-y-auto">
                  {cart.items.map((item) => (
                    <div key={item.id} className="flex justify-between mb-3 pb-3 border-b border-neutral-200 last:border-0">
                      <span className="text-neutral-700 flex-1">
                        <span className="font-medium">{item.productName}</span>
                        <span className="text-neutral-500 ml-2">x {item.quantity}</span>
                      </span>
                      <span className="font-semibold text-neutral-800">${(item.unitPrice * item.quantity).toFixed(2)}</span>
                    </div>
                  ))}
                </div>
                <div className="space-y-3 mb-6">
                  <div className="flex justify-between text-neutral-600">
                    <span>Subtotal:</span>
                    <span className="font-semibold text-neutral-800">${calculateTotal().toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-neutral-600">
                    <span>Shipping:</span>
                    <span className="font-semibold text-primary-600">Free</span>
                  </div>
                  <div className="border-t border-neutral-200 pt-3 flex justify-between">
                    <span className="text-xl font-bold text-neutral-800">Total:</span>
                    <span className="text-3xl font-bold gradient-text">${calculateTotal().toFixed(2)}</span>
                  </div>
                </div>
                <button
                  type="submit"
                  disabled={loading}
                  className="inline-flex items-center justify-center gap-2 rounded-xl px-5 py-3 text-base font-semibold transition-all duration-300 bg-gradient-to-r from-primary-500 to-primary-600 text-white hover:from-primary-600 hover:to-primary-700 shadow-md hover:shadow-lg hover:scale-[1.02] active:scale-[0.98] border-0 w-full disabled:opacity-50 disabled:hover:scale-100"
                >
                  {loading ? (
                    <>
                      <div className="spinner"></div>
                      Processing...
                    </>
                  ) : (
                    <>
                      <Check className="w-5 h-5" />
                      Place Order
                    </>
                  )}
                </button>
              </>
            ) : (
              <p className="text-neutral-500 text-center py-8">Your cart is empty</p>
            )}
          </div>
        </div>
      </form>
    </div>
  );
}
