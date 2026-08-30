import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should register a new user', async ({ page }) => {
    await page.click('a[href="/register"]');
    await expect(page).toHaveURL('/register');

    const testEmail = `test${Date.now()}@example.com`;
    await page.fill('input[name="email"]', testEmail);
    await page.fill('input[name="password"]', 'TestPass123!');
    await page.fill('input[name="confirmPassword"]', 'TestPass123!');
    await page.fill('input[name="firstName"]', 'Test');
    await page.fill('input[name="lastName"]', 'User');
    await page.click('button[type="submit"]');

    await expect(page.locator('.toast-success')).toBeVisible({ timeout: 10000 });
    await expect(page).toHaveURL('/products');
  });

  test('should login with valid credentials', async ({ page }) => {
    await page.click('a[href="/login"]');
    await expect(page).toHaveURL('/login');

    await page.fill('input[name="email"]', 'demo@ecom.com');
    await page.fill('input[name="password"]', 'Demo@123');
    await page.click('button[type="submit"]');

    await expect(page.locator('.toast-success')).toBeVisible({ timeout: 10000 });
    await expect(page).toHaveURL('/products');
  });

  test('should show error for invalid login', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'wrong@example.com');
    await page.fill('input[name="password"]', 'WrongPass123!');
    await page.click('button[type="submit"]');

    await expect(page.locator('.toast-error')).toBeVisible({ timeout: 5000 });
  });

  test('should navigate to forgot password', async ({ page }) => {
    await page.goto('/login');
    await page.click('a[href="/forgot-password"]');
    await expect(page).toHaveURL('/forgot-password');
  });

  test('should verify email page loads', async ({ page }) => {
    await page.goto('/verify-email');
    await expect(page.locator('h1')).toContainText('Verify Email');
  });

  test('should logout successfully', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'demo@ecom.com');
    await page.fill('input[name="password"]', 'Demo@123');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/products');

    await page.click('button:has-text("Logout"), a:has-text("Logout")');
    await expect(page.locator('.toast-success')).toBeVisible({ timeout: 5000 });
    await expect(page).toHaveURL('/products');
  });
});

test.describe('Protected Routes Redirect', () => {
  test('should redirect to login when accessing cart without auth', async ({ page }) => {
    await page.goto('/cart');
    await expect(page).toHaveURL('/login');
    await expect(page).toHaveURL(/.*returnUrl=%2Fcart/);
  });

  test('should redirect to login when accessing checkout without auth', async ({ page }) => {
    await page.goto('/checkout');
    await expect(page).toHaveURL('/login');
  });

  test('should redirect to login when accessing orders without auth', async ({ page }) => {
    await page.goto('/orders');
    await expect(page).toHaveURL('/login');
  });

  test('should redirect to login when accessing wishlist without auth', async ({ page }) => {
    await page.goto('/wishlist');
    await expect(page).toHaveURL('/login');
  });

  test('should redirect to login when accessing profile without auth', async ({ page }) => {
    await page.goto('/profile');
    await expect(page).toHaveURL('/login');
  });
});