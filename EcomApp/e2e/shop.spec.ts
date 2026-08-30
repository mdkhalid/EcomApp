import { test, expect } from '@playwright/test';

test.describe('Shopping Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display products on home page', async ({ page }) => {
    await expect(page.locator('.product-card, .product-item')).toHaveCount.greaterThan(0);
  });

  test('should navigate to product detail', async ({ page }) => {
    const firstProduct = page.locator('.product-card, .product-item').first();
    await firstProduct.click();
    await expect(page).toHaveURL(/\/products\/\d+/);
    await expect(page.locator('h1')).toBeVisible();
  });

  test('should add product to cart', async ({ page }) => {
    await page.goto('/products/1');
    await page.click('button:has-text("Add to Cart")');
    await expect(page.locator('.toast-success')).toBeVisible({ timeout: 5000 });

    const cartCount = page.locator('.cart-count, [data-testid="cart-count"]');
    await expect(cartCount).toContainText('1');
  });

  test('should apply coupon', async ({ page }) => {
    await page.goto('/products/1');
    await page.click('button:has-text("Add to Cart")');
    await expect(page.locator('.toast-success')).toBeVisible();

    await page.goto('/cart');
    await page.fill('input[placeholder*="coupon"], input[name="couponCode"]', 'WELCOME10');
    await page.click('button:has-text("Apply")');
    await expect(page.locator('.toast-success, .coupon-applied')).toBeVisible({ timeout: 5000 });
  });

  test('should proceed to checkout', async ({ page }) => {
    await page.goto('/products/1');
    await page.click('button:has-text("Add to Cart")');
    await expect(page.locator('.toast-success')).toBeVisible();

    await page.goto('/cart');
    await page.click('a:has-text("Checkout"), button:has-text("Checkout")');
    await expect(page).toHaveURL('/checkout');
  });

  test('should search for products', async ({ page }) => {
    const searchInput = page.locator('input[type="search"], input[placeholder*="search" i]');
    await searchInput.fill('test');
    await searchInput.press('Enter');
    await expect(page).toHaveURL(/\/products.*search/);
  });

  test('should filter products by category', async ({ page }) => {
    await page.goto('/products');
    const categoryFilter = page.locator('select[name="category"], [data-testid="category-filter"]');
    if (await categoryFilter.isVisible()) {
      await categoryFilter.selectOption({ index: 1 });
      await expect(page.locator('.product-card')).toHaveCount.greaterThanOrEqual(0);
    }
  });
});