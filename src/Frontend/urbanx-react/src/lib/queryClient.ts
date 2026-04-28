import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '../services/http';

/**
 * App-wide react-query client. Defaults are tuned for an authenticated SPA where
 * server state may invalidate while the user is away (token, cart, orders).
 */
export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            // Treat data as fresh for 30s — refetch on remount/refocus afterwards.
            staleTime: 30_000,
            // Keep cached results around for 5 minutes after the last subscriber.
            gcTime: 5 * 60_000,
            refetchOnWindowFocus: true,
            // Retry transient network/timeout/5xx, never retry 4xx (auth, validation).
            retry: (failureCount, error) => {
                if (error instanceof ApiError) {
                    if (!error.isRetriable) return false;
                }
                return failureCount < 2;
            },
            retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 8_000),
        },
        mutations: {
            // Mutations are user-initiated; don't auto-retry (would double-charge).
            retry: false,
        },
    },
});
