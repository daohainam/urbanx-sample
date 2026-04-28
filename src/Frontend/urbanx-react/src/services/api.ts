import type { Product, Order, PlaceOrderRequest } from '../types';
import { request } from './http';

export const catalogService = {
    async getProducts(category?: string, search?: string, signal?: AbortSignal): Promise<Product[]> {
        const params = new URLSearchParams();
        if (category) params.append('category', category);
        if (search) params.append('search', search);
        const qs = params.toString();
        const data = await request<{ products: Product[] }>(
            `/products${qs ? `?${qs}` : ''}`,
            { anonymous: true, signal },
        );
        return data.products ?? [];
    },

    getProductById(id: string, signal?: AbortSignal): Promise<Product> {
        return request<Product>(`/products/${id}`, { anonymous: true, signal });
    },
};

export const orderService = {
    getOrders(customerId: string, signal?: AbortSignal): Promise<Order[]> {
        return request<Order[]>(`/orders/customer/${encodeURIComponent(customerId)}`, { signal });
    },

    getOrder(orderId: string, signal?: AbortSignal): Promise<Order> {
        return request<Order>(`/orders/${encodeURIComponent(orderId)}`, { signal });
    },

    placeOrder(orderData: PlaceOrderRequest, signal?: AbortSignal): Promise<Order> {
        return request<Order>('/orders', { method: 'POST', json: orderData, signal });
    },
};

export const accountService = {
    register(email: string, password: string, fullName: string, signal?: AbortSignal) {
        return request<unknown>('/account/register', {
            method: 'POST',
            anonymous: true,
            json: { email, password, fullName },
            signal,
        });
    },
};
