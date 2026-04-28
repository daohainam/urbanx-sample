import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useCart } from '../context/useCart';
import { useAuth } from '../context/useAuth';
import { orderService } from '../services/api';
import { Check, ChevronRight, CreditCard, Truck, Tag, ShoppingBag, LogIn } from 'lucide-react';
import type { PlaceOrderRequest, Order } from '../types';
import { checkoutAddressSchema, type CheckoutAddressForm } from '../schemas/address';
import { TextField } from '../components/forms/TextField';
import { logger } from '../lib/logger';
import { ApiError } from '../services/http';
import { ErrorState } from '../components/ui/ErrorState';

const shippingMethods = [
    { id: 'std', name: 'Standard Shipping', price: 0, estimatedDays: '3-5 business days' },
    { id: 'exp', name: 'Express Shipping', price: 15, estimatedDays: '1-2 business days' },
];

const paymentMethods = [
    { id: 'cc', name: 'Credit Card', description: 'Visa, Mastercard, AMEX' },
    { id: 'paypal', name: 'PayPal', description: 'Safe and secure' },
    { id: 'pod', name: 'Pay on Delivery', description: 'Cash or card on arrival' },
];

const DEFAULT_ADDRESS: CheckoutAddressForm = {
    firstName: '',
    lastName: '',
    street: '',
    city: '',
    zipCode: '',
    country: 'USA',
};

