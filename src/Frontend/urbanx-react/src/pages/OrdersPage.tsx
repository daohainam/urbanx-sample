import { useNavigate } from 'react-router-dom';
import { ShoppingBag, Package, Truck, CheckCircle } from 'lucide-react';

const OrdersPage = () => {
    const navigate = useNavigate();
    // Mock orders data
    const orders = [
        {
            id: 'URB-847251',
            date: 'May 12, 2024',
            total: 354.00,
            status: 'Delivered',
            items: [
                { name: 'Premium Wireless Headphones', quantity: 1, price: 299 },
                { name: 'USB-C Fast Charger', quantity: 2, price: 25 },
            ],
            icon: <CheckCircle className="text-green-600" size={20} />
        },
        {
            id: 'URB-921033',
            date: 'May 18, 2024',
            total: 129.50,
            status: 'In Transit',
            items: [
                { name: 'Smart Fitness Tracker', quantity: 1, price: 129.50 },
            ],
            icon: <Truck className="text-secondary" size={20} />
        },
        {
            id: 'URB-105572',
            date: 'June 01, 2024',
            total: 2155.00,
            status: 'Processing',
            items: [
                { name: 'MacBook Pro 14"', quantity: 1, price: 1999 },
                { name: 'Leather Slim Sleeve', quantity: 1, price: 156 },
            ],
            icon: <Package className="text-gray-400" size={20} />
        },
    ];

    return (
        <div className="container mx-auto px-6 py-12 min-h-[70vh]">
            <div className="mb-8 border-b border-gray-100 pb-4">
                <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Your Orders</h1>
                <p className="text-gray-500">Track, manage, and review your previous purchases.</p>
            </div>

            <div className="space-y-6">
                {orders.map((order) => (
                    <div key={order.id} className="bg-white border border-gray-100 rounded-lg overflow-hidden shadow-sm hover:shadow-md transition-shadow">
                        <div className="bg-gray-50 p-6 flex flex-col md:flex-row justify-between items-start md:items-center border-b border-gray-100 gap-4">
                            <div className="flex flex-wrap gap-x-12 gap-y-4">
                                <div className="flex flex-col">
                                    <span className="text-xs font-bold uppercase tracking-wider text-gray-500">Order Number</span>
                                    <span className="font-semibold text-primary">#{order.id}</span>
                                </div>
                                <div className="flex flex-col">
                                    <span className="text-xs font-bold uppercase tracking-wider text-gray-500">Date Placed</span>
                                    <span className="font-semibold text-primary">{order.date}</span>
                                </div>
                                <div className="flex flex-col">
                                    <span className="text-xs font-bold uppercase tracking-wider text-gray-500">Total Amount</span>
                                    <span className="font-semibold text-primary">${order.total.toLocaleString()}</span>
                                </div>
                            </div>
                            <div className="flex items-center gap-2">
                                {order.icon}
                                <span className="font-semibold text-sm text-gray-900">{order.status}</span>
                            </div>
                        </div>

                        <div className="p-6">
                            <div className="space-y-4">
                                {order.items.map((item, idx) => (
                                    <div key={idx} className="flex items-center gap-4">
                                        <div className="w-16 h-16 bg-gray-100 rounded-md flex items-center justify-center text-gray-400 flex-shrink-0">
                                            <ShoppingBag size={24} />
                                        </div>
                                        <div className="flex-1">
                                            <span className="block font-semibold text-gray-900">{item.name}</span>
                                            <span className="text-sm text-gray-500">Quantity: {item.quantity}</span>
                                        </div>
                                        <span className="font-semibold text-gray-900">${item.price.toLocaleString()}</span>
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div className="p-4 bg-gray-50 border-t border-gray-100 flex justify-end gap-3">
                            <button className="px-4 py-2 bg-white border border-gray-200 text-gray-700 rounded text-sm font-medium hover:bg-gray-50 hover:border-gray-300 transition-colors">
                                Track Order
                            </button>
                            <button
                                className="px-4 py-2 bg-primary text-white rounded text-sm font-medium hover:bg-gray-900 transition-colors"
                                onClick={() => navigate(`/orders/${order.id}`)}
                            >
                                View Details
                            </button>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default OrdersPage;
