# EcomApp — Implementation Roadmap (Aug 2026)

**Stack:** .NET 10 Web API + Angular 21 + SQL Server (EF Core) + MailKit
**Basis:** Verified against live code on 2026-08-28. Supersedes `PRODUCTION_AUDIT.md` items that are already fixed and extends `suggestion.md`.

---

## 0. Current State — What Is Already Good ✅

The codebase has matured significantly since the June audit. These are verified fixed/done — the architecture patterns are sound and should be **kept and reused**, not reworked:

| Area | Status | Notes |
|------|--------|-------|
| Architecture (Repository + DTO + Mapster) | ✅ Good | Keep — consistent across 18 controllers |
| Notification pipeline (Strategy + background queue) | ✅ Done | Email live via MailKit; SMS/WhatsApp drop-in ready. Reuse for every feature below |
| Password reset | ✅ Done | Hashed token, single-use, 30-min expiry, no enumeration |
| Admin runtime settings (`SettingsCatalog` + `ISettingsProvider`) | ✅ Done | Extend this pattern for new config — never hard-code |
| JWT auth + refresh rotation + lockout + roles | ✅ Done | Solid baseline |
| `environment.ts` / `environment.prod.ts` | ✅ Done | No hard-coded API host left |
| HSTS + HTTPS redirect | ✅ Done | `Program.cs:136-139` |
| JWT secret hygiene | ✅ Done | `appsettings.json` empty; real key only in gitignored `appsettings.Production.json` |
| Backend tests (`EcomApi.Tests`) | ✅ Started | 13 passing — keep growing alongside new features |
| Lazy-loaded routes, Signals, functional guards/interceptors | ✅ Done | Modern Angular 21 idioms |
| Admin settings UI (uncommitted) | ⚠️ Finish & commit | `admin-settings.service.ts` + model exist but are untracked |

**Verdict: design and code quality are good.** The remaining work is hardening, money-movement, DevOps, and growth features — not a rewrite.

---

## 1. Gap Register (verified missing as of today)

| # | Gap | Severity | Phase | Status |
|---|-----|----------|-------|--------|
| G1 | No real payment gateway (payment = free-text string) | 🔴 Critical | 2 | ✅ Resolved (Phase 2) |
| G2 | No 2FA for Admin/SubAdmin | 🔴 High | 1 | ✅ Resolved (Phase 1) |
| G3 | No email verification on signup | 🟠 High | 1 | ✅ Resolved (Phase 1) |
| G4 | No Docker / docker-compose | 🟠 High | 3 | ✅ Resolved (Phase 3) |
| G5 | No CI/CD pipeline | 🟠 High | 3 | ✅ Resolved (Phase 3) |
| G6 | Zero frontend tests (`*.spec.ts`) | 🟡 Medium | 3 | ✅ Resolved (Phase 3) |
| G7 | No security-headers middleware (CSP, X-Frame-Options, nosniff) | 🟡 Medium | 1 | ✅ Resolved (Phase 1) |
| G8 | Refresh tokens stored as plain Base64 | 🟡 Medium | 1 | ✅ Resolved (Phase 1) |
| G9 | Rate limiting in-memory, auth endpoints only | 🟡 Medium | 1 | 🟡 Partial (auth endpoints covered; not global) |
| G10 | No shipping cost / tax (GST) calculation | 🟡 Medium | 2 | ✅ Resolved (Phase 2) |
| G11 | Refunds have no money movement | 🟡 Medium | 2 | ✅ Resolved (Phase 2) |
| G12 | CORS hard-coded to `localhost:4200` | 🟡 Medium | 1 | ✅ Resolved (Phase 1) |
| G13 | `Database.Migrate()` on every startup | 🟡 Medium | 3 | ✅ Resolved (Phase 3) |
| G14 | Admin god-component (~1,233 lines) | 🟡 Medium | 3 | ✅ Resolved (Phase 3) |
| G15 | `returnUrl` not validated (open redirect) | 🟡 Medium | 1 | ✅ Resolved (Phase 1) |
| G16 | No map-inbound-claims normalization on JWT | 🟢 Low | 1 | ✅ Resolved (Phase 1) |
| G17 | Frontend route `**` silently redirects (no 404 page) | 🟢 Low | 3 | ✅ Resolved (Phase 3) |
| G18 | Stripe prod secrets not yet wired (dev runs Mock gateway; need gitignored `appsettings.Production.json`) | 🟠 High | 2/3 | ⬜ Open |

