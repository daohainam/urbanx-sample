import '@testing-library/jest-dom/vitest';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { cleanup } from '@testing-library/react';
import { server } from './msw/server';

// Start MSW before any test runs; require explicit handlers per test for unmocked routes.
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
    server.resetHandlers();
    cleanup();
    window.localStorage.clear();
});
afterAll(() => server.close());
