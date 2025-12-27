export interface Product {
  id: string;
  name: string;
  description?: string;
  price: number;
  merchantId: string;
  stockQuantity: number;
  imageUrl?: string;
  category?: string;
  isActive: boolean;
}

export interface CartItem {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  merchantId: string;
}

export interface Cart {
  id: string;
  customerId: string;
  items: CartItem[];
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
  status: string;
  note?: string;
  createdAt: string;
}