---

## Phase 1 — Security Hardening & Trust  (est. 1.5–2 weeks)

> Goal: nothing ships to real users until this phase is green. Every item reuses existing patterns.

### 1.1 Security headers middleware — fixes G7 ✅ Done (2026-08-29)
- New `SecurityHeadersMiddleware` (or `app.Use(async ...)` in `Program.cs`) setting:
  - `Content-Security-Policy` (start with `default-src 'self'; img-src 'self' data: https:;` — inline styles needed for Angular, so allow `style-src 'self' 'unsafe-inline'` initially)
  - `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- Static assets (uploads) must also pass through the header middleware — register it before `UseStaticFiles()`.
- **Test:** `curl -I` every route class (API, uploads, SPA fallback) and assert headers present.

### 1.2 Hash refresh tokens at rest — fixes G8 ✅ Done (2026-08-29)
- Add migration `HashRefreshTokens`: new column `TokenHash` (nullable first).
- On issue: store `SHA256(token)` (reuse the exact pattern from `PasswordResetToken.cs` — it already does gen/hash/validate and has tests).
- On lookup: hash incoming token, query by hash. Keep lookup indexed.
- Migration backfill: existing plain tokens can't be hashed retroactively (hash is one-way) → invalidate all existing refresh tokens once (force re-login; acceptable one-time cost). Then drop the plain column.
- **Test:** unit test mirroring `PasswordResetTokenTests`.

### 1.3 Email verification on signup — fixes G3 ✅ Done (2026-08-29)
- `User`: `EmailVerified` (bool, default false), `EmailVerificationTokenHash`, `EmailVerificationTokenExpiry`.
- Endpoints: `POST /api/auth/verify-email` (token+email), `POST /api/auth/resend-verification` (rate-limited, generic response).
- Reuse `PasswordResetToken` service for token lifecycle; reuse `EmailTemplates` for a new `VerifyEmail` template (add `{{VerifyUrl}}` placeholder, same branded wrapper).
- Dispatch via existing `INotificationQueue` — zero new infrastructure.
- Policy: verified email required before checkout; login allowed but UI banners "verify your email".
- Frontend: `verify-email` page (guest-guarded), banner on profile, resend button.
- **Security:** same no-enumeration rule as forgot-password; single-use tokens; 24h expiry.

### 1.4 TOTP 2FA for Admin/SubAdmin — fixes G2 ✅ Done (2026-08-29)
- Packages: `Otp.NET` (backend), `qrcode` (frontend).
- `User`: `TwoFactorSecret` (encrypted at rest via ASP.NET DataProtection), `TwoFactorEnabled`, plus `RecoveryCode` entity (hashed, 10 codes).
- Endpoints: `/api/auth/2fa/setup` (returns secret+OTP-auth URI), `/2fa/verify` (enables), `/2fa/disable` (requires password+TOTP), `/2fa/recovery-codes`.
- Login flow: password OK + `TwoFactorEnabled` → return `requiresTwoFactor` + short-lived `2fa_token` (separate claim/audience, 5 min) → client posts 6-digit code or recovery code.
- Enforce on `Admin`/`SubAdmin` roles (config flag in `SettingsCatalog`, e.g. `Auth:Enforce2FA`).
- Frontend: 6-digit input component on login, setup section in profile with QR code.
- **Test:** TOTP generation/validation pure class + unit tests (mirror `PasswordResetToken` pattern).

### 1.5 Config-driven CORS + open-redirect fix + claims normalization — fixes G12, G15, G16 ✅ Done (2026-08-29)
- CORS origins from config (`Cors:AllowedOrigins` array; add a `Setting` descriptor for runtime edit).
- Validate `returnUrl`: must be a relative local URL (`Uri.IsWellFormedUriString` + starts with `/` and not `//`).
- `JwtBearerOptions.MapInboundClaims = false` (or map to short claim names) so `ClaimTypes.NameIdentifier` lookups are predictable; introduce a `CurrentUser` service/extension to kill the repeated `int.Parse(...!)` pattern (30+ call sites) in the same pass.
- **Definition of Done Phase 1:** all auth endpoints rate-limited globally; security headers verified; refresh tokens hashed; new signups can't checkout unverified; admin login requires 2FA; zero secrets in tracked files (`git grep` audit); `dotnet build` + all tests green.

