export interface Category {
  id: string;
  merchantId: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Product {
  id: string;
  merchantId: string;
  name: string;
  description?: string;
  price: number;
  stockQuantity: number;
  imageUrl?: string;
  category?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
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

export interface Order {
  id: string;
  customerId: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  items: OrderItem[];
  shippingAddress: string;
  createdAt: string;
  updatedAt: string;
  statusHistory: OrderStatusHistory[];
}

export interface OrderStatusHistory {
  id: string;
  orderId: string;
  status: string;
  note?: string;
  createdAt: string;
}

export const OrderStatus = {
  Pending: 'Pending',
  PaymentReceived: 'PaymentReceived',
  Confirmed: 'Confirmed',
  Preparing: 'Preparing',
  ReadyForPickup: 'ReadyForPickup',
  InTransit: 'InTransit',
  Delivered: 'Delivered',
  Cancelled: 'Cancelled'
} as const;

export type OrderStatusType = typeof OrderStatus[keyof typeof OrderStatus];

export interface Merchant {
  id: string;
  name: string;
  description?: string;
  email: string;
  phone?: string;
  address?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}
