const API_BASE_URL = '/api';

async function handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: response.statusText }));
        throw new Error(error.message || response.statusText);
    }
    return response.json();
}

export const catalogService = {
    async getProducts(category?: string, search?: string) {
        let url = `${API_BASE_URL}/products`;
        const params = new URLSearchParams();
        if (category) params.append('category', category);
        if (search) params.append('search', search);
        if (params.toString()) url += `?${params.toString()}`;

        return handleResponse(await fetch(url));
    },

    async getProductById(id: string) {
        return handleResponse(await fetch(`${API_BASE_URL}/products/${id}`));
    }
};

export const orderService = {
    async getCart(customerId: string) {
        return handleResponse(await fetch(`${API_BASE_URL}/cart/${customerId}`));
    },

    async addToCart(customerId: string, productId: string, quantity: number) {
        return handleResponse(await fetch(`${API_BASE_URL}/cart/${customerId}/items`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ productId, quantity })
        }));
    },

    async placeOrder(orderData: any) {
        return handleResponse(await fetch(`${API_BASE_URL}/orders`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(orderData)
        }));
    }
};