---

## Phase 2 — Payments & Checkout Money-Movement  (est. 2–3 weeks)

> Goal: real charges, real refunds, legally sane order totals. Stripe recommended (best .NET SDK docs); design stays gateway-agnostic behind an interface.

### 2.1 Payment architecture (gateway-agnostic) ✅ Done
- `Services/IPaymentGateway.cs` (Strategy — same shape as `INotificationChannel`):
  ```csharp
  Task<PaymentIntentResult> CreatePaymentAsync(CreatePaymentRequest req, CancellationToken ct);
  Task<RefundResult> RefundAsync(string gatewayPaymentId, decimal amount, string reason, CancellationToken ct);
  Task<WebhookEventResult> ParseWebhookAsync(string body, string signatureHeader);
  ```
- `StripeGateway` implementation (`Stripe.net` package) + `StripeSettings` in gitignored `appsettings.Production.json` (secret key, webhook secret). ✅ Done
- New `Payment` entity: `Id, OrderId, Gateway, GatewayPaymentId, Amount, Currency, Status (Pending/Succeeded/Failed/Refunded/PartiallyRefunded), IdempotencyKey, RawEventJson`. Migration `AddPayments`. ✅ Done
- Order lifecycle: `Pending → AwaitingPayment → Paid → Processing → ...`. Order creation no longer flips to "Placed/Paid" until webhook confirms. ✅ Done
- **Idempotency (mandatory):** `Idempotency-Key` header on payment create; unique index on `Payment.IdempotencyKey`. Checkout retries must never double-charge. ✅ Done (idempotency key per order + `ProcessedWebhookEvent` dedup)

### 2.2 Webhooks (source of truth) ✅ Done
- `POST /api/payments/webhook` — anonymous, **signature-verified** (`StripeEventUtility.ConstructEvent`), raw body, no auth middleware interference. ✅ Done
- Handle: `payment_intent.succeeded` → mark Order `Paid`, enqueue confirmation email; `charge.refunded` → sync refund status; `payment_intent.payment_failed` → mark failed + notify. ✅ Done
- Webhook processing must be idempotent (check event id against processed set — reuse `Payment.RawEventJson` or a `ProcessedWebhookEvent` table). ✅ Done
- All order-state transitions from webhooks go through the existing background-queue style (respond 200 fast, process safely). ✅ Done (processor + `ProcessedWebhookEvent`)

### 2.3 Checkout flow (frontend) ✅ Done
- Checkout step 3: payment via Stripe Payment Element (test cards in dev). ✅ Done (dynamic Stripe.js loader; Mock gateway for dev)
- Guard: order-detail/orders pages reflect `AwaitingPayment` with "Complete payment" retry CTA. ✅ Done
- Never trust client totals: recompute cart → subtotal, shipping, tax, discount server-side at PaymentIntent creation. ✅ Done

### 2.4 Shipping zones & rates — fixes G10a ✅ Done
- Entities: `ShippingZone` (name, regions), `ShippingRate` (zone, flat/per-item/weight-tier, free-over threshold). Admin CRUD in existing admin module + `ShippingController`. ✅ Done
- Checkout calls `POST /api/shipping/quote` (address → zone → rate). Store `ShippingCost` snapshot on Order. ✅ Done

### 2.5 Tax (GST) — fixes G10b ✅ Done
- `TaxRate` table (percent per zone/class), computed server-side in the same totals pipeline as 2.4, stored as `TaxAmount` + `TaxBreakdownJson` snapshot on Order (audit-proof). ✅ Done
- Show breakdown on checkout summary + invoice PDF (`InvoiceService` gains shipping/tax lines). ✅ Done

