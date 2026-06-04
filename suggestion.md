# EcomX - Feature Suggestions for Future Development

> Inspired by major e-commerce platforms. Suggested name: **EcomX**

---

## Current Features (Already Implemented)

| Feature | Status |
|---------|--------|
| User Authentication | ✅ |
| Product Management | ✅ |
| Category Management | ✅ |
| Shopping Cart | ✅ |
| Wishlist | ✅ |
| Order Management | ✅ |
| Product Reviews & Ratings | ✅ |
| Banner/Promotion System | ✅ |
| Search & Filters | ✅ |
| User Profile Management | ✅ |
| Address Book Management | ✅ |
| Coupon & Discount System | ✅ |
| Return & Refund System | ✅ |
| User Activity Tracking | ✅ |
| Recently Viewed Products | ✅ |
| Personalized Recommendations | ✅ |
| Product Image Gallery (Multi-Image) | ✅ |
| Product Variants (Size/Color) | ✅ |
| Order Invoice PDF Download | ✅ |
| Admin Notifications (In-App) | ✅ |
| Admin Sales Analytics Dashboard (Charts) | ✅ |
| Global Search with Autosuggest (Recent, Suggestions, Categories) | ✅ |
| Account Lockout after N Failed Logins | ✅ |

---

## Features to Implement (Priority Order)

### 🔴 High Priority

1. **Email Integration**
   - Activate `NotificationService` email templates (already written but commented out)
   - Integrate SMTP (MailKit/SendGrid) for order confirmation, shipping updates
   - Password reset via email

2. **Payment Gateway Integration**
   - Razorpay / Stripe / PayPal integration
   - UPI, Credit/Debit Cards, Net Banking, Wallets
   - Save payment methods for future purchases
   - EMI options

3. ~~**Admin Sales Analytics Charts**~~ ✅ Done
   - Chart.js line/bar/doughnut/pie charts on dedicated Analytics tab
   - Daily / Weekly / Monthly revenue trend with period toggle
   - Top-selling products horizontal bar chart + detail table
   - Category-wise sales doughnut chart
   - Order status pie chart
   - Low stock alert table
   - KPIs: Total Revenue, Orders, Avg Order Value, Pending Orders
   - **Role-based visibility:** Revenue, AOV, top products, category breakdown, and order amounts visible to **Super Admin (Admin role) only**; SubAdmin sees order counts, pending, products, and users
   - Backend endpoints: `/api/analytics/overview`, `/revenue`, `/top-products`, `/category-breakdown`, `/order-status`, `/low-stock`
   - Revenue endpoint guarded with `[Authorize(Roles = "Admin")]` at the API layer

4. **Two-Factor Authentication (2FA) for Admin & SubAdmin** 🔐
   - TOTP-based (Google Authenticator / Authy / Microsoft Authenticator)
   - QR code setup from profile, 6-digit code on login
   - 10 one-time backup recovery codes
   - Activate from profile page; enforced on next login
   - `User.TwoFactorSecret` + `User.TwoFactorEnabled` columns
   - Endpoints: `/api/auth/2fa/setup`, `/api/auth/2fa/verify`, `/api/auth/2fa/disable`
   - Modified `/api/auth/login` to require TOTP after password verify
   - Stack: `Otp.NET` (backend), `qrcode` npm + custom 6-digit input (frontend)

5. ~~**Account Lockout after N Failed Logins**~~ ✅ Done
   - 5 failed attempts → 15-minute lockout
   - Counter resets on successful login
   - User model: `FailedLoginAttempts`, `LockoutEnd`, `LockoutReason`
   - Login returns **HTTP 423 Locked** with remaining time
   - Admin can manually unlock: `POST /api/auth/users/{id}/unlock`
   - Lockout status shown in admin user list
   - Successful password change clears any active lockout

### 🟡 Medium Priority

4. ~~**Product Variants (Size/Color)**~~ ✅ Done
   - `ProductVariant` entity with own price, stock, and sort order
   - Add / edit / delete variants from admin product modal
   - Variant selector on product detail page
   - Cart and checkout respect the selected variant

5. **Social Login (Google OAuth)**
   - Login via Google account
   - Auto-create user profile from Google data
   - Link existing account with social login

6. **Wishlist Sharing & Alerts**
   - Share wishlist via link/social media
   - Price drop alerts for wishlist items
   - Back-in-stock notifications

7. **Seller Dashboard (Multi-Vendor)**
   - Seller registration and verification
   - Product listing, inventory, and pricing management
   - Order fulfillment and commission tracking

