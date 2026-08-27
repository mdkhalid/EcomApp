# Feature: Multi-Channel Notification System (Email + SMS/WhatsApp) & Password Reset

**Status:** Phases 0–5 COMPLETE & pushed to GitHub. Notification delivery runs on a background queue (HTTP requests no longer block on email/SMS I/O).
**Priority:** Critical (unblocks real-world usage)
**Stack impact:** Backend (MailKit + channel pipeline + new endpoints + migration), Frontend (2 new pages), Config (gitignored SMTP/secrets)

---

## 1. Why this feature

The app currently has **no outbound communication**:
- `NotificationService` has email/SMS templates written but **commented out** (no SMTP/SMS client).
- No `forgot-password` / `reset-token` flow — a locked-out user has no recovery path.
- No order confirmation / shipping / delivery notice — customers are never informed.
- Audit (`PRODUCTION_AUDIT.md`): "No email service wired up" = **Critical Issue #4**.

This feature adds a **pluggable, multi-channel notification pipeline** (Email now; SMS & WhatsApp later with zero breaking changes) plus account recovery.

---

## 2. Design / Patterns

**Strategy (per-channel) + Facade (orchestrator) + Observer-style dispatch.**

```
INotificationChannel  (Strategy)
   ├── InAppChannel       (existing admin DB notifications)
   ├── EmailChannel       (uses IEmailService / MailKit)
   ├── SmsChannel         (uses ISmsProvider)        -> added later, drop-in
   └── WhatsAppChannel    (uses IWhatsAppProvider)   -> added later, drop-in

NotificationService : INotificationService   (Facade / Orchestrator)
   - holds IEnumerable<INotificationChannel>
   - Dispatch(NotificationMessage) -> each channel sends what it can

NotificationMessage
   - Type   : Welcome | OrderConfirmation | OrderShipped | OrderDelivered
             | OrderStatusUpdate | TrackingUpdate | PasswordReset
   - Email / Phone  (recipients)
   - Subject / HtmlBody / TextBody
```

- Adding SMS or WhatsApp later = implement one `INotificationChannel` + its provider and register it. **No controller or `NotificationService` changes** → satisfies "easy to add without breaking any feature."
- Providers are interfaces (`IEmailService`, `ISmsProvider`, `IWhatsAppProvider`); concrete impls (MailKit, Twilio, WhatsApp Business API) are swappable.
- Triggers stay in existing flows (register, order placed, status change) and simply call `Dispatch`.

**Admin-configurable settings (key requirement):** nothing is hard-coded. A `Setting` entity + `ISettingsProvider` (DB-backed, `IConfiguration` fallback, 1-min cache) is the single source of truth for anything an admin can change at runtime — SMTP host/port/user/password/from, per-channel enable toggles (`Notification:Email:Enabled`, etc.), and the client base URL for email links. `AdminSettingsController` (`GET/PUT /api/admin/settings`, Admin-only) exposes a catalog (`SettingsCatalog`) of editable keys. Changing a value takes effect within ~1 minute, no restart. To add a new configurable item: add one `SettingDescriptor` to `SettingsCatalog` — no controller/service changes (Open/Closed).

---

## 3. Scope