### 2.6 Real refunds — fixes G11 ✅ Done (backend)
- Approving a return in admin → optional "issue refund" action → `IPaymentGateway.RefundAsync` with the original `GatewayPaymentId` → payment status updated, refund email dispatched (existing channel + template). ✅ Done (`POST /api/payments/refund` admin endpoint + `PaymentWebhookProcessor` refund path)
- **Definition of Done Phase 2:** test-mode charge → webhook → order Paid end-to-end; duplicate webhook safe; refund reflects in Stripe dashboard; totals snapshot (subtotal/shipping/tax/discount) on every order; no client-supplied amount ever persisted.

---

## Phase 3 — DevOps, Testing & Refactor  (est. 1.5–2 weeks)

> Goal: reproducible builds, automated quality gates, and a maintainable admin UI.

### 3.1 Docker — fixes G4
- `EcomApi/Dockerfile`: multi-stage (`mcr.microsoft.com/dotnet/sdk:10.0` → `aspnet:10.0`), non-root user, `dotnet publish -c Release`.
- `EcomApp/Dockerfile`: node build stage → nginx serve stage (SPA fallback config).
- `docker-compose.yml`: api + web + `mssql` (volume for data) + env-driven secrets (`.env` gitignored, provide `.env.example`).
- `.dockerignore`: `bin`, `obj`, `node_modules`, `dist`, `wwwroot/uploads`, logs.
- Move seed/migrate out of startup: `Database.Migrate()` behind `Migration:RunOnStartup` flag (default false in prod); seeding only when explicit `Seed:Enabled`.

### 3.2 CI/CD (GitHub Actions) — fixes G5
- `.github/workflows/ci.yml`, triggered on PR + main:
  1. backend job: `dotnet restore/build/test` on the solution (tests exist — gate on them)
  2. frontend job: `npm ci`, `ng build` (production budgets enforced), `ng test` (once 3.3 lands)
  3. docker build both images (no push until deploy target chosen)
- Later: `deploy.yml` on tag push (host TBD — Azure App Service / VPS / Fly.io).
- Add dependabot.yml (npm + nuget + docker).

### 3.3 Frontend tests — fixes G6
- Vitest (already configured in `package.json`/`tsconfig.spec.json`):
  - Services with logic: `cart.service`, `auth.service` (token refresh, 2FA branch), guards (5 guards, incl. guest/admin negative paths), `coupon.service` validation rules.
  - One component test per critical page: checkout totals, login (error paths), reset-password.
- Target: every new feature ships with tests (match backend rule).

### 3.4 E2E happy paths (Playwright)
- 3 specs: `auth.spec` (register → verify → login), `shop.spec` (browse → cart → coupon → checkout → mock-paid order), `admin.spec` (login+2FA → product CRUD).
- Run against docker-compose stack in CI (stretch goal; local-first is fine).

### 3.5 Admin god-component split — fixes G14
- Break `admin.component.ts` (~1,233 lines) into lazy feature components under `components/admin/`: `admin-products`, `admin-orders`, `admin-users`, `admin-categories`, `admin-banners`, `admin-coupons`, `admin-returns`, `admin-analytics`, `admin-settings` (already has its own service/model — extract its UI too).
- Shared `admin-table` + `admin-modal` components for the repeated list/edit patterns.
- Routes stay under `/admin` with `adminGuard`; child lazy routes (`loadComponent`) — bundle budget (800 KB warn) then becomes realistic.

### 3.6 Small quality passes
- 404 page component (replace silent `**` redirect, G17).
- ESLint + `npm run lint` in CI; Prettier `format` script; `.editorconfig` for backend.
- **Definition of Done Phase 3:** `docker compose up` gives a working store; CI green is the merge gate; admin bundle split with lazy routes; every spec/guard/service has tests.

---

## Phase 4 — Growth & Engagement Features  (est. 2–3 weeks, independent items)

> Goal: revenue and retention wins. All reuse the notification pipeline + analytics + coupon systems already in place. Items are independent — ship in any order.

