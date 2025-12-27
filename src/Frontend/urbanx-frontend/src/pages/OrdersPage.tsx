import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Package, Calendar, ChevronRight } from 'lucide-react';
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
      Pending: 'bg-gradient-to-r from-yellow-400 to-yellow-500',
      PaymentReceived: 'bg-gradient-to-r from-blue-400 to-blue-500',
      Confirmed: 'bg-gradient-to-r from-green-400 to-green-500',
      Preparing: 'bg-gradient-to-r from-purple-400 to-purple-500',
      ReadyForPickup: 'bg-gradient-to-r from-indigo-400 to-indigo-500',
      InTransit: 'bg-gradient-to-r from-cyan-400 to-cyan-500',
      Delivered: 'bg-gradient-to-r from-green-500 to-green-600',
      Cancelled: 'bg-gradient-to-r from-red-400 to-red-500',
    };
    return colors[status] || 'bg-gradient-to-r from-neutral-400 to-neutral-500';
  };

  if (loading) {
    return (
      <div className="flex flex-col justify-center items-center h-screen">
        <div className="spinner mb-4"></div>
        <p className="text-neutral-600">Loading orders...</p>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-6 py-10 animate-fade-in">
      <div className="mb-10">
        <h1 className="text-5xl font-bold mb-3 gradient-text">My Orders</h1>
        <p className="text-neutral-600 text-lg">Track and manage your orders</p>
      </div>

      {orders.length > 0 ? (
        <div className="grid grid-cols-1 gap-5">
          {orders.map((order) => (
            <div 
              key={order.id} 
              className="card-elevated p-6 cursor-pointer group"
              onClick={() => navigate(`/tracking/${order.id}`)}
            >
              <div className="flex justify-between items-start mb-4">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <h3 className="text-2xl font-bold text-neutral-800">{order.orderNumber}</h3>
                    <span className={`badge ${getStatusColor(order.status)} text-white shadow-soft`}>
                      {order.status}
                    </span>
                  </div>
                  <div className="flex items-center gap-2 text-neutral-600">
                    <Calendar className="w-4 h-4" />
                    <span>{new Date(order.createdAt).toLocaleDateString('en-US', { 
                      year: 'numeric', 
                      month: 'long', 
                      day: 'numeric' 
                    })}</span>
                  </div>
                </div>
                <div className="text-right flex items-start gap-4">
                  <div>
                    <p className="text-sm text-neutral-500 mb-1">Total Amount</p>
                    <p className="text-3xl font-bold gradient-text">${order.totalAmount.toFixed(2)}</p>
                  </div>
                  <ChevronRight className="w-6 h-6 text-neutral-400 group-hover:text-primary-600 transition-colors mt-2" />
                </div>
              </div>
              <div className="border-t border-neutral-200 pt-4">
                <div className="flex items-start gap-3">
                  <Package className="w-5 h-5 text-neutral-400 mt-1" />
                  <div className="flex-1">
                    <p className="text-neutral-600 font-medium mb-2">Items:</p>
                    <div className="space-y-1">
                      {order.items.slice(0, 3).map((item) => (
                        <p key={item.id} className="text-neutral-700">
                          <span className="font-medium">{item.productName}</span>
                          <span className="text-neutral-500 ml-2">x {item.quantity}</span>
                        </p>
                      ))}
                    </div>
                    {order.items.length > 3 && (
                      <p className="text-neutral-500 text-sm mt-2">+{order.items.length - 3} more items</p>
                    )}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="text-center py-20">
          <div className="inline-flex items-center justify-center w-24 h-24 rounded-full bg-gradient-to-br from-neutral-100 to-neutral-200 mb-6">
            <Package className="w-12 h-12 text-neutral-400" />
          </div>
          <p className="text-neutral-500 text-2xl font-semibold mb-3">No orders yet</p>
          <p className="text-neutral-400 mb-6">Start shopping to create your first order</p>
          <button
            onClick={() => navigate('/')}
            className="btn btn-primary text-base px-8"
          >
            Start Shopping
          </button>
        </div>
      )}
    </div>
  );
}
