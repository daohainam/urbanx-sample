import { useState, useEffect } from 'react';
import { Eye } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { ApiClient } from '../lib/api';
import type { Order } from '../types';
import { OrderStatus } from '../types';

export default function OrdersPage() {
  const auth = useAuth();
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);

  const api = new ApiClient(() => auth.user?.access_token);
  const merchantId = 'c0a80121-0000-0000-0000-000000000001'; // TODO: Get from user profile

  useEffect(() => {
    loadOrders();
  }, []);

  const loadOrders = async () => {
    try {
      setLoading(true);
      const data = await api.get<Order[]>(`/api/merchants/${merchantId}/orders`);
      setOrders(data);
    } catch (error) {
      console.error('Failed to load orders:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateStatus = async (orderId: string, status: string) => {
    try {
      await api.put(`/api/orders/${orderId}/status`, status);
      loadOrders();
      if (selectedOrder?.id === orderId) {
        const updatedOrder = await api.get<Order>(`/api/orders/${orderId}`);
        setSelectedOrder(updatedOrder);
      }
    } catch (error) {
      console.error('Failed to update order status:', error);
      alert('Failed to update order status');
    }
  };

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      [OrderStatus.Pending]: 'bg-yellow-100 text-yellow-800',
      [OrderStatus.PaymentReceived]: 'bg-blue-100 text-blue-800',
      [OrderStatus.Confirmed]: 'bg-green-100 text-green-800',
      [OrderStatus.Preparing]: 'bg-purple-100 text-purple-800',
      [OrderStatus.ReadyForPickup]: 'bg-cyan-100 text-cyan-800',
      [OrderStatus.InTransit]: 'bg-indigo-100 text-indigo-800',
      [OrderStatus.Delivered]: 'bg-green-100 text-green-800',
      [OrderStatus.Cancelled]: 'bg-red-100 text-red-800',
    };
    return colors[status] || 'bg-neutral-100 text-neutral-800';
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="spinner"></div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-neutral-900 mb-2">Orders</h1>
        <p className="text-neutral-600">Manage and track your orders</p>
      </div>

      <div className="bg-white rounded-lg shadow-soft border border-neutral-200">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-neutral-50 border-b border-neutral-200">
              <tr>
                <th className="px-6 py-3 text-left text-sm font-semibold text-neutral-900">
                  Order #
                </th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-neutral-900">
                  Date
                </th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-neutral-900">
                  Items
                </th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-neutral-900">
                  Total
                </th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-neutral-900">
                  Status
                </th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-neutral-900">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-200">
              {orders.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-neutral-500">
                    No orders found.
                  </td>
                </tr>
              ) : (
                orders.map((order) => (
                  <tr key={order.id} className="hover:bg-neutral-50">
                    <td className="px-6 py-4">
                      <span className="text-sm font-medium text-neutral-900">
                        {order.orderNumber}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm text-neutral-600">
                        {new Date(order.createdAt).toLocaleDateString()}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm text-neutral-600">
                        {order.items.length} item(s)
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm font-medium text-neutral-900">
                        ${order.totalAmount.toFixed(2)}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusColor(
                          order.status
                        )}`}
                      >
                        {order.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button
                        onClick={() => setSelectedOrder(order)}
                        className="p-2 text-primary-600 hover:bg-primary-50 rounded-lg transition-colors"
                        title="View Details"
                      >
                        <Eye className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {selectedOrder && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg shadow-large max-w-2xl w-full max-h-[90vh] overflow-auto">
            <div className="p-6 border-b border-neutral-200">
              <h2 className="text-2xl font-bold text-neutral-900">
                Order Details
              </h2>
              <p className="text-sm text-neutral-600 mt-1">
                {selectedOrder.orderNumber}
              </p>
            </div>

            <div className="p-6 space-y-6">
              <div>
                <h3 className="text-lg font-semibold mb-3">Order Information</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <p className="text-sm text-neutral-600">Date</p>
                    <p className="text-sm font-medium">
                      {new Date(selectedOrder.createdAt).toLocaleString()}
                    </p>
                  </div>
                  <div>
                    <p className="text-sm text-neutral-600">Status</p>
                    <span
                      className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusColor(
                        selectedOrder.status
                      )}`}
                    >
                      {selectedOrder.status}
                    </span>
                  </div>
                  <div className="col-span-2">
                    <p className="text-sm text-neutral-600">Shipping Address</p>
                    <p className="text-sm font-medium">
                      {selectedOrder.shippingAddress}
                    </p>
                  </div>
                </div>
              </div>

              <div>
                <h3 className="text-lg font-semibold mb-3">Items</h3>
                <div className="space-y-2">
                  {selectedOrder.items.map((item) => (
                    <div
                      key={item.id}
                      className="flex justify-between items-center p-3 bg-neutral-50 rounded-lg"
                    >
                      <div>
                        <p className="text-sm font-medium">{item.productName}</p>
                        <p className="text-xs text-neutral-600">
                          Quantity: {item.quantity}
                        </p>
                      </div>
                      <p className="text-sm font-medium">
                        ${(item.unitPrice * item.quantity).toFixed(2)}
                      </p>
                    </div>
                  ))}
                </div>
                <div className="mt-4 pt-4 border-t border-neutral-200">
                  <div className="flex justify-between items-center">
                    <p className="text-lg font-semibold">Total</p>
                    <p className="text-lg font-bold text-primary-600">
                      ${selectedOrder.totalAmount.toFixed(2)}
                    </p>
                  </div>
                </div>
              </div>

              <div>
                <h3 className="text-lg font-semibold mb-3">Update Status</h3>
                <div className="flex flex-wrap gap-2">
                  {Object.values(OrderStatus).map((status) => (
                    <button
                      key={status}
                      onClick={() => handleUpdateStatus(selectedOrder.id, status)}
                      disabled={selectedOrder.status === status}
                      className={`px-3 py-1.5 text-sm rounded-lg transition-colors ${
                        selectedOrder.status === status
                          ? 'bg-neutral-200 text-neutral-500 cursor-not-allowed'
                          : 'bg-primary-600 text-white hover:bg-primary-700'
                      }`}
                    >
                      {status}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="p-6 border-t border-neutral-200 flex justify-end">
              <button
                onClick={() => setSelectedOrder(null)}
                className="px-4 py-2 bg-neutral-200 text-neutral-700 rounded-lg hover:bg-neutral-300 transition-colors"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
