import { defineConfig, devices } from '@playwright/test';

const PORT = 4173; // Default `vite preview` port.

/**
 * Playwright runs against `vite preview` (the prod build) so the smoke tests
 * exercise what real users see. Backend calls are intercepted with `page.route()`
 * inside individual tests — no real backend required.
 */
export default defineConfig({
    testDir: './e2e',
    fullyParallel: true,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    workers: process.env.CI ? 1 : undefined,
    reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
    use: {
        baseURL: `http://localhost:${PORT}`,
        trace: 'on-first-retry',
        screenshot: 'only-on-failure',
        video: 'retain-on-failure',
    },
    projects: [
        {
            name: 'chromium',
            use: { ...devices['Desktop Chrome'] },
        },
    ],
    webServer: {
        // `npm run build` then `vite preview` — same artifact CI deploys.
        command: 'npm run build && npm run preview -- --port 4173 --strictPort',
        url: `http://localhost:${PORT}`,
        reuseExistingServer: !process.env.CI,
        timeout: 180_000,
        env: {
            // Vite reads .env.production for the build; just guarantee the required vars are set.
            VITE_API_BASE_URL: '/api',
            VITE_BFF_BASE_URL: '/bff',
            VITE_OIDC_CLIENT_ID: 'urbanx-spa',
            VITE_OIDC_SCOPES: 'openid profile email',
            VITE_APP_VERSION: 'e2e',
        },
    },
});
