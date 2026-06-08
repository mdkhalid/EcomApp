# Ecom Project — Production-Readiness Audit

**Project:** Ecom (EcomApi + EcomApp)
**Audit Date:** 2026-06-06
**Stack:** .NET 10 Web API (backend) + Angular 21 (frontend) + SQL Server (EF Core)

---

## Verdict

**Not production-ready (~50-55%).** Feature-rich demo with clean architecture, but blocked on security config, deployment automation, observability, testing, and payment integration.

---

## 1. Project Structure

| Item | Status | Notes |
|------|--------|-------|
| Monorepo with EcomApi + EcomApp | OK | Clean top-level split |
| Backend folder organization | OK | Repository pattern correctly used |
| Frontend folder organization | OK | Good Angular structure |
| Stray folders at repo root (`.commandcode`, `.deepseek`, `.openclaude`) | PROBLEM | AI-tooling metadata mixed into the repo |
| `login_details.jpg` at repo root | PROBLEM | Likely a screenshot of credentials — must be removed |
| `suggestion.md` (feature roadmap) checked in | WARN | OK to keep, but is committed history noise |
| Unrelated `Images/` folder at root with raw product photos (some with wrong `.jpt` extensions) | WARN | Same assets duplicated in `EcomApi/wwwroot/uploads/` — sync nightmare |
| `wwwroot/uploads/` tracked in git (with profile images) | PROBLEM | Should be ignored; risks leaking user PII |
| `backend.log`, `backend.err.log`, `frontend.log`, `frontend.err.log` at repo root | WARN | Should be git-ignored (currently are) but exist on disk |

---

## 2. Backend (EcomApi)

### 2.1 Language / Framework — OK
- .NET 10 (`net10.0`) Web API, C# 12 with `Nullable enable` and `ImplicitUsings`.

### 2.2 API Design — WARN
- REST controllers (14 of them) with proper `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`.
- No API versioning beyond `v1` in the swagger doc; URI scheme may need to change for v2.
- No HATEOAS, no consistent RFC-7807 problem-details responses; each controller hand-builds `{ error = "..." }` objects.
- No OpenAPI XML comments wired up; Swagger schema is bare.

### 2.3 Authentication & Authorization — WARN
- JWT bearer with HS256, issuer/audience/lifetime/signature all validated. Refresh tokens persisted with rotation. Good baseline.
- Roles: `Admin`, `SubAdmin`, `Customer` enforced with `[Authorize(Roles = ...)]`.
- Account lockout (5 attempts / 15 min) with manual admin unlock — implemented.
- Issues:
  - `Jwt:SecretKey` is hard-coded in `appsettings.json` and committed to git.
  - `ClockSkew = TimeSpan.Zero` is overly strict — minor risk of legitimate 401s near token edge.
  - No email verification, no password complexity enforcement beyond `StringLength(MinimumLength = 8)`.
  - 2FA is mentioned in `suggestion.md` as "next recommended feature" — not implemented.
  - `Math.Ceiling((user.LockoutEnd!.Value - DateTime.UtcNow).TotalSeconds)` uses `!` null-forgive — risky in race conditions.
  - `int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)` pattern repeated 30+ times; no centralized `CurrentUser` helper.

### 2.4 Database / ORM — WARN
- EF Core 10 with SQL Server, 19 migrations, properly versioned.
- `ApplicationDbContext` has rich fluent configuration: indices, max lengths, decimal precision, cascade behaviors.
- Issues:
  - `Program.cs` calls `context.Database.Migrate()` on every startup (line 95). Risky for production rollouts.
  - Auto-seed of products, categories, banners, coupons runs on every startup (DbSeeder). Includes workaround for "broken image URLs".
  - Connection string uses `Trusted_Connection=True` and hard-coded `Server=localhost;Database=EcomDb` — not environment-driven.
  - No soft-delete pattern; `IsActive` flag used inconsistently.
  - No connection-resiliency / retry policy (`EnableRetryOnFailure`).
  - No database-level audit log.

### 2.5 Error Handling Middleware — WARN
- Custom `ExceptionHandlingMiddleware` exists, but:
  - Registered before `UseStaticFiles()` / `UseAuthentication()` — auth failures don't flow through consistently.
  - Only handles `ArgumentException`, `KeyNotFoundException`, `UnauthorizedAccessException`; everything else returns generic 500 with no `traceId`/`correlationId`.
  - No `UseExceptionHandler` (built-in developer exception page in dev).
  - No central ProblemDetails class.

### 2.6 Input Validation — WARN
- DTOs use `System.ComponentModel.DataAnnotations` attributes — good.
- `[ApiController]` triggers automatic 400 with model-state errors — good.
- Missing:
  - No FluentValidation or custom validators.
  - No file-upload antivirus / magic-byte check (only extension allow-list) — fake `.jpg` can be uploaded.
  - `dto.Adapt(product)` (Mapster) silently overwrites server fields in some controllers.

