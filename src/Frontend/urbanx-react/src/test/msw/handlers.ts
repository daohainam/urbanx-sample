import { http, HttpResponse } from 'msw';
import type { Order, Product } from '../../types';

/**
 * Default MSW handlers for happy-path responses. Tests can override individual
 * routes via `server.use(...)` to simulate failures, slow responses, etc.
 */

const sampleProducts: Product[] = [
    {
        id: 'p1',
        name: 'Test Headphones',
        description: 'Crystal clear audio.',
        price: 199,
        imageUrl: 'https://example.com/headphones.png',
        category: 'headphones',
        merchantId: 'm1',
        stockQuantity: 5,
    },
    {
        id: 'p2',
        name: 'Test Watch',
        description: 'Stylish timepiece.',
        price: 299,
        imageUrl: 'https://example.com/watch.png',
        category: 'watches',
        merchantId: 'm1',
        stockQuantity: 3,
    },
];

export const sampleOrder: Order = {
    id: 'o1',
    customerId: 'cust-1',
    orderNumber: 'ORD-1001',
    items: [
        {
            id: 'oi1',
            orderId: 'o1',
            productId: 'p1',
            productName: 'Test Headphones',
            quantity: 1,
            unitPrice: 199,
            merchantId: 'm1',
        },
    ],
    totalAmount: 199,
    status: 'Pending',
    createdAt: '2026-04-28T10:00:00Z',
    updatedAt: '2026-04-28T10:00:00Z',
    shippingAddress: '123 Test St, Test City, 10001, USA',
    statusHistory: [],
};

export const handlers = [
    http.get('/api/products', () => HttpResponse.json({ products: sampleProducts })),
    http.get('/api/products/:id', ({ params }) => {
        const product = sampleProducts.find((p) => p.id === params.id);
        return product ? HttpResponse.json(product) : new HttpResponse(null, { status: 404 });
    }),
    http.get('/api/orders/customer/:customerId', () => HttpResponse.json([sampleOrder])),
    http.get('/api/orders/:id', ({ params }) => {
        return params.id === sampleOrder.id
            ? HttpResponse.json(sampleOrder)
            : new HttpResponse(null, { status: 404 });
    }),
    http.post('/api/orders', () => HttpResponse.json(sampleOrder)),
    http.post('/api/account/register', () => HttpResponse.json({ ok: true })),
];
