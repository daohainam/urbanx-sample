import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
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
      Pending: 'bg-yellow-500',
      PaymentReceived: 'bg-blue-500',
      Confirmed: 'bg-green-500',
      Preparing: 'bg-purple-500',
      ReadyForPickup: 'bg-indigo-500',
      InTransit: 'bg-cyan-500',
      Delivered: 'bg-green-600',
      Cancelled: 'bg-red-500',
    };
    return colors[status] || 'bg-gray-500';
  };

  if (loading) {
    return <div className="flex justify-center items-center h-screen">Loading...</div>;
  }

  if (!order) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-800 mb-4">Order Not Found</h1>
          <p className="text-gray-600">The order you're looking for doesn't exist.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-4xl font-bold mb-8">Order Tracking</h1>

      <div className="bg-white rounded-lg shadow-md p-6 mb-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
          <div>
            <h2 className="text-xl font-bold mb-2">Order Details</h2>
            <p className="text-gray-600">Order Number: <span className="font-semibold">{order.orderNumber}</span></p>
            <p className="text-gray-600">Order Date: <span className="font-semibold">{new Date(order.createdAt).toLocaleDateString()}</span></p>
            <p className="text-gray-600">Status: <span className={`inline-block px-3 py-1 rounded-full text-white text-sm ${getStatusColor(order.status)}`}>{order.status}</span></p>
          </div>
          <div>
            <h2 className="text-xl font-bold mb-2">Total Amount</h2>
            <p className="text-3xl font-bold text-blue-600">${order.totalAmount.toFixed(2)}</p>
          </div>
        </div>

        <div className="mb-6">
          <h2 className="text-xl font-bold mb-4">Shipping Address</h2>
          <p className="text-gray-600 whitespace-pre-line">{order.shippingAddress}</p>
        </div>

        <div className="mb-6">
          <h2 className="text-xl font-bold mb-4">Order Items</h2>
          <div className="space-y-3">
            {order.items.map((item) => (
              <div key={item.id} className="flex justify-between items-center border-b border-gray-200 pb-3">
                <div>
                  <h3 className="font-semibold">{item.productName}</h3>
                  <p className="text-gray-600 text-sm">Quantity: {item.quantity}</p>
                </div>
                <p className="font-bold">${(item.unitPrice * item.quantity).toFixed(2)}</p>
              </div>
            ))}
          </div>
        </div>

        <div>
          <h2 className="text-xl font-bold mb-4">Status Timeline</h2>
          <div className="space-y-4">
            {order.statusHistory
              .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
              .map((history) => (
                <div key={history.id} className="flex items-start">
                  <div className={`w-4 h-4 rounded-full ${getStatusColor(history.status)} mt-1 flex-shrink-0`}></div>
                  <div className="ml-4">
                    <p className="font-semibold">{history.status}</p>
                    <p className="text-gray-600 text-sm">{new Date(history.createdAt).toLocaleString()}</p>
                    {history.note && <p className="text-gray-600 text-sm">{history.note}</p>}
                  </div>
                </div>
              ))}
          </div>
        </div>
      </div>
    </div>
  );
}
