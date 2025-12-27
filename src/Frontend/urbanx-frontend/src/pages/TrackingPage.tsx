import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { MapPin, Package, Calendar, Clock, CheckCircle2 } from 'lucide-react';
import { api } from '../lib/api';
import type { Order } from '../types';

export default function TrackingPage() {
  const { orderId } = useParams<{ orderId: string }>();
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (orderId) {
      loadOrder();
    }
  }, [orderId]);

  const loadOrder = async () => {
    try {
      setLoading(true);
      const data = await api.getOrder(orderId!);
      setOrder(data);
    } catch (error) {
      console.error('Failed to load order:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Pending: 'from-yellow-400 to-yellow-500',
      PaymentReceived: 'from-blue-400 to-blue-500',
      Confirmed: 'from-green-400 to-green-500',
      Preparing: 'from-purple-400 to-purple-500',
      ReadyForPickup: 'from-indigo-400 to-indigo-500',
      InTransit: 'from-cyan-400 to-cyan-500',
      Delivered: 'from-green-500 to-green-600',
      Cancelled: 'from-red-400 to-red-500',
    };
    return colors[status] || 'from-neutral-400 to-neutral-500';
  };

  if (loading) {
    return (
      <div className="flex flex-col justify-center items-center h-screen">
        <div className="spinner mb-4"></div>
        <p className="text-neutral-600">Loading order details...</p>
      </div>
    );
  }

  if (!order) {
    return (
      <div className="container mx-auto px-6 py-20 text-center">
        <div className="inline-flex items-center justify-center w-24 h-24 rounded-full bg-gradient-to-br from-neutral-100 to-neutral-200 mb-6">
          <Package className="w-12 h-12 text-neutral-400" />
        </div>
        <h1 className="text-3xl font-bold text-neutral-800 mb-3">Order Not Found</h1>
        <p className="text-neutral-600">The order you're looking for doesn't exist.</p>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-6 py-10 animate-fade-in">
      <div className="mb-10">
        <h1 className="text-5xl font-bold mb-3 gradient-text">Order Tracking</h1>
        <p className="text-neutral-600 text-lg">Track your order status in real-time</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
        <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-12 h-12 rounded-full bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center">
              <Package className="w-6 h-6 text-white" />
            </div>
            <div>
              <p className="text-sm text-neutral-500">Order Number</p>
              <p className="text-xl font-bold text-neutral-800">{order.orderNumber}</p>
            </div>
          </div>
        </div>

        <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-12 h-12 rounded-full bg-gradient-to-br from-accent-500 to-accent-600 flex items-center justify-center">
              <Calendar className="w-6 h-6 text-white" />
            </div>
            <div>
              <p className="text-sm text-neutral-500">Order Date</p>
              <p className="text-lg font-semibold text-neutral-800">
                {new Date(order.createdAt).toLocaleDateString('en-US', { 
                  year: 'numeric', 
                  month: 'short', 
                  day: 'numeric' 
                })}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6">
          <div className="flex items-center gap-3 mb-4">
            <div className={`w-12 h-12 rounded-full bg-gradient-to-br ${getStatusColor(order.status)} flex items-center justify-center`}>
              <CheckCircle2 className="w-6 h-6 text-white" />
            </div>
            <div>
              <p className="text-sm text-neutral-500">Status</p>
              <p className="text-lg font-semibold text-neutral-800">{order.status}</p>
            </div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6">
            <div className="flex items-center gap-3 mb-6">
              <MapPin className="w-6 h-6 text-primary-600" />
              <h2 className="text-2xl font-bold text-neutral-800">Shipping Address</h2>
            </div>
            <p className="text-neutral-700 whitespace-pre-line leading-relaxed bg-neutral-50 p-4 rounded-xl">
              {order.shippingAddress}
            </p>
          </div>

          <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6">
            <div className="flex items-center gap-3 mb-6">
              <Clock className="w-6 h-6 text-primary-600" />
              <h2 className="text-2xl font-bold text-neutral-800">Status Timeline</h2>
            </div>
            <div className="space-y-4">
              {order.statusHistory
                .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
                .map((history, index) => (
                  <div key={history.id} className="flex items-start gap-4">
                    <div className="relative">
                      <div className={`w-10 h-10 rounded-full bg-gradient-to-br ${getStatusColor(history.status)} flex items-center justify-center shadow-medium flex-shrink-0`}>
                        <CheckCircle2 className="w-5 h-5 text-white" />
                      </div>
                      {index < order.statusHistory.length - 1 && (
                        <div className="absolute top-10 left-1/2 -translate-x-1/2 w-0.5 h-8 bg-neutral-200"></div>
                      )}
                    </div>
                    <div className="flex-1 pt-1">
                      <p className="font-semibold text-neutral-800 text-lg">{history.status}</p>
                      <p className="text-neutral-500 text-sm mt-1">
                        {new Date(history.createdAt).toLocaleString('en-US', {
                          year: 'numeric',
                          month: 'short',
                          day: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit'
                        })}
                      </p>
                      {history.note && (
                        <p className="text-neutral-600 text-sm mt-2 bg-neutral-50 p-3 rounded-lg">{history.note}</p>
                      )}
                    </div>
                  </div>
                ))}
            </div>
          </div>
        </div>

        <div className="lg:col-span-1">
          <div className="rounded-2xl border border-neutral-200 bg-white shadow-md hover:shadow-lg transition-all duration-300 p-6 sticky top-24">
            <h2 className="text-2xl font-bold mb-6 text-neutral-800">Order Summary</h2>
            <div className="space-y-4 mb-6">
              {order.items.map((item) => (
                <div key={item.id} className="pb-4 border-b border-neutral-200 last:border-0">
                  <div className="flex justify-between items-start mb-2">
                    <h3 className="font-semibold text-neutral-800 flex-1">{item.productName}</h3>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-neutral-600 text-sm">Qty: {item.quantity}</span>
                    <span className="font-bold text-neutral-800">${(item.unitPrice * item.quantity).toFixed(2)}</span>
                  </div>
                </div>
              ))}
            </div>
            <div className="border-t border-neutral-200 pt-4">
              <div className="flex justify-between mb-3">
                <span className="text-xl font-bold text-neutral-800">Total:</span>
                <span className="text-3xl font-bold gradient-text">${order.totalAmount.toFixed(2)}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
