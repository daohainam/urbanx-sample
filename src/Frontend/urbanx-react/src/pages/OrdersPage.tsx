import { useNavigate, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ShoppingBag, Package, Truck, CheckCircle, LogIn } from 'lucide-react';
import { orderService } from '../services/api';
import { useAuth } from '../context/useAuth';
import { ListRowSkeleton } from '../components/ui/Skeleton';
import { ErrorState } from '../components/ui/ErrorState';
import { EmptyState } from '../components/ui/EmptyState';
import { queryKeys } from '../lib/queryKeys';

const statusIcon = (status: string) => {
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

const OrdersPage = () => {
    const navigate = useNavigate();
    const { user, isLoading: authLoading, login } = useAuth();
    const customerId = user?.profile.sub;

    const { data: orders = [], isPending, isError, error, refetch, isFetching } = useQuery({
        queryKey: customerId ? queryKeys.orders.list(customerId) : ['orders', 'list', 'anonymous'],
        queryFn: ({ signal }) => orderService.getOrders(customerId as string, signal),
        enabled: Boolean(customerId),
    });

    if (authLoading) {
        return (
            <div className="container mx-auto px-6 py-12 min-h-[70vh] space-y-6">
                <ListRowSkeleton />
                <ListRowSkeleton />
            </div>
        );
    }

    if (!user) {
        return (
            <div className="container mx-auto px-6 py-32 text-center">
                <ShoppingBag size={64} className="mx-auto text-gray-200 mb-6" aria-hidden="true" />
                <h1 className="text-2xl font-serif font-bold text-gray-900 mb-3">Sign in to view your orders</h1>
                <p className="text-gray-500 mb-8">Please sign in to access your order history.</p>
                <button
                    type="button"
                    onClick={login}
                    className="inline-flex items-center gap-2 h-10 justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900"
                >
                    <LogIn size={16} aria-hidden="true" /> Sign In
                </button>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-6 py-12 min-h-[70vh]" aria-busy={isFetching}>
            <div className="mb-8 border-b border-gray-100 pb-4">
                <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Your Orders</h1>
                <p className="text-gray-500">Track, manage, and review your previous purchases.</p>
            </div>

            {isPending ? (
                <div className="space-y-6">
                    <ListRowSkeleton />
                    <ListRowSkeleton />
                    <ListRowSkeleton />
                </div>
            ) : isError ? (
                <ErrorState error={error} onRetry={() => refetch()} title="Couldn't load your orders" />
            ) : orders.length === 0 ? (
                <EmptyState
                    icon={<ShoppingBag size={48} />}
                    title="You haven't placed any orders yet."
                    action={
                        <Link to="/catalog" className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900">
                            Start Shopping
                        </Link>
                    }
                />
            ) : (
                <div className="space-y-6">
                    {orders.map((order) => (
                        <div key={order.id} className="bg-white border border-gray-100 rounded-lg overflow-hidden shadow-sm hover:shadow-md transition-shadow">
                            <div className="bg-gray-50 p-6 flex flex-col md:flex-row justify-between items-start md:items-center border-b border-gray-100 gap-4">
                                <div className="flex flex-wrap gap-x-12 gap-y-4">
                                    <div className="flex flex-col">
                                        <span className="text-xs font-bold uppercase tracking-wider text-gray-500">Order Number</span>
                                        <span className="font-semibold text-primary">#{order.orderNumber}</span>
                                    </div>
                                    <div className="flex flex-col">
                                        <span className="text-xs font-bold uppercase tracking-wider text-gray-500">Date Placed</span>
                                        <span className="font-semibold text-primary">{new Date(order.createdAt).toLocaleDateString()}</span>
                                    </div>
                                    <div className="flex flex-col">
                                        <span className="text-xs font-bold uppercase tracking-wider text-gray-500">Total Amount</span>
                                        <span className="font-semibold text-primary">${order.totalAmount.toLocaleString()}</span>
                                    </div>
                                </div>
                                <div className="flex items-center gap-2">
                                    {statusIcon(order.status)}
                                    <span className="font-semibold text-sm text-gray-900">{statusLabel(order.status)}</span>
                                </div>
                            </div>

                            <div className="p-6">
                                <div className="space-y-4">
                                    {order.items.map((item) => (
                                        <div key={item.id} className="flex items-center gap-4">
                                            <div className="w-16 h-16 bg-gray-100 rounded-md flex items-center justify-center text-gray-400 flex-shrink-0" aria-hidden="true">
                                                <ShoppingBag size={24} />
                                            </div>
                                            <div className="flex-1">
                                                <span className="block font-semibold text-gray-900">{item.productName}</span>
                                                <span className="text-sm text-gray-500">Quantity: {item.quantity}</span>
                                            </div>
                                            <span className="font-semibold text-gray-900">${item.unitPrice.toLocaleString()}</span>
                                        </div>
                                    ))}
                                </div>
                            </div>

                            <div className="p-4 bg-gray-50 border-t border-gray-100 flex justify-end gap-3">
                                <button
                                    type="button"
                                    className="px-4 py-2 bg-primary text-white rounded text-sm font-medium hover:bg-gray-900 transition-colors"
                                    onClick={() => navigate(`/orders/${order.id}`)}
                                >
                                    View Details
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

export default OrdersPage;
