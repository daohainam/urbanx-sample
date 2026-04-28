/**
 * Centralised query keys. Keep them here so cache invalidation
 * (e.g. after `placeOrder` succeeds) can target by hierarchy.
 */
export const queryKeys = {
    products: {
        list: (filters: { category?: string; search?: string }) => ['products', 'list', filters] as const,
        detail: (id: string) => ['products', 'detail', id] as const,
    },
    orders: {
        list: (customerId: string) => ['orders', 'list', customerId] as const,
        detail: (orderId: string) => ['orders', 'detail', orderId] as const,
    },
} as const;
