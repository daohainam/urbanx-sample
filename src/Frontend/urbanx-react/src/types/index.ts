export interface Product {
    id: string;
    name: string;
    description: string;
    price: number;
    imageUrl: string;
    category: string;
    merchantId: string;
    stockQuantity: number;
}

export interface CartItem extends Product {
    quantity: number;
}

export interface Order {
    id: string;
    customerId: string;
    orderNumber: string;
    items: OrderItem[];
    totalAmount: number;
    status: OrderStatus;
    createdAt: string;
    updatedAt: string;
    shippingAddress: string;
    statusHistory: OrderStatusHistory[];
}

export interface OrderItem {
    id: string;
    orderId: string;
    productId: string;
    productName: string;
    quantity: number;
    unitPrice: number;
    merchantId: string;
}

export interface OrderStatusHistory {
    id: string;
    orderId: string;
    status: OrderStatus;
    note?: string;
    createdAt: string;
}

export interface PlaceOrderRequest {
    customerId: string;
    items: PlaceOrderItem[];
    totalAmount: number;
    shippingAddress: string;
}

export interface PlaceOrderItem {
    productId: string;
    productName: string;
    quantity: number;
    unitPrice: number;
    merchantId: string;
}

export type OrderStatus = 'Pending' | 'PaymentReceived' | 'Confirmed' | 'Preparing' | 'ReadyForPickup' | 'InTransit' | 'Delivered' | 'Cancelled';

export interface Category {
    id: string;
    name: string;
    slug: string;
    imageUrl?: string;
}

export interface ShippingMethod {
    id: string;
    name: string;
    price: number;
    estimatedDays: string;
}

export interface PaymentMethod {
    id: string;
    name: string;
    description: string;
}
