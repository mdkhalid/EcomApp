import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { guestGuard } from './guards/guest.guard';
import { customerGuard } from './guards/customer.guard';
import { adminGuard } from './guards/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  { path: 'products', loadComponent: () => import('./components/products/products.component').then(m => m.ProductsComponent) },
  { path: 'products/:id', loadComponent: () => import('./components/product-detail/product-detail.component').then(m => m.ProductDetailComponent) },
  { path: 'login', loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent), canActivate: [guestGuard] },
  { path: 'register', loadComponent: () => import('./components/register/register.component').then(m => m.RegisterComponent), canActivate: [guestGuard] },
  { path: 'forgot-password', loadComponent: () => import('./components/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent), canActivate: [guestGuard] },
  { path: 'reset-password', loadComponent: () => import('./components/reset-password/reset-password.component').then(m => m.ResetPasswordComponent), canActivate: [guestGuard] },
  { path: 'verify-email', loadComponent: () => import('./components/verify-email/verify-email.component').then(m => m.VerifyEmailComponent) },
  { path: 'cart', loadComponent: () => import('./components/cart/cart.component').then(m => m.CartComponent), canActivate: [customerGuard] },
  { path: 'checkout', loadComponent: () => import('./components/checkout/checkout.component').then(m => m.CheckoutComponent), canActivate: [customerGuard] },
  { path: 'orders', loadComponent: () => import('./components/orders/orders.component').then(m => m.OrdersComponent), canActivate: [customerGuard] },
  { path: 'orders/:id', loadComponent: () => import('./components/order-detail/order-detail.component').then(m => m.OrderDetailComponent), canActivate: [customerGuard] },
  { path: 'wishlist', loadComponent: () => import('./components/wishlist/wishlist.component').then(m => m.WishlistComponent), canActivate: [customerGuard] },
  { path: 'profile', loadComponent: () => import('./components/profile/profile.component').then(m => m.ProfileComponent), canActivate: [authGuard] },
  { path: 'return-policy', loadComponent: () => import('./components/return-policy/return-policy.component').then(m => m.ReturnPolicyComponent) },
  {
    path: 'admin',
    canActivate: [adminGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./components/admin/dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'products', loadComponent: () => import('./components/admin/products/admin-products.component').then(m => m.AdminProductsComponent) },
      { path: 'orders', loadComponent: () => import('./components/admin/orders/admin-orders.component').then(m => m.AdminOrdersComponent) },
      { path: 'users', loadComponent: () => import('./components/admin/users/admin-users.component').then(m => m.AdminUsersComponent) },
      { path: 'categories', loadComponent: () => import('./components/admin/categories/admin-categories.component').then(m => m.AdminCategoriesComponent) },
      { path: 'banners', loadComponent: () => import('./components/admin/banners/admin-banners.component').then(m => m.AdminBannersComponent) },
      { path: 'coupons', loadComponent: () => import('./components/admin/coupons/admin-coupons.component').then(m => m.AdminCouponsComponent) },
      { path: 'returns', loadComponent: () => import('./components/admin/returns/admin-returns.component').then(m => m.AdminReturnsComponent) },
      { path: 'analytics', loadComponent: () => import('./components/admin/analytics/admin-analytics.component').then(m => m.AdminAnalyticsComponent) },
      { path: 'settings', loadComponent: () => import('./components/admin/settings/admin-settings.component').then(m => m.AdminSettingsComponent) }
    ]
  },
  { path: '404', loadComponent: () => import('./components/not-found/not-found.component').then(m => m.NotFoundComponent) },
  { path: '**', redirectTo: '/404' }
];