const CheckoutPage = () => {
    const { items, selectedItems, totalPrice, clearCart } = useCart();
    const { user, login } = useAuth();
    const [step, setStep] = useState(1);
    // Address persists across steps. Validated on the address step before advancing.
    const [address, setAddress] = useState<CheckoutAddressForm>(DEFAULT_ADDRESS);
    const [shippingMethod, setShippingMethod] = useState(shippingMethods[0]);
    const [paymentMethod, setPaymentMethod] = useState(paymentMethods[0]);
    const [coupons, setCoupons] = useState<string[]>([]);
    const [couponInput, setCouponInput] = useState('');
    const [placedOrder, setPlacedOrder] = useState<Order | null>(null);

    const queryClient = useQueryClient();
    const placeOrderMutation = useMutation({
        mutationFn: (request: PlaceOrderRequest) => orderService.placeOrder(request),
        onSuccess: (order) => {
            setPlacedOrder(order);
            clearCart();
            // Invalidate the customer's orders list so /orders refetches next time it's visited.
            if (user) {
                queryClient.invalidateQueries({ queryKey: ['orders', 'list', user.profile.sub] });
            }
        },
        onError: (err) => logger.error('Place order failed', err),
    });

    const {
        register,
        handleSubmit: handleAddressSubmit,
        formState: { errors: addressErrors },
    } = useForm<CheckoutAddressForm>({
        resolver: zodResolver(checkoutAddressSchema),
        mode: 'onBlur',
        defaultValues: address,
    });

    const checkoutItems = items.filter((item) => selectedItems.has(item.id));
    const shippingCost = shippingMethod.price;
    const discount = coupons.length * 10; // Simple mock discount
    const finalTotal = totalPrice + shippingCost - discount;

    const handleApplyCoupon = () => {
        const trimmed = couponInput.trim();
        if (trimmed && !coupons.includes(trimmed)) {
            setCoupons([...coupons, trimmed]);
            setCouponInput('');
        }
    };

    const onAddressSubmit = (values: CheckoutAddressForm) => {
        setAddress(values);
        setStep(2);
    };

    const handlePlaceOrder = async () => {
        if (!user) {
            await login();
            return;
        }
        const shippingAddressStr = `${address.firstName} ${address.lastName}, ${address.street}, ${address.city}, ${address.zipCode}, ${address.country}`;
        placeOrderMutation.mutate({
            customerId: user.profile.sub,
            items: checkoutItems.map((item) => ({
                productId: item.id,
                productName: item.name,
                quantity: item.quantity,
                unitPrice: item.price,
                merchantId: item.merchantId,
            })),
            totalAmount: finalTotal,
            shippingAddress: shippingAddressStr,
        });
    };

    if (placedOrder) {
        return (
            <div className="container mx-auto px-6 py-20 text-center max-w-lg">
                <div className="w-24 h-24 bg-green-50 text-green-600 rounded-full flex items-center justify-center mx-auto mb-8">
                    <Check size={48} aria-hidden="true" />
                </div>
                <h1 className="text-3xl font-serif font-bold text-gray-900 mb-4">Thank you for your order!</h1>
                <p className="text-gray-600 mb-8">Your order has been placed successfully and is being processed.</p>
                <div className="inline-block bg-gray-100 px-4 py-2 rounded-md font-mono text-sm text-gray-700 mb-8 border border-gray-200">
                    Order #{placedOrder.orderNumber}
                </div>
                <div>
                    <button
                        type="button"
                        className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900"
                        onClick={() => (window.location.href = '/')}
                    >
                        Continue Shopping
                    </button>
                </div>
            </div>
        );
    }

    if (checkoutItems.length === 0) {
        return (
            <div className="container mx-auto px-6 py-20 text-center">
                <ShoppingBag size={64} className="mx-auto text-gray-200 mb-6" />
                <h1 className="text-2xl font-serif font-bold text-gray-900 mb-3">Your selection is empty</h1>
                <p className="text-gray-500 mb-8">Please select items from your cart to proceed with checkout.</p>
                <button
                    type="button"
                    className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900"
                    onClick={() => (window.location.href = '/cart')}
                >
                    Go to Cart
                </button>
            </div>
        );
    }

    if (!user) {
        return (
            <div className="container mx-auto px-6 py-20 text-center">
                <ShoppingBag size={64} className="mx-auto text-gray-200 mb-6" />
                <h1 className="text-2xl font-serif font-bold text-gray-900 mb-3">Sign in to continue</h1>
                <p className="text-gray-500 mb-8">Please sign in to complete your purchase.</p>
                <button
                    type="button"
                    className="inline-flex items-center gap-2 h-10 justify-center rounded-md bg-primary px-8 text-sm font-medium text-white shadow transition-colors hover:bg-gray-900"
                    onClick={login}
                >
                    <LogIn size={16} /> Sign In
                </button>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-6 py-12">
            <title>Checkout — UrbanX</title>
            {/* Steps Indicator */}
            <div className="flex justify-center items-center gap-4 mb-16 overflow-x-auto pb-4 md:pb-0">
                {([
                    { n: 1, label: 'Address' },
                    { n: 2, label: 'Shipping' },
                    { n: 3, label: 'Payment' },
                    { n: 4, label: 'Confirm' },
                ] as const).map(({ n, label }, idx) => (
                    <div key={n} className="flex items-center gap-4">
                        <div className={`flex items-center gap-2 font-medium whitespace-nowrap ${step >= n ? (step > n ? 'text-green-600' : 'text-primary') : 'text-gray-400'}`}>
                            <span className={`w-7 h-7 rounded-full border-2 flex items-center justify-center text-xs ${step >= n ? (step > n ? 'border-green-600 bg-green-600 text-white' : 'border-primary text-primary') : 'border-current'}`}>
                                {step > n ? <Check size={14} /> : n}
                            </span>
                            <span>{label}</span>
                        </div>
                        {idx < 3 && <ChevronRight size={16} className="text-gray-300 flex-shrink-0" />}
                    </div>
                ))}
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
                <div className="lg:col-span-2">
                    {step === 1 && (
                        <form onSubmit={handleAddressSubmit(onAddressSubmit)} noValidate>
                            <h2 className="text-2xl font-serif font-bold text-gray-900 mb-8">Shipping Address</h2>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 bg-gray-50 p-8 rounded-lg border border-gray-100">
                                <TextField
                                    id="firstName"
                                    label="First Name"
                                    placeholder="John"
                                    autoComplete="given-name"
                                    error={addressErrors.firstName?.message}
                                    {...register('firstName')}
                                />
                                <TextField
                                    id="lastName"
                                    label="Last Name"
                                    placeholder="Doe"
                                    autoComplete="family-name"
                                    error={addressErrors.lastName?.message}
                                    {...register('lastName')}
                                />
                                <div className="md:col-span-2">
                                    <TextField
                                        id="street"
                                        label="Street Address"
                                        placeholder="123 Luxury St"
                                        autoComplete="street-address"
                                        error={addressErrors.street?.message}
                                        {...register('street')}
                                    />
                                </div>
                                <TextField
                                    id="city"
                                    label="City"
                                    placeholder="New York"
                                    autoComplete="address-level2"
                                    error={addressErrors.city?.message}
                                    {...register('city')}
                                />
                                <TextField
                                    id="zipCode"
                                    label="ZIP Code"
                                    placeholder="10001"
                                    autoComplete="postal-code"
                                    error={addressErrors.zipCode?.message}
                                    {...register('zipCode')}
                                />
                            </div>
                            <div className="mt-8 flex justify-end">
                                <button
                                    type="submit"
                                    className="bg-primary text-white px-8 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200"
                                >
                                    Continue to Shipping
                                </button>
                            </div>
                        </form>
                    )}

                    {step === 2 && (
                        <div>
                            <h2 className="text-2xl font-serif font-bold text-gray-900 mb-8">Select Shipping Method</h2>
                            <div className="space-y-4" role="radiogroup" aria-label="Shipping method">
                                {shippingMethods.map((method) => (
                                    <button
                                        type="button"
                                        key={method.id}
                                        role="radio"
                                        aria-checked={shippingMethod.id === method.id}
                                        className={`w-full text-left flex items-center gap-6 p-6 border rounded-lg cursor-pointer transition-all ${
                                            shippingMethod.id === method.id
                                                ? 'border-secondary bg-yellow-50/10 ring-1 ring-secondary'
                                                : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                                        }`}
                                        onClick={() => setShippingMethod(method)}
                                    >
                                        <Truck size={24} className={shippingMethod.id === method.id ? 'text-secondary' : 'text-gray-400'} />
                                        <div className="flex-1">
                                            <span className={`block font-semibold ${shippingMethod.id === method.id ? 'text-secondary' : 'text-gray-900'}`}>{method.name}</span>
                                            <span className="text-sm text-gray-500">{method.estimatedDays}</span>
                                        </div>
                                        <span className="font-bold text-gray-900">{method.price === 0 ? 'FREE' : `$${method.price}`}</span>
                                    </button>
                                ))}
                            </div>
                            <div className="mt-8 flex justify-between">
                                <button type="button" className="text-gray-500 font-medium hover:text-gray-900 px-4" onClick={() => setStep(1)}>Back</button>
                                <button type="button" className="bg-primary text-white px-8 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200" onClick={() => setStep(3)}>
                                    Continue to Payment
                                </button>
                            </div>
                        </div>
                    )}

                    {step === 3 && (
                        <div>
                            <h2 className="text-2xl font-serif font-bold text-gray-900 mb-8">Choose Payment Method</h2>
                            <div className="space-y-4" role="radiogroup" aria-label="Payment method">
                                {paymentMethods.map((method) => (
                                    <button
                                        type="button"
                                        key={method.id}
                                        role="radio"
                                        aria-checked={paymentMethod.id === method.id}
                                        className={`w-full text-left flex items-center gap-6 p-6 border rounded-lg cursor-pointer transition-all ${
                                            paymentMethod.id === method.id
                                                ? 'border-secondary bg-yellow-50/10 ring-1 ring-secondary'
                                                : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                                        }`}
                                        onClick={() => setPaymentMethod(method)}
                                    >
                                        <CreditCard size={24} className={paymentMethod.id === method.id ? 'text-secondary' : 'text-gray-400'} />
                                        <div className="flex-1">
                                            <span className={`block font-semibold ${paymentMethod.id === method.id ? 'text-secondary' : 'text-gray-900'}`}>{method.name}</span>
                                            <span className="text-sm text-gray-500">{method.description}</span>
                                        </div>
                                    </button>
                                ))}
                            </div>
                            <div className="mt-8 flex justify-between">
                                <button type="button" className="text-gray-500 font-medium hover:text-gray-900 px-4" onClick={() => setStep(2)}>Back</button>
                                <button type="button" className="bg-primary text-white px-8 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200" onClick={() => setStep(4)}>
                                    Review Order
                                </button>
                            </div>
                        </div>
                    )}

                    {step === 4 && (
                        <div>
                            <h2 className="text-2xl font-serif font-bold text-gray-900 mb-8">Review Your Order</h2>
                            <div className="bg-gray-50 rounded-lg border border-gray-200 divide-y divide-gray-200">
                                <div className="p-6">
                                    <h3 className="text-sm font-bold uppercase tracking-wider text-gray-500 mb-3">Shipping Details</h3>
                                    <div className="text-gray-900">
                                        <p>{address.firstName} {address.lastName}</p>
                                        <p>{address.street}</p>
                                        <p>{address.city}, {address.zipCode}</p>
                                        <p className="mt-2 text-sm text-gray-500">via {shippingMethod.name} ({shippingMethod.estimatedDays})</p>
                                    </div>
                                </div>
                                <div className="p-6">
                                    <h3 className="text-sm font-bold uppercase tracking-wider text-gray-500 mb-3">Payment Details</h3>
                                    <p className="text-gray-900">{paymentMethod.name}</p>
                                </div>
                            </div>
                            <div className="mt-8 flex flex-col gap-4">
                                {placeOrderMutation.isError && (
                                    <ErrorState
                                        error={placeOrderMutation.error}
                                        onRetry={
                                            placeOrderMutation.error instanceof ApiError && placeOrderMutation.error.isRetriable
                                                ? handlePlaceOrder
                                                : undefined
                                        }
                                        title="We couldn't place your order"
                                    />
                                )}
                                <div className="flex justify-between">
                                    <button
                                        type="button"
                                        className="text-gray-500 font-medium hover:text-gray-900 px-4"
                                        onClick={() => setStep(3)}
                                        disabled={placeOrderMutation.isPending}
                                    >
                                        Back
                                    </button>
                                    <button
                                        type="button"
                                        className="bg-primary text-white px-8 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200 disabled:opacity-60 disabled:cursor-not-allowed"
                                        onClick={handlePlaceOrder}
                                        disabled={placeOrderMutation.isPending}
                                    >
                                        {placeOrderMutation.isPending ? 'Placing Order...' : 'Place Order'}
                                    </button>
                                </div>
                            </div>
                        </div>
                    )}
                </div>

                <div className="lg:col-span-1">
                    <div className="bg-gray-50 p-6 md:p-8 rounded-lg sticky top-24">
                        <h3 className="text-xl font-serif font-bold text-gray-900 mb-6">Order Summary</h3>

                        <div className="space-y-4 mb-6 pb-6 border-b border-gray-200">
                            {checkoutItems.map((item) => (
                                <div key={item.id} className="flex justify-between text-sm">
                                    <span className="text-gray-600 line-clamp-1 flex-1 pr-4">{item.name} <span className="text-gray-400">x{item.quantity}</span></span>
                                    <span className="font-medium text-gray-900 shrink-0">${(item.price * item.quantity).toLocaleString()}</span>
                                </div>
                            ))}
                        </div>

                        <div className="mb-6 pb-6 border-b border-gray-200">
                            <div className="flex gap-2">
                                <label htmlFor="coupon" className="sr-only">Coupon code</label>
                                <input
                                    id="coupon"
                                    type="text"
                                    placeholder="Coupon code"
                                    value={couponInput}
                                    onChange={(e) => setCouponInput(e.target.value)}
                                    className="flex-1 border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:border-primary"
                                />
                                <button
                                    type="button"
                                    onClick={handleApplyCoupon}
                                    className="bg-gray-900 text-white px-4 py-2 rounded text-xs font-bold uppercase tracking-wider hover:bg-gray-800"
                                >
                                    Apply
                                </button>
                            </div>
                            {coupons.length > 0 && (
                                <div className="flex flex-wrap gap-2 mt-3">
                                    {coupons.map((c) => (
                                        <span key={c} className="bg-yellow-100 text-yellow-800 text-xs px-2 py-1 rounded-full flex items-center gap-1 font-medium">
                                            <Tag size={10} /> {c}
                                        </span>
                                    ))}
                                </div>
                            )}
                        </div>

                        <div className="space-y-3 mb-6">
                            <div className="flex justify-between text-sm text-gray-600">
                                <span>Subtotal</span>
                                <span>${totalPrice.toLocaleString()}</span>
                            </div>
                            <div className="flex justify-between text-sm text-gray-600">
                                <span>Shipping ({shippingMethod.name})</span>
                                <span>{shippingCost === 0 ? 'FREE' : `$${shippingCost}`}</span>
                            </div>
                            {discount > 0 && (
                                <div className="flex justify-between text-sm text-green-600">
                                    <span>Discount</span>
                                    <span>-${discount}</span>
                                </div>
                            )}
                        </div>

                        <div className="flex justify-between items-center border-t-2 border-primary pt-4">
                            <span className="font-bold text-gray-900">Total</span>
                            <span className="text-2xl font-bold text-primary">${finalTotal.toLocaleString()}</span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default CheckoutPage;
