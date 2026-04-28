import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { renderWithProviders } from '../test/renderWithProviders';
import { server } from '../test/msw/server';
import CatalogPage from './CatalogPage';

vi.mock('../services/auth', () => ({
    userManager: {
        getUser: () => Promise.resolve(null),
    },
}));

describe('CatalogPage', () => {
    it('renders product cards once the API resolves', async () => {
        renderWithProviders(<CatalogPage />, { route: '/catalog' });

        // Initial render shows skeletons (no product names yet).
        expect(screen.queryByText('Test Headphones')).not.toBeInTheDocument();

        await waitFor(() => {
            expect(screen.getByText('Test Headphones')).toBeInTheDocument();
            expect(screen.getByText('Test Watch')).toBeInTheDocument();
        });
    });

    it('shows an explicit error state when the API fails — no silent mock fallback', async () => {
        server.use(
            http.get('/api/products', () =>
                HttpResponse.json({ message: 'catalog is offline' }, { status: 503 }),
            ),
        );

        renderWithProviders(<CatalogPage />, { route: '/catalog' });

        await waitFor(
            () => {
                expect(screen.getByRole('alert')).toBeInTheDocument();
            },
            { timeout: 5000 },
        );

        expect(screen.getByText(/Couldn't load products/i)).toBeInTheDocument();
        // Critically, no headphones/watches should appear from any leftover mock data.
        expect(screen.queryByText('Premium Wireless Headphones')).not.toBeInTheDocument();
        // Retry button is present for 5xx (retriable).
        expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
    });

    it('recovers via the retry button after a transient failure', async () => {
        let calls = 0;
        server.use(
            http.get('/api/products', () => {
                calls += 1;
                if (calls === 1) return HttpResponse.json(null, { status: 503 });
                return HttpResponse.json({
                    products: [
                        {
                            id: 'p99',
                            name: 'Recovered Product',
                            description: '',
                            price: 1,
                            imageUrl: '',
                            category: '',
                            merchantId: 'm1',
                            stockQuantity: 1,
                        },
                    ],
                });
            }),
        );

        renderWithProviders(<CatalogPage />, { route: '/catalog' });

        const retryBtn = await screen.findByRole('button', { name: /try again/i });

        await userEvent.setup().click(retryBtn);

        await waitFor(() => {
            expect(screen.getByText('Recovered Product')).toBeInTheDocument();
        });
    });

    it('shows an empty-state when the API returns no products', async () => {
        server.use(http.get('/api/products', () => HttpResponse.json({ products: [] })));

        renderWithProviders(<CatalogPage />, { route: '/catalog' });

        await waitFor(() => {
            expect(screen.getByText(/no products found/i)).toBeInTheDocument();
        });
    });
});