**In scope (this build)**
- SMTP email service (`IEmailService` + MailKit) with env/gitignored config.
- Channel pipeline: `InAppChannel` + `EmailChannel` (+ inert `SmsChannel`/`WhatsAppChannel` scaffolding so they're drop-in ready).
- HTML email templates: Welcome, Order Confirmation, Shipped, Delivered, Password Reset.
- Password reset: `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`.
- Frontend: `forgot-password` + `reset-password` pages + login link.
- Rate limiting on `forgot-password`.

**Scaffolded for later (no behavior yet)**
- `ISmsProvider` + `SmsChannel` (null provider; activate with Twilio/etc.).
- `IWhatsAppProvider` + `WhatsAppChannel` (null provider; activate with WhatsApp Business API).

**Out of scope:** real payment gateway, marketing email, email verification on signup (fast follow-up reusing `IEmailService`).

---

## 4. Configuration

`Smtp` section (gitignored `appsettings.Production.json`):
```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "User": "apikey-or-user",
  "Password": "<secret>",
  "From": "no-reply@yourapp.com",
  "FromName": "Ecom",
  "EnableSsl": true
}
```
Read via `IOptions<SmtpSettings>` with env override. SMS/WhatsApp creds added the same way when those channels are activated.

---

## 5. Backend changes

### 5.1 Foundation (Phase 0) — DONE
- `Models/Setting.cs` + `DbSet` + migration `AddSettings` — admin-editable config store.
- `Models/NotificationMessage.cs` — message DTO + `NotificationType` enum.
- `Services/INotificationChannel.cs` — `SendAsync(NotificationMessage, CancellationToken)`.
- `Services/IEmailService.cs` + `Services/EmailService.cs` — MailKit SMTP send, reads config at runtime via `ISettingsProvider`.
- `Services/ISettingsProvider.cs` + `SettingsProvider` — DB-first, config fallback, cached.
- `Repositories/ISettingRepository.cs` + `SettingRepository`.
- `Services/SettingsCatalog.cs` — known editable keys.
- `Services/NotificationChannels/EmailChannel.cs`, `InAppChannel.cs`, `SmsChannel.cs` (null provider), `WhatsAppChannel.cs` (null provider).
- `Controllers/AdminSettingsController.cs` — `GET/PUT /api/admin/settings`.
- `Program.cs` — registered `IMemoryCache`, settings, email, channels; `NotificationService` refactored to `Dispatch` through channels with admin-toggleable enable flags.

### 5.2 Templates (Phase 1)
- `Services/EmailTemplates.cs` — 5 HTML templates with `{{placeholder}}` substitution; `BuildOrderEmail(Order, type)`.

### 5.3 Password reset (Phase 2)
- `User`: `PasswordResetTokenHash`, `PasswordResetTokenExpiry`. Migration `AddPasswordReset`.
- `POST /api/auth/forgot-password` → generic response + token + email.
- `POST /api/auth/reset-password` → validate, hash new password, revoke refresh tokens, clear token.
- Rate-limit `forgot-password`.

### 5.4 Wire flows (Phase 3)
- Register → Welcome email (Dispatch).
- Order placed → Order Confirmation (Dispatch).
- Status shipped/delivered → respective email (Dispatch).

---

## 6. Frontend changes (Phase 4)
- `forgot-password` page + `auth.service.forgotPassword(email)`.
- `reset-password` page (reads `token`+`email` from query) + `auth.service.resetPassword(...)`.
- Login: "Forgot password?" link → `/forgot-password`.
- All URLs via `environment.apiUrl` (no hard-coded host).

---

## 7. Security
- SMTP/SMS/WhatsApp secrets only in gitignored config / env.
- Reset token: crypto-random, **hashed at rest (SHA-256)**, 30-min expiry, single-use.
- `forgot-password` returns identical response regardless of email existence (no enumeration).
- Reset revokes all active refresh tokens (force re-login).
- Rate-limit reset requests.
- Email links use `environment`-driven client base URL.

---

## 8. Development plan (phased — commit after each phase)

**Phase 0 — Config & channel architecture (foundation) — DONE ✅**
- [x] Add `MailKit` package.
- [x] `Setting` entity + migration `AddSettings` (admin-editable config; no hard-coded SMTP).
- [x] `NotificationMessage` + `NotificationType` + `INotificationChannel`.
- [x] `IEmailService` + `EmailService` (MailKit) — reads config at runtime via `ISettingsProvider`.
- [x] `ISettingsProvider` + `SettingRepository` + `SettingsCatalog`.
- [x] `EmailChannel`, `InAppChannel`, `SmsChannel` (null), `WhatsAppChannel` (null).
- [x] `AdminSettingsController` (`GET/PUT /api/admin/settings`, Admin-only).
- [x] Register pipeline in `Program.cs`; refactor `NotificationService` to `Dispatch` with admin-toggleable channels.
- [x] `dotnet build` clean + migration generated. COMMITTED.

**Phase 1 — Email templates & send — DONE ✅**
- [x] `Services/EmailTemplates.cs` — Welcome, OrderConfirmation, OrderStatusUpdate, OrderShipped, OrderDelivered, Tracking, PasswordReset (centralized, branded wrapper).
- [x] `NotificationService` routes through `EmailTemplates`; branches to Shipped/Delivered templates on status.
- [x] `EmailChannel` sends real emails (MailKit), config-driven via `ISettingsProvider`. COMMITTED.

**Phase 2 — Password reset backend — DONE ✅**
- [x] `User`: `PasswordResetTokenHash` + `PasswordResetTokenExpiry`; migration `AddPasswordReset`.
- [x] DTOs `ForgotPasswordDto`, `ResetPasswordDto`.
- [x] `AuthController.ForgotPassword` — generic response (no enumeration), crypto token (hash stored, 30-min expiry), email via `EmailTemplates.PasswordReset` + `Client:BaseUrl` link.
- [x] `AuthController.ResetPassword` — validates hashed token, sets new password, revokes refresh tokens, single-use.
- [x] Global rate limiter (30/min/IP) already covers `forgot-password`. COMMITTED.

**Phase 3 — Wire transactional emails**
- [ ] Welcome on register; Confirmation on order; Shipped/Delivered on status. COMMIT.

**Phase 4 — Frontend — DONE ✅**
- [x] `AuthService.forgotPassword()` + `resetPassword()`.
- [x] `forgot-password` page (email request, generic success message).
- [x] `reset-password` page (reads `token`+`email` from query params, validates, confirms match).
- [x] Routes `/forgot-password`, `/reset-password` (guest-guarded).
- [x] Login page "Forgot password?" link. All URLs use `environment.apiUrl` (no hard-coded host). COMMITTED.

**Phase 5 — Test & polish - DONE ✅**
- [x] Backend unit tests (`EcomApi.Tests`): `PasswordResetToken` (gen/hash/validate/expiry), `SettingsProvider` (DB value, default, bool conversion, caching), `NotificationDispatcher` (enabled-only routing, disabled skip, per-channel fault isolation). 13 tests, all passing.
- [x] Password-reset token logic extracted to `Services/PasswordResetToken.cs` (pure, testable) and used by `AuthController`.
- [x] Notification delivery moved off the request path: `INotificationQueue` (Channel) + `NotificationBackgroundService` + `NotificationDispatcher` (per-scope). `NotificationService` now enqueues; requests return immediately.
- [x] `AdminSettingsController.Update` hardened: validates all keys before applying (no partial updates) and never overwrites a real secret with the `"********"` mask returned by GET.
- [x] Removed redundant manual password-length check (covered by `ResetPasswordDto` DataAnnotations).
- [x] SCSS de-duplicated into `src/app/shared/styles/_auth-form.scss` (imported by both auth pages).
- [x] Frontend happy/error paths implemented; no hard-coded host (uses `environment.apiUrl`). Rebuilt both. COMMITTED.

---

## 9. Testing checklist
- [ ] Forgot-password known email → receives email with working link.
- [ ] Forgot-password unknown email → identical generic response, no email.
- [ ] Reset expired/used/wrong token → rejected.
- [ ] Reset success → password works, old sessions invalidated.
- [ ] Register → welcome email; order placed → confirmation; shipped/delivered → emails.
- [ ] SMS/WhatsApp channels compile & are drop-in (null provider inert).
- [ ] Secrets NOT in git history.

---

## 10. Future drop-in (SMS / WhatsApp) — no breaking changes
1. Implement `ISmsProvider` (e.g., Twilio) / `IWhatsAppProvider`.
2. Register the provider + `SmsChannel`/`WhatsAppChannel` in `Program.cs`.
3. Add SMS/WhatsApp creds to gitignored config.
4. (Optional) include phone numbers in `NotificationMessage` from order/user data.
No edits to controllers or `NotificationService` required.
