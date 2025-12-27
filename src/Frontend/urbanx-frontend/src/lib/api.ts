const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const api = {
  // Products
  getProducts: async (search?: string, category?: string, page = 1) => {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (category) params.append('category', category);
    params.append('page', page.toString());
    
    const response = await fetch(`${API_BASE_URL}/api/products?${params}`);
    if (!response.ok) throw new Error('Failed to fetch products');
    return response.json();
  },

  getProduct: async (id: string) => {
    const response = await fetch(`${API_BASE_URL}/api/products/${id}`);
    if (!response.ok) throw new Error('Failed to fetch product');
    return response.json();
  },

  // Cart
  getCart: async (customerId: string) => {
    const response = await fetch(`${API_BASE_URL}/api/cart/${customerId}`);
    if (!response.ok) throw new Error('Failed to fetch cart');
    return response.json();
  },

  addToCart: async (customerId: string, item: any) => {
    const response = await fetch(`${API_BASE_URL}/api/cart/${customerId}/items`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(item),
    });
    if (!response.ok) throw new Error('Failed to add to cart');
    return response.json();
  },

  removeFromCart: async (customerId: string, itemId: string) => {
    const response = await fetch(`${API_BASE_URL}/api/cart/${customerId}/items/${itemId}`, {
      method: 'DELETE',
    });
    if (!response.ok) throw new Error('Failed to remove from cart');
    return response.json();
  },

  // Orders
  createOrder: async (order: any) => {
    const response = await fetch(`${API_BASE_URL}/api/orders`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(order),
    });
    if (!response.ok) throw new Error('Failed to create order');
    return response.json();
  },

  getOrder: async (orderId: string) => {
    const response = await fetch(`${API_BASE_URL}/api/orders/${orderId}`);
    if (!response.ok) throw new Error('Failed to fetch order');
    return response.json();
  },

  getCustomerOrders: async (customerId: string) => {
    const response = await fetch(`${API_BASE_URL}/api/orders/customer/${customerId}`);
    if (!response.ok) throw new Error('Failed to fetch orders');
    return response.json();
  },

  // Payment
  processPayment: async (payment: any) => {
    const response = await fetch(`${API_BASE_URL}/api/payments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payment),
    });
    if (!response.ok) throw new Error('Failed to process payment');
    return response.json();
  },
};
