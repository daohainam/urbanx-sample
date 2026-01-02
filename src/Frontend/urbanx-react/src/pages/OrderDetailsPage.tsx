import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Package, Truck, CheckCircle, MapPin, CreditCard } from 'lucide-react';

const OrderDetailsPage = () => {
    const navigate = useNavigate();
    const { id } = useParams();

    // Mock full order details
    const order = {
        id: id || 'URB-847251',
        date: 'May 12, 2024',
        status: 'Delivered',
        items: [
            { name: 'Premium Wireless Headphones', quantity: 1, price: 299, image: 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=200&q=80' },
            { name: 'USB-C Fast Charger', quantity: 2, price: 25, image: 'https://images.unsplash.com/photo-1579811216948-6f57c127329d?w=200&q=80' },
        ],
        subtotal: 349.00,
        shipping: 5.00,
        tax: 0.00,
        total: 354.00,
        shippingAddress: {
            name: 'John Doe',
            street: '123 Luxury St',
            city: 'New York',
            zip: '10001',
            country: 'USA'
        },
        paymentMethod: 'Visa ending in 4242'
    };

    const getStatusIcon = (status: string) => {
        switch (status) {
            case 'Delivered': return <CheckCircle className="text-green-600" size={20} />;
            case 'In Transit': return <Truck className="text-secondary" size={20} />;
            default: return <Package className="text-gray-400" size={20} />;
        }
    };

    return (
        <div className="container mx-auto px-6 py-12">
            <button className="flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8" onClick={() => navigate('/orders')}>
                <ArrowLeft size={16} /> Back to Orders
            </button>

            <div className="flex flex-col md:flex-row justify-between items-start md:items-center border-b border-gray-100 pb-8 mb-8 gap-4">
                <div>
                    <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Order #{order.id}</h1>
                    <p className="text-gray-500 text-sm">Placed on {order.date}</p>
                </div>
                <div className="flex items-center gap-2 bg-gray-50 px-4 py-2 rounded-full border border-gray-100 font-semibold text-gray-900">
                    {getStatusIcon(order.status)}
                    <span>{order.status}</span>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="lg:col-span-2 space-y-8">
                    {/* Items */}
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h3 className="font-serif font-bold text-gray-900 mb-6 pb-4 border-b border-gray-50">Items Ordered</h3>
                        <div className="space-y-6">
                            {order.items.map((item, idx) => (
                                <div key={idx} className="flex items-center gap-4 md:gap-6">
                                    <div className="w-16 h-16 md:w-20 md:h-20 bg-gray-50 rounded-md overflow-hidden border border-gray-100 flex-shrink-0">
                                        <img src={item.image} alt={item.name} className="w-full h-full object-cover" />
                                    </div>
                                    <div className="flex-1">
                                        <span className="block font-medium text-gray-900 mb-1">{item.name}</span>
                                        <span className="text-sm text-gray-500">Qty: {item.quantity}</span>
                                    </div>
                                    <div className="font-semibold text-gray-900">
                                        ${item.price.toLocaleString()}
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Timeline */}
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h3 className="font-serif font-bold text-gray-900 mb-6 pb-4 border-b border-gray-50">Order Timeline</h3>
                        <div className="relative pl-4 ml-2 border-l-2 border-gray-100 space-y-8 py-2">
                            {[
                                { title: 'Order Placed', time: 'May 12, 10:30 AM', completed: true },
                                { title: 'Processing', time: 'May 12, 2:00 PM', completed: true },
                                { title: 'Shipped', time: 'May 13, 9:00 AM', completed: true },
                                { title: 'Delivered', time: 'May 15, 4:30 PM', completed: true },
                            ].map((step, idx) => (
                                <div key={idx} className="relative pl-8">
                                    <div className={`absolute -left-[9px] top-1.5 w-[16px] h-[16px] rounded-full border-2 bg-white ${step.completed ? 'border-green-600 bg-green-600' : 'border-gray-200'}`}>
                                        {step.completed && <div className="w-full h-full rounded-full bg-green-600"></div>}
                                    </div>
                                    <div>
                                        <h4 className="font-semibold text-sm text-gray-900">{step.title}</h4>
                                        <p className="text-xs text-gray-500">{step.time}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>

                {/* Sidebar */}
                <div className="lg:col-span-1 space-y-8">
                    {/* Summary */}
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h3 className="font-serif font-bold text-gray-900 mb-6">Order Summary</h3>
                        <div className="space-y-3 mb-6 pb-6 border-b border-gray-50 text-sm">
                            <div className="flex justify-between text-gray-600">
                                <span>Subtotal</span>
                                <span>${order.subtotal.toFixed(2)}</span>
                            </div>
                            <div className="flex justify-between text-gray-600">
                                <span>Shipping</span>
                                <span>${order.shipping.toFixed(2)}</span>
                            </div>
                            <div className="flex justify-between text-gray-600">
                                <span>Tax</span>
                                <span>${order.tax.toFixed(2)}</span>
                            </div>
                        </div>
                        <div className="flex justify-between items-center font-bold text-lg text-gray-900">
                            <span>Total</span>
                            <span>${order.total.toFixed(2)}</span>
                        </div>
                    </div>

                    {/* Shipping Details */}
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h3 className="font-serif font-bold text-gray-900 mb-4">Shipping Details</h3>
                        <div className="flex gap-4">
                            <MapPin size={20} className="text-gray-400 flex-shrink-0 mt-0.5" />
                            <div className="text-sm text-gray-600 leading-relaxed">
                                <p className="font-semibold text-gray-900">{order.shippingAddress.name}</p>
                                <p>{order.shippingAddress.street}</p>
                                <p>{order.shippingAddress.city}, {order.shippingAddress.zip}</p>
                                <p>{order.shippingAddress.country}</p>
                            </div>
                        </div>
                    </div>

                    {/* Payment Info */}
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h3 className="font-serif font-bold text-gray-900 mb-4">Payment Information</h3>
                        <div className="flex gap-4 items-center">
                            <CreditCard size={20} className="text-gray-400 flex-shrink-0" />
                            <p className="text-sm text-gray-600">{order.paymentMethod}</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default OrderDetailsPage;
