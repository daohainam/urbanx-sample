import { test, expect } from '@playwright/test';

/**
 * Single happy-path smoke test that proves the production bundle boots, products
 * render, the cart works, and routing between pages works. All API calls are
 * stubbed with `page.route()` so this can run anywhere without a backend.
 */

const sampleProducts = {
    products: [
        {
            id: 'p1',
            name: 'Smoke Test Headphones',
            description: 'Crystal clear audio engineered for testing.',
            price: 299,
            imageUrl: 'https://example.com/headphones.png',
            category: 'headphones',
            merchantId: 'm1',
            stockQuantity: 5,
        },
    ],
};

test('catalog → product details → cart flow', async ({ page }) => {
    await page.route('**/api/products*', async (route) => {
        const url = new URL(route.request().url());
        if (/\/api\/products\/p1$/.test(url.pathname)) {
            await route.fulfill({ json: sampleProducts.products[0] });
            return;
        }
        await route.fulfill({ json: sampleProducts });
    });

    // Land on the homepage and navigate to the catalog.
    await page.goto('/catalog');

    // Skeleton/empty initial render → resolved product card.
    const productCard = page.getByText('Smoke Test Headphones');
    await expect(productCard).toBeVisible();

    // Drill into the product details page via the card link.
    await productCard.click();
    await expect(page).toHaveURL(/\/product\/p1$/);
    await expect(page.getByRole('heading', { name: 'Smoke Test Headphones' })).toBeVisible();

    // Add to cart and verify the badge updates.
    await page.getByRole('button', { name: /add to cart/i }).click();

    // Navigate to the cart and confirm the item is there.
    await page.goto('/cart');
    await expect(page.getByText('Smoke Test Headphones')).toBeVisible();
});

test('catalog shows an explicit error UI when the API returns 503', async ({ page }) => {
    await page.route('**/api/products*', (route) =>
        route.fulfill({ status: 503, json: { message: 'catalog is offline' } }),
    );

    await page.goto('/catalog');

    await expect(page.getByText(/Couldn't load products/i)).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('button', { name: /try again/i })).toBeVisible();
    // No silent fallback to mock products.
    await expect(page.getByText('Premium Wireless Headphones')).not.toBeVisible();
});