### 4.1 Order tracking timeline UI (front-end only — data already exists) ✅ Done (pre-existing)
- Backend: `GET /api/orders/{id}` already returns status history — expose `timestamp + status + note` list if not already.
- Frontend: `order-timeline` component on `order-detail`: Placed → Confirmed → Shipped → Out for delivery → Delivered with dates, current step highlighted; cancelled shows terminal state.
- Effort: ~1 day. Highest polish-per-effort item on this list.

### 4.2 Back-in-stock "Notify Me" ✅ Done (2026-08-30)
- Entity `StockAlert` (UserId, ProductId/VariantId, CreatedAt, NotifiedAt) + `POST /api/products/{id}/stock-alerts` (auth, unique per user-product).
- Trigger: on admin product/variant stock update (existing admin save path) → query alerts → dispatch email via `EmailTemplates` + mark `NotifiedAt`. Runs in the existing background queue.
- Frontend: replace disabled "Add to Cart" with "Notify Me" on PDP when stock = 0.
- Security: rate-limit subscribe; unsubscribe link in email (token, same pattern as password reset).

### 4.3 Abandoned-cart recovery ✅ Done (2026-08-31)
- Settings (admin-editable in `SettingsCatalog`):
  - `Cart:AbandonmentHours` (default 24), `Cart:ResendDays` (default 7), `Cart:ScanIntervalMinutes` (default 60),
    `Cart:MinCartTotal` (default 100), `Cart:RecoveryCouponEnabled` (default false), `Cart:RecoveryCouponAmount` (default ₹100).
- `User`: `AbandonedCartOptOut` (bool, default false), `LastAbandonedCartNotifiedAt` (DateTime?) + index.
- `AbandonedCartBackgroundService` (`BackgroundService`, mirrors `NotificationBackgroundService`) — runs in its own DI scope on a configurable `PeriodicTimer`; default scan interval 60 min.
- `AbandonedCartScanRunner` (scoped) — finds carts untouched past threshold with items, enforces per-user cooldown + opt-out + min cart total, optionally creates a single-use `COMEBACK-<userId>-<cartId>` coupon, and enqueues an email via the existing `INotificationQueue`. Sets `LastAbandonedCartNotifiedAt` on success.
- `NotificationType.AbandonedCart` + `EmailTemplates.AbandonedCart(...)` (branded wrapper, optional coupon block, unsubscribe footer).
- Endpoints: `PUT /api/auth/profile/abandoned-cart-opt-out` (auth) and `GET /api/auth/abandoned-cart/unsubscribe?token=...` (anonymous, daily-rotating SHA-256 token, mirrors BackInStock pattern).
- EF migration `AddAbandonedCartRecovery` adds the two User columns + index.
- 14 xUnit tests in `EcomApi.Tests/AbandonedCartRecoveryTests.cs` cover: enqueue, recent-cart skip, opt-out, lockout, cooldown, min-total, coupon on/off, coupon-failure-resilience, timestamp update, empty case, email-template structure.

### 4.4 Invoice PDF attached to order confirmation email ✅ Done (2026-09-02)
- `InvoiceService` already generates PDFs — in the order-confirmation dispatch, generate + attach (`MimeEntity` attachment in `EmailChannel`). ✅ Regenerated on demand in `EmailChannel` when the message type is `OrderConfirmation` (loads order via `IOrderRepository`, generates via `IInvoiceService`, attaches as `invoice-{orderId}.pdf`). Attachment plumbing added to `IEmailService.SendAsync`/`EmailService` (`BodyBuilder.Attachments`).
- Store generated invoices under `wwwroot/invoices/{orderId}.pdf` (gitignored) or regenerate on demand — prefer regenerate (no PII at rest). ✅ Regenerate on demand chosen (no PII/bytes persisted in the queue); generation failure degrades to body-only email so confirmations are never lost.

### 4.5 Coupon performance report (admin analytics)
- Backend: `GET /api/analytics/coupons?from&to` — per coupon: redemptions, discounted total, revenue attributable (orders with coupon vs. not), unique customers. `[Authorize(Roles = "Admin")]` (matches revenue-only rule you already apply).
- Frontend: table + bar chart in existing analytics tab; export CSV (already a roadmap ask in `suggestion.md`).

