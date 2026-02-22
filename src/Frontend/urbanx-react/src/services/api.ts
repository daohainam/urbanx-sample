import type { Product, Order, PlaceOrderRequest } from '../types';
import { userManager } from './auth';

const API_BASE_URL = '/api';

async function getAuthHeaders(): Promise<Record<string, string>> {
    const user = await userManager.getUser();
    if (user?.access_token) {
        return { 'Authorization': `Bearer ${user.access_token}` };
    }
    return {};
}

async function handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: response.statusText }));
        throw new Error(error.message || response.statusText);
    }
    return response.json();
}

export const catalogService = {
    async getProducts(category?: string, search?: string): Promise<Product[]> {
        let url = `${API_BASE_URL}/products`;
        const params = new URLSearchParams();
        if (category) params.append('category', category);
        if (search) params.append('search', search);
        if (params.toString()) url += `?${params.toString()}`;

        const data = await handleResponse<{ products: Product[] }>(await fetch(url));
        return data.products ?? [];
    },

    async getProductById(id: string): Promise<Product> {
        return handleResponse<Product>(await fetch(`${API_BASE_URL}/products/${id}`));
    }
};

export const orderService = {
    async getOrders(customerId: string): Promise<Order[]> {
        const headers = await getAuthHeaders();
        return handleResponse<Order[]>(await fetch(`${API_BASE_URL}/orders/customer/${customerId}`, { headers }));
    },

    async getOrder(orderId: string): Promise<Order> {
        const headers = await getAuthHeaders();
        return handleResponse<Order>(await fetch(`${API_BASE_URL}/orders/${orderId}`, { headers }));
    },

    async placeOrder(orderData: PlaceOrderRequest): Promise<Order> {
        const headers = await getAuthHeaders();
        return handleResponse<Order>(await fetch(`${API_BASE_URL}/orders`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', ...headers },
            body: JSON.stringify(orderData)
        }));
    }
};

export const accountService = {
    async register(email: string, password: string, fullName: string) {
        return handleResponse(await fetch(`${API_BASE_URL}/account/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password, fullName })
        }));
    }
};

