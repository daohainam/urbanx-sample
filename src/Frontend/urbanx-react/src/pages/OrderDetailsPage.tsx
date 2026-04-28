import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Package, Truck, CheckCircle, MapPin, CreditCard } from 'lucide-react';
import { orderService } from '../services/api';
import { Skeleton } from '../components/ui/Skeleton';
import { ErrorState } from '../components/ui/ErrorState';
import { queryKeys } from '../lib/queryKeys';

const getStatusIcon = (status: string) => {
    switch (status) {
        case 'Delivered': return <CheckCircle className="text-green-600" size={20} aria-hidden="true" />;
        case 'InTransit': return <Truck className="text-secondary" size={20} aria-hidden="true" />;
        default: return <Package className="text-gray-400" size={20} aria-hidden="true" />;
    }
};

const statusLabel = (status: string) => {
    switch (status) {
        case 'PaymentReceived': return 'Payment Received';
        case 'ReadyForPickup': return 'Ready for Pickup';
        case 'InTransit': return 'In Transit';
        default: return status;
    }
};

const OrderDetailsPage = () => {
    const navigate = useNavigate();
    const { id } = useParams();

    const { data: order, isPending, isError, error, refetch } = useQuery({
        queryKey: id ? queryKeys.orders.detail(id) : ['orders', 'detail', 'missing'],
        queryFn: ({ signal }) => orderService.getOrder(id as string, signal),
        enabled: Boolean(id),
    });

    if (!id) {
        return (
            <div className="container mx-auto px-6 py-20">
                <ErrorState error={new Error('Missing order id')} title="Order not found" />
            </div>
        );
    }

    if (isPending) {
        return (
            <div className="container mx-auto px-6 py-12 space-y-8">
                <Skeleton className="h-8 w-48" />
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                    <div className="lg:col-span-2 space-y-4">
                        <Skeleton className="h-40 w-full" />
                        <Skeleton className="h-32 w-full" />
                    </div>
                    <div className="space-y-4">
                        <Skeleton className="h-32 w-full" />
                        <Skeleton className="h-24 w-full" />
                    </div>
                </div>
            </div>
        );
    }

    if (isError || !order) {
        return (
            <div className="container mx-auto px-6 py-20">
                <ErrorState
                    error={error}
                    onRetry={() => refetch()}
                    title="Couldn't load this order"
                />
                <div className="text-center mt-6">
                    <button
                        type="button"
                        className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900"
                        onClick={() => navigate('/orders')}
                    >
                        Back to Orders
                    </button>
                </div>
            </div>
        );
    }

    const subtotal = order.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);

    return (
        <div className="container mx-auto px-6 py-12">
            <title>{`Order #${order.orderNumber} — UrbanX`}</title>
            <button
                type="button"
                className="flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8"
                onClick={() => navigate('/orders')}
            >
                <ArrowLeft size={16} aria-hidden="true" /> Back to Orders
            </button>

            <div className="flex flex-col md:flex-row justify-between items-start md:items-center border-b border-gray-100 pb-8 mb-8 gap-4">
                <div>
                    <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Order #{order.orderNumber}</h1>
                    <p className="text-gray-500 text-sm">Placed on {new Date(order.createdAt).toLocaleDateString()}</p>
                </div>
                <div className="flex items-center gap-2 bg-gray-50 px-4 py-2 rounded-full border border-gray-100 font-semibold text-gray-900">
                    {getStatusIcon(order.status)}
                    <span>{statusLabel(order.status)}</span>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="lg:col-span-2 space-y-8">
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h2 className="font-serif font-bold text-gray-900 mb-6 pb-4 border-b border-gray-50">Items Ordered</h2>
                        <div className="space-y-6">
                            {order.items.map((item) => (
                                <div key={item.id} className="flex items-center gap-4 md:gap-6">
                                    <div className="w-16 h-16 md:w-20 md:h-20 bg-gray-50 rounded-md overflow-hidden border border-gray-100 flex-shrink-0 flex items-center justify-center text-gray-300" aria-hidden="true">
                                        <Package size={24} />
                                    </div>
                                    <div className="flex-1">
                                        <span className="block font-medium text-gray-900 mb-1">{item.productName}</span>
                                        <span className="text-sm text-gray-500">Qty: {item.quantity}</span>
                                    </div>
                                    <div className="font-semibold text-gray-900">
                                        ${(item.unitPrice * item.quantity).toLocaleString()}
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>

                    {order.statusHistory.length > 0 && (
                        <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                            <h2 className="font-serif font-bold text-gray-900 mb-6 pb-4 border-b border-gray-50">Order Timeline</h2>
                            <ol className="relative pl-4 ml-2 border-l-2 border-gray-100 space-y-8 py-2">
                                {order.statusHistory.map((entry) => (
                                    <li key={entry.id ?? `${entry.status}-${entry.createdAt}`} className="relative pl-8">
                                        <div className="absolute -left-[9px] top-1.5 w-[16px] h-[16px] rounded-full border-2 bg-green-600 border-green-600" aria-hidden="true"></div>
                                        <div>
                                            <h3 className="font-semibold text-sm text-gray-900">{statusLabel(entry.status)}</h3>
                                            <p className="text-xs text-gray-500">{new Date(entry.createdAt).toLocaleString()}</p>
                                            {entry.note && <p className="text-xs text-gray-400 mt-1">{entry.note}</p>}
                                        </div>
                                    </li>
                                ))}
                            </ol>
                        </div>
                    )}
                </div>

                <div className="lg:col-span-1 space-y-8">
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h2 className="font-serif font-bold text-gray-900 mb-6">Order Summary</h2>
                        <div className="space-y-3 mb-6 pb-6 border-b border-gray-50 text-sm">
                            <div className="flex justify-between text-gray-600">
                                <span>Subtotal</span>
                                <span>${subtotal.toFixed(2)}</span>
                            </div>
                        </div>
                        <div className="flex justify-between items-center font-bold text-lg text-gray-900">
                            <span>Total</span>
                            <span>${order.totalAmount.toFixed(2)}</span>
                        </div>
                    </div>

                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h2 className="font-serif font-bold text-gray-900 mb-4">Shipping Details</h2>
                        <div className="flex gap-4">
                            <MapPin size={20} className="text-gray-400 flex-shrink-0 mt-0.5" aria-hidden="true" />
                            <div className="text-sm text-gray-600 leading-relaxed">
                                <p>{order.shippingAddress}</p>
                            </div>
                        </div>
                    </div>

                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 md:p-8">
                        <h2 className="font-serif font-bold text-gray-900 mb-4">Payment Information</h2>
                        <div className="flex gap-4 items-center">
                            <CreditCard size={20} className="text-gray-400 flex-shrink-0" aria-hidden="true" />
                            <p className="text-sm text-gray-600">Online Payment</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default OrderDetailsPage;
