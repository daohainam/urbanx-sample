import React from 'react';
import { render, type RenderOptions } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, type MemoryRouterProps } from 'react-router-dom';
import { CartProvider } from '../context/CartContext';

interface ProvidersOptions {
    /** Initial route for MemoryRouter. Defaults to "/". */
    route?: string;
    routerProps?: Omit<MemoryRouterProps, 'children' | 'initialEntries'>;
    /** Pre-built QueryClient if a test needs to inspect cache. */
    queryClient?: QueryClient;
}

/**
 * Builds a fresh `QueryClient` per test (no shared cache, no retries) and renders
 * the UI inside it plus `MemoryRouter`. Use for any component that uses react-query
 * or react-router hooks. Returns the original RTL render result plus the client.
 */
export function renderWithProviders(
    ui: React.ReactElement,
    { route = '/', routerProps, queryClient, ...renderOptions }: ProvidersOptions & Omit<RenderOptions, 'wrapper'> = {},
) {
    const client =
        queryClient ??
        new QueryClient({
            defaultOptions: {
                queries: { retry: false, gcTime: 0, staleTime: 0 },
                mutations: { retry: false },
            },
        });

    const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
        <QueryClientProvider client={client}>
            <CartProvider>
                <MemoryRouter initialEntries={[route]} {...routerProps}>
                    {children}
                </MemoryRouter>
            </CartProvider>
        </QueryClientProvider>
    );

    return {
        ...render(ui, { wrapper: Wrapper, ...renderOptions }),
        queryClient: client,
    };
}