### 2.7 Environment Configuration — CRITICAL
- `appsettings.json` contains:
  - `Jwt:SecretKey` — a real, committed 60-character secret. Must be moved to environment variable / Azure Key Vault / User Secrets.
  - `ConnectionStrings:DefaultConnection` with `Trusted_Connection=True; Server=localhost` — should be env-driven.
- No `.env`, no `appsettings.Production.json` (only `Development.json` exists, and it's empty), no user-secrets scaffolding.
- `AllowedHosts: "*"` — overly permissive in production.

### 2.8 Security — CRITICAL

| Concern | Status |
|---------|--------|
| SQL injection | OK (EF Core parameterizes) |
| CORS | WARN (Hard-coded to `http://localhost:4200`; no env override) |
| Helmet / security headers | MISSING (No `UseHsts()`, no `UseHttpsRedirection()`, no security-headers middleware) |
| Rate limiting | WARN (Custom in-memory, 30 req/min, auth only — does not work in multi-instance) |
| File-upload validation | WARN (Extension + size only; no MIME check, no antivirus) |
| HTTPS | MISSING (No HTTPS redirect; `applicationUrl` is plain HTTP) |
| `MapInboundClaims` | MISSING (JWT claim names get auto-mapped to long Microsoft URI strings) |
| Open redirect | WARN (Login/Register use `returnUrl` query param without validation) |
| Session cookies | WARN (`CartId` cookie has no `Secure` flag and no `__Host-` prefix) |
| Password storage | OK (ASP.NET `PasswordHasher<T>` PBKDF2) |
| Refresh token storage | WARN (Base64 stored in plain text. Should be hashed.) |
| Open Swagger UI in prod | OK (Gated on Development) |

### 2.9 Logging — MISSING
- Uses the built-in `ILogger<T>` (console only) — no Serilog, NLog, Seq, Application Insights.
- No request-logging middleware.
- No correlation IDs / `HttpContext.TraceIdentifier` propagation.

### 2.10 Testing — MISSING
- Zero test projects. No `*Tests*` folder, no `*.spec.cs`, no `xunit.runner.json`.

### 2.11 API Documentation (Swagger/OpenAPI) — WARN
- Swashbuckle 10 is registered. `MapOpenApi()` and `UseSwaggerUI()` exist for dev.
- No XML-doc generation enabled — Swagger has no parameter descriptions.
- No Swagger examples / `Swashbuckle.AspNetCore.Filters`.

### 2.12 Docker Support — MISSING
- No `Dockerfile` in `EcomApi/` or at the root.
- No `docker-compose.yml` / `docker-compose.dev.yml`.
- No `.dockerignore`.
- No multi-stage build.

### 2.13 Other Backend Notes
- `UseSession()` is called for a pure JWT API — vestigial.
- `AddDistributedMemoryCache()` — fine for dev, would leak memory in production.
- `[Authorize(Roles = "Admin,SubAdmin")]` strings duplicated everywhere → role-name typo = silent auth failure.
- `MapOrder()` in `OrdersController` is hand-rolled — inconsistent with Mapster usage elsewhere.

---

## 3. Frontend (EcomApp)

### 3.1 Framework / Language — OK
- Angular 21 with strict mode + standalone components + Signals (172+ uses) — modern.

### 3.2 State Management — OK
- Angular Signals throughout (no NgRx). `AuthService`, `CartService`, `NotificationService`, `AdminNotificationService` all use signals.
- `computed()` and `toSignal()` used appropriately.

### 3.3 Routing — WARN
- 11 routes, 5 guards (`authGuard`, `customerGuard`, `adminGuard`, `guestGuard`, `rootRedirectGuard`) — well-considered.
- Lazy loading NOT configured — every feature ships in the main bundle.
- No 404 page — `**` route just redirects to `/products`.

### 3.4 API Integration — CRITICAL
- Every single service hard-codes `http://localhost:5068` as `apiUrl` (verified across 14+ services). Production blocker.
- No environment file, no `environment.ts` / `environment.prod.ts`, no proxy config.
- `auth.interceptor.ts` correctly registered and adds `Authorization: Bearer` + `withCredentials: true`.
- `with-credentials.interceptor.ts` exists but is never used — dead code.
- No global HTTP error interceptor.
- No request-retry / back-off.

### 3.5 Error Handling — WARN
- Each component subscribes to its own `error:` handler. No central toast/error service for HTTP failures.
- `NotificationService` (signal-based toast) exists and is good — but no `HttpInterceptor` for global error → toast.
- No client-side `globalErrorHandler`.

### 3.6 Loading States — OK
- `loading` / `isLoading` signals in every component.

### 3.7 Responsive Design — WARN
- SCSS, custom CSS variables, media queries used.
- No CSS framework (Tailwind / Material) — heavy custom SCSS to maintain.
- `darkMode` toggle exists.
- No automated responsive tests / cross-browser matrix.

### 3.8 Accessibility — WARN
- Some `aria-label`, `role="listbox"`, `aria-expanded` on the search component.
- Coverage inconsistent — no skip-to-content link, no focus management on modals, no live region for toasts.
- No `aria-live="polite"` on the cart count.

### 3.9 Testing — MISSING
- `tsconfig.spec.json` and `package.json` reference Vitest globals, but no `*.spec.ts` file exists.
- `angular.json` has `skipTests: true` on every schematic.

### 3.10 Bundle / Build — WARN
- Production budgets: 800 KB warning / 1 MB error for initial bundle. Will likely fail once admin lazy-loads.
- No service worker / PWA config.
- `outputHashing: all` OK for cache-busting.

---

## 4. DevOps & Quality

| Item | Status |
|------|--------|
| CI/CD (GitHub Actions / GitLab / Azure Pipelines) | MISSING |
| Dockerfile / docker-compose | MISSING |
| `.dockerignore` | MISSING |
| Linting (ESLint / dotnet format / StyleCop) | MISSING |
| Prettier | PARTIAL (Configured but no `format` script) |
| Husky / lint-staged | MISSING |
| TypeScript strict mode | OK |
| C# nullable + ImplicitUsings | OK |
| `.editorconfig` (frontend) | OK |
| `.editorconfig` (backend) | MISSING |
| Top-level README | MISSING (`EcomApp/README.md` is default Angular CLI) |
| LICENSE | MISSING |
| CONTRIBUTING.md | MISSING |
| CODE_OF_CONDUCT.md | MISSING |
| CHANGELOG.md | MISSING |
| SECURITY.md | MISSING |
| `.gitattributes` | MISSING |
| Semantic Versioning / release tags | MISSING |
| Dependabot / Renovate config | MISSING |
| SBOM / third-party license inventory | PARTIAL (Angular has it, backend does not) |

---

## 5. Code Quality

### 5.1 Console / Debug Logs Left In — WARN
- Three explicit `console.log` calls in `auth.service.ts` (lines 31, 132, 136) print the entire JWT payload (including user's role and email) — PII leak.
- No other `console.log` in app code.
- No `TODO` / `FIXME` / `HACK` / `XXX` comments in the source tree.

### 5.2 Hard-coded Secrets — CRITICAL
- `EcomApi/appsettings.json`:
  - `Jwt:SecretKey` — full key committed.
- `EcomApp/src/app/**/*.ts`:
  - API base URL `http://localhost:5068` in 14+ services.
- `EcomApp/src/index.html`:
  - `https://fonts.googleapis.com/...` — external CDN dependency with no SRI.
- `DbSeeder`:
  - Default admin email `admin@ecom.com` / password `Admin@123` and demo `demo@ecom.com` / `Demo@123` — also documented in `README_UserManagement.md`.

### 5.3 Error Messages — OK (mostly)
- Controllers return descriptive `BadRequest(new { error = "..." })` messages.
- Frontend surfaces them via `err.error?.error || 'fallback'`.
- `ExceptionHandlingMiddleware` masks the real message in 500s with "An unexpected error occurred." — correct but hard to debug.

### 5.4 Code Organization / Patterns — WARN
- Backend: clean Repository → Service → Controller pattern, Mapster for DTO mapping.
- Frontend: `admin.component.ts` is a 1,233-line god-component handling products, orders, users, categories, banners, coupons, returns, analytics — violates SRP.
- Frontend models are duplicated as TS interfaces mirroring C# DTOs — no OpenAPI codegen.
- Inline image-URL builder (`getFullImageUrl(path)` → `return 'http://localhost:5068' + path`) repeated in 7+ components.

### 5.5 Other
- `app.scss` (15,980 B) and `styles.scss` (11,742 B) are large; no design-token / theme abstraction.
- No Storybook / component catalog.
- No e2e tests (Playwright/Cypress).
- The `EcomApi.csproj.lscache` file is Rider/VS-specific and should be ignored.

---

## 6. E-commerce Specific Features

| Feature | Status | Notes |
|---------|--------|-------|
| Product catalog (search, filter, sort) | OK | Full Search/Filter DTO |
| Product detail + variants + multi-image gallery | OK | |
| Product reviews & ratings | OK | One per user, must-have-purchased check |
| Categories & banners | OK | Admin-managed with sort order, date-range |
| Cart (guest + auth merging) | OK | Cookie-based session cart merges on login |
| Wishlist | OK | |
| Coupon / discount system | OK | Percentage and fixed, min cart, usage limits, expiry |
| Returns / refunds | OK | Reason enum, admin approval, 5 statuses |
| Order management | OK | 7 status states, status history, tracking, PDF invoice |
| User profiles & address book | OK | |
| Activity tracking & recommendations | OK | Recently viewed, For you, Trending |
| Global search with autosuggest | OK | Debounced, recent searches, keyboard nav |
| Admin analytics dashboard | OK | Chart.js, revenue, top products, low-stock |
| Admin notifications (in-app) | OK | Polling-based, mark read / read-all |
| Account lockout | OK | 5 attempts / 15 min, HTTP 423, admin unlock |
| Guest checkout | PARTIAL | Possible via session cookie but no flow |
| Real payment gateway (Stripe/Razorpay/PayPal) | MISSING | "Payment" is just a `PaymentMethod` string. No real charge. |
| Email notifications | MISSING | Templates commented out in `NotificationService.cs` |
| SMS notifications | MISSING | Commented out |
| 2FA for admin | MISSING | |
| Inventory management | PARTIAL | Low-stock alerts but no automatic reservation |
| Multi-currency / multi-language | MISSING | |
| PWA / offline mode | MISSING | |
| SEO / SSR / Angular Universal | MISSING | Pure SPA |
| Tax / GST calculation | MISSING | |
| Shipping cost / zone calculation | MISSING | |
| Refund actual money movement | MISSING | Status flow exists, no payment-side integration |

---

## Top 15 Critical Issues (Fix Before Production)

1. **JWT secret key hard-coded in `appsettings.json` and committed to git.** Move to env var / secret manager and rotate immediately.
2. **Default admin credentials (`admin@ecom.com` / `Admin@123`) seeded and documented.** Remove or force-change on first deploy.
3. **No payment gateway integration.** Orders are placed with no real money movement.
4. **No email service wired up.** Order confirmations, password resets, shipping updates are no-ops.
5. **API base URL `http://localhost:5068` hard-coded in 14+ Angular services.** Create `environment.ts` / `environment.prod.ts` and proxy config.
6. **No HTTPS / no HSTS / no security-headers middleware.** Wide open to MITM, clickjacking, XSS downgrade.
7. **No automated tests** — zero backend unit, zero frontend specs, zero e2e.
8. **No Docker / docker-compose** — deployment story is "clone, set up SQL Server, `dotnet run`".
9. **No CI/CD pipeline** — no GitHub Actions, Azure DevOps, GitLab CI.
10. **Rate limiter is in-process and only covers `/api/auth`.** Replace with distributed rate limiter (Redis-backed) and apply globally.
11. **CORS hard-coded to `http://localhost:4200`.** Frontend on a real domain will be blocked.
12. **Automatic `Database.Migrate()` + full seeder on every startup** breaks concurrent deploys and re-inserts default admin in prod.
13. **Admin component is a 1,233-line god-class.** Refactor into feature sub-modules with lazy loading.
14. **Three `console.log` calls in `auth.service.ts` dump the entire JWT payload** — PII leak.
15. **No LICENSE, no top-level README, no SECURITY.md, no CONTRIBUTING.md, no changelog, no editorconfig on backend, no ESLint, no Prettier script, no Husky hooks.**

---

## Honourable Mentions

- File-upload validation checks extension only — executable masquerading as `.jpg` will pass.
- Refresh tokens stored unhashed in the database.
- No background job runner (Hangfire/Quartz) for order workflows, abandoned-cart emails.
- `UseSession()` and `AddDistributedMemoryCache()` are unused overhead in a pure-JWT API.
- No EF Core second-level cache.
- No data-protection keys persisted.

---

## What's Done Well

- Solid use of Repository pattern + Mapster + DTO separation on the backend.
- Modern Angular 21 with Signals, standalone components, strict TypeScript, functional interceptors/guards.
- Comprehensive feature set: 14 controllers, 13 Angular services, 13 pages, 5 guards.
- Sensible entity model (16 entities, 19 migrations) with proper indices, precision, cascade rules.
- Account lockout, refresh-token rotation, role-based authorization, file-upload size limits.
- `suggestion.md` roadmap already acknowledges remaining gaps (payment, email, 2FA) — mature engineering hygiene.

---

## Estimated Effort to Production

| Phase | Effort | Scope |
|-------|--------|-------|
| Phase 1: Security hardening | 1-2 weeks | secret/CORS/HTTPS/rate-limit |
| Phase 2: Payments + Email | 2-3 weeks | real Stripe/Razorpay + transactional email |
| Phase 3: DevOps | 1-2 weeks | Docker, docker-compose, GitHub Actions, basic load test |
| Phase 4: Testing | 1-2 weeks | xUnit + Vitest happy-path tests |
| Ongoing | — | Split admin god-component, add ESLint/Prettier/Husky, write README, pick a license |

**Overall production-readiness score: 50-55%.**
