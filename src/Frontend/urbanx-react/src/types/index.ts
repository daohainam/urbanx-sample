export interface Product {
    id: string;
    name: string;
    description: string;
    price: number;
    imageUrl: string;
    category: string;
    merchantId: string;
    inventoryCount: number;
}

export interface CartItem extends Product {
    quantity: number;
}

export interface Order {
    id: string;
    customerId: string;
    items: OrderItem[];
    totalAmount: number;
    status: OrderStatus;
    createdAt: string;
    shippingAddress: string;
    shippingMethod: string;
    paymentMethod: string;
    couponsApplied: string[];
}

export interface OrderItem {
    productId: string;
    productName: string;
    quantity: number;
    price: number;
}

export interface OrderData {
    customerId: string;
    items: OrderItem[];
    totalAmount: number;
    shippingAddress: string;
    shippingMethod: string;
    paymentMethod: string;
    couponsApplied: string[];
}

export type OrderStatus = 'Pending' | 'Confirmed' | 'Shipped' | 'Delivered' | 'Cancelled';

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
