import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../lib/api';
import type { Order } from '../types';

export default function OrdersPage() {
  const navigate = useNavigate();
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  
  const customerId = '00000000-0000-0000-0000-000000000001';

  useEffect(() => {
    loadOrders();
  }, []);

  const loadOrders = async () => {
    try {
      setLoading(true);
      const data = await api.getCustomerOrders(customerId);
      setOrders(data);
    } catch (error) {
      console.error('Failed to load orders:', error);
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

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-4xl font-bold mb-8">My Orders</h1>

      {orders.length > 0 ? (
        <div className="space-y-4">
          {orders.map((order) => (
            <div key={order.id} className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition cursor-pointer"
                 onClick={() => navigate(`/tracking/${order.id}`)}>
              <div className="flex justify-between items-start mb-4">
                <div>
                  <h3 className="text-xl font-bold mb-2">{order.orderNumber}</h3>
                  <p className="text-gray-600">Order Date: {new Date(order.createdAt).toLocaleDateString()}</p>
                </div>
                <div className="text-right">
                  <span className={`inline-block px-3 py-1 rounded-full text-white text-sm ${getStatusColor(order.status)}`}>
                    {order.status}
                  </span>
                  <p className="text-2xl font-bold text-blue-600 mt-2">${order.totalAmount.toFixed(2)}</p>
                </div>
              </div>
              <div className="border-t border-gray-200 pt-4">
                <p className="text-gray-600 mb-2">Items:</p>
                {order.items.slice(0, 3).map((item) => (
                  <p key={item.id} className="text-gray-700">
                    {item.productName} x {item.quantity}
                  </p>
                ))}
                {order.items.length > 3 && (
                  <p className="text-gray-500 text-sm mt-1">+{order.items.length - 3} more items</p>
                )}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="text-center py-12">
          <p className="text-gray-500 text-xl mb-4">No orders yet</p>
          <button
            onClick={() => navigate('/')}
            className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
          >
            Start Shopping
          </button>
        </div>
      )}
    </div>
  );
}