8. **Loyalty & Rewards Program**
   - Points for every purchase
   - Referral bonus points
   - Redeemable rewards on checkout

9. **Product Comparison**
   - Compare up to 4 products side by side
   - Specs, price, and rating comparison

### 🟢 Nice to Have

10. **Flash Sales & Deal Timers**
    - Limited-time deals with countdown
    - Lightning deals section on homepage

11. **Stock Alerts**
    - "Notify Me" button for out-of-stock products
    - Auto-email when product is restocked

12. **Gift Cards & Gifting**
    - Purchase and send digital gift cards
    - Gift wrapping option at checkout

13. **Live Chat Support**
    - AI chatbot for common queries
    - Escalation to human agent

14. **Product Q&A Section**
    - Customers can ask questions on product pages
    - Admin/other customers can answer
    - Builds community and reduces support load

15. **Voice Search**
    - Integrate voice-based product search
    - Multilingual support

16. **AR/VR Product Preview**
    - Virtual try-on for fashion items
    - 360° product view for electronics/furniture

17. **Multi-Language & Multi-Currency**
    - Language switcher (English, Hindi, etc.)
    - Currency selector for international users

18. **Progressive Web App (PWA)**
    - Offline browsing capability
    - Push notifications
    - App-like experience on mobile

---

## Tech Stack Recommendations

| Layer | Technology |
|-------|------------|
| Backend | .NET 10 Web API |
| Frontend | Angular 21 |
| Database | SQL Server / PostgreSQL |
| Caching | Redis |
| Search | Elasticsearch / Algolia |
| Payment | Razorpay / Stripe |
| Storage | Azure Blob / AWS S3 |
| Charts | Chart.js 4.x |
| CI/CD | GitHub Actions |

---

## Implementation Roadmap

```
Phase 1 (MVP Enhancement) ✅
├── User Authentication ✅
├── Product & Category Management ✅
├── Shopping Cart & Wishlist ✅
├── Order Management ✅
├── Search & Filters ✅
├── User Profile & Address Book ✅

Phase 2 (Growth) ✅
├── Coupon & Discount System ✅
├── Return & Refund System ✅
├── Activity Tracking & Recommendations ✅
├── Product Image Gallery ✅
├── Order Invoice PDF ✅
├── Admin Notifications (In-App) ✅

Phase 3 (Scale)
├── Email Integration
├── Payment Gateway
├── Admin Analytics Charts ✅
├── Product Variants ✅
├── Social Login
├── Wishlist Sharing & Alerts
├── Seller Dashboard
└── Loyalty Program

Phase 4 (Innovation)
├── Flash Sales & Deal Timers
├── Stock Alerts
├── Gift Cards & Gifting
├── Live Chat Support
├── Product Q&A
├── Voice Search
├── AR/VR Preview
├── Multi-Language & Multi-Currency
└── PWA
```
---
## Next Recommended Feature

**Email Integration** (High Priority #1)

Why this next:
- NotificationService already has email templates written but commented out
- SMTP integration (MailKit) is standard and well-documented
- Enables order confirmation, shipping, password reset emails
- Critical for a production-grade e-commerce platform





---

### Coupon Distribution (Future Enhancements)
- Welcome coupon auto-generated on registration (e.g., `WELCOME-{userid}`)
- Referral coupon — give user shareable code; both referrer and friend get discount
- Birthday coupon — auto-generate from profile DOB
- Cart abandonment — auto-generate and email coupon to users who left items in cart
- Bulk CSV upload — admin uploads pre-generated codes to distribute
- Batch code generation API — `POST /api/coupons/generate-batch` for N unique codes

---

### Analytics Dashboard (Future Enhancements)
- **Cohort & retention charts** — repeat-purchase rate, customer lifetime value
- **Cohort heatmap** — signup month vs. order month
- **Real-time KPI ticker** — websocket-driven live revenue / orders
- **Forecast** — simple linear / moving-average projection for next 30 days
- **Custom date-range filter** — pick any start / end date (not just fixed periods)
- **Export** — PDF and CSV download of charts and tables
- **Comparison mode** — compare this month vs. last, this year vs. last
- **Top customers leaderboard** — by spend, order count, or AOV
- **Coupon performance report** — usage, revenue uplift, ROI per coupon
- **Refund analytics** — return rate by category, refund amount trend
- **Geographic breakdown** — sales by city / state
- **Traffic → conversion funnel** — sessions → cart → checkout → paid
- **Drill-down modals** — click a chart bar/pie slice to see the underlying orders

---

*Last Updated: June 5, 2026*