### 4.6 Admin daily digest
- Scheduled `BackgroundService` (23:00 local, configurable): yesterday's orders count/revenue, new users, pending returns, low-stock list → single email to `Admin:DigestRecipient` setting.
- Reuses everything: settings catalog, email channel, analytics queries.

---

## Security & Standards Checklist (apply to every phase)

**Secrets & config**
- [ ] No secrets in tracked files — dev: `dotnet user-secrets`; prod: env vars / gitignored `appsettings.Production.json`
- [ ] Any new admin-editable value goes through `SettingsCatalog` + `ISettingsProvider` (never hard-code, never new config file)
- [ ] GET settings never returns real secrets (keep `********` mask behavior)

**Auth & authz**
- [ ] Every new endpoint: explicit `[Authorize]` with least-privilege role — no anonymous by default
- [ ] All money/state-changing actions re-validate server-side; never trust client totals/ids/amounts
- [ ] Webhooks: signature verification + idempotency + fast 200 response
- [ ] Tokens (reset, verify, unsubscribe, recovery): hashed at rest, single-use, short expiry, no enumeration responses

**Input & output safety**
- [ ] DTO validation via DataAnnotations on every new DTO (existing convention)
- [ ] No PII or JWT payload in logs (no `console.log` of auth data; server logs use `ILogger` without tokens)
- [ ] File uploads: keep extension+size checks; add MIME magic-byte check when uploads expand

**Process**
- [ ] Each phase ships as small commits (match the per-phase commit discipline from the email feature)
- [ ] Feature = backend + frontend + tests + docs update in the same change
- [ ] `dotnet test` + `ng build` green before every commit; CI gate once Phase 3 lands
- [ ] New entities → EF migration with proper indices + decimal precision (match existing fluent config)

**Definition of Done (any feature)**
1. Backend endpoint(s) with validation + authz + tests
2. Frontend wired through `environment.apiUrl`, loading + error states handled
3. Security checklist above passes
4. Relevant `.md` doc updated (this file's phase checkboxes)

---

## Suggested Execution Order

```
Phase 1  Security hardening        ← do first, blocks real users
Phase 2  Payments + shipping/tax   ← do second, blocks real revenue
Phase 3  DevOps + tests + refactor ← parallelize 3.1/3.2 anytime
Phase 4  Growth features           ← any order; 4.1 ships in a day
```

| Phase | Effort | Unblocks |
|-------|--------|----------|
| 1 | 1.5–2 wks | Trust: verified emails, 2FA admin, hardened tokens |
| 2 | 2–3 wks | Real money: charges, refunds, correct totals |
| 3 | 1.5–2 wks | Shipping: Docker, CI, safe refactors |
| 4 | 2–3 wks | Growth: recovery, alerts, reporting |

---

## Progress Tracker

- [x] Phase 0 — (pre-existing) notification system, password reset, settings, environment files, HSTS
- [x] Phase 1 — Security hardening & trust (2026-08-29: 1.1 security headers, 1.2 hashed refresh tokens, 1.3 email verification + checkout gate, 1.4 TOTP 2FA, 1.5 config CORS + open-redirect fix + claims normalization)
- [x] Phase 2 — Payments & checkout money-movement (2026-08-29: 2.1 gateway-agnostic architecture, 2.2 webhooks + idempotent processor, 2.3 checkout/order-detail payment flow, 2.4 shipping zones/rates, 2.5 GST tax, 2.6 refunds)
- [x] Phase 3 — DevOps, testing & refactor (2026-08-30: 3.1 Docker/Docker Compose, 3.2 GitHub Actions CI/CD, 3.3 frontend unit tests + E2E, 3.4 admin god-component split, 3.5 404 page, ESLint/Prettier)
- [x] Phase 4 — Growth & engagement features (4.1 order tracking timeline UI ✅, 4.2 back-in-stock Notify Me ✅, 4.3 abandoned-cart recovery ✅, 4.4 invoice PDF on order confirmation ✅)

*Created: 2026-08-28. Update checkboxes as phases complete; add findings to the Gap Register rather than new documents.*
