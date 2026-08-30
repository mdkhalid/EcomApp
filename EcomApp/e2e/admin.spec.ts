import { test, expect } from '@playwright/test';

test.describe('Admin Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'admin@ecom.com');
    await page.fill('input[name="password"]', 'Admin@123');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/products');
    await page.goto('/admin');
  });

  test('should load admin dashboard', async ({ page }) => {
    await expect(page).toHaveURL('/admin');
    await expect(page.locator('h1, h2')).toContainText('Admin');
  });

  test('should navigate to products management', async ({ page }) => {
    await page.click('a:has-text("Products"), button:has-text("Products")');
    await expect(page.locator('table, .product-list')).toBeVisible();
  });

  test('should navigate to orders management', async ({ page }) => {
    await page.click('a:has-text("Orders"), button:has-text("Orders")');
    await expect(page.locator('table, .order-list')).toBeVisible();
  });

  test('should navigate to users management', async ({ page }) => {
    await page.click('a:has-text("Users"), button:has-text("Users")');
    await expect(page.locator('table, .user-list')).toBeVisible();
  });

  test('should navigate to categories management', async ({ page }) => {
    await page.click('a:has-text("Categories"), button:has-text("Categories")');
    await expect(page.locator('table, .category-list')).toBeVisible();
  });

  test('should navigate to coupons management', async ({ page }) => {
    await page.click('a:has-text("Coupons"), button:has-text("Coupons")');
    await expect(page.locator('table, .coupon-list')).toBeVisible();
  });

  test('should navigate to analytics', async ({ page }) => {
    await page.click('a:has-text("Analytics"), button:has-text("Analytics")');
    await expect(page.locator('canvas, .chart-container')).toBeVisible();
  });

  test('should block non-admin users from admin', async ({ page }) => {
    await page.goto('/logout');
    await page.goto('/login');
    await page.fill('input[name="email"]', 'demo@ecom.com');
    await page.fill('input[name="password"]', 'Demo@123');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/products');

    await page.goto('/admin');
    await expect(page).toHaveURL('/products');
    await expect(page.locator('.toast-error, .toast-warning')).toContainText(/unauthorized|access denied|admin/i);
  });

  test('should create a new product', async ({ page }) => {
    await page.click('a:has-text("Products"), button:has-text("Products")');
    await page.click('button:has-text("Add Product"), button:has-text("Create")');
    await expect(page.locator('h2, h3')).toContainText(/add|create/i);

    await page.fill('input[name="name"]', `Test Product ${Date.now()}`);
    await page.fill('input[name="description"]', 'Test description');
    await page.fill('input[name="price"]', '99.99');
    await page.fill('input[name="stock"]', '10');
    await page.click('button[type="submit"]:has-text("Save"), button[type="submit"]:has-text("Create")');

    await expect(page.locator('.toast-success')).toBeVisible({ timeout: 10000 });
  });
});