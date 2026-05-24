# User Management System

## User Roles and Permissions

### Customer
- **Can register themselves** via `/api/auth/register`
- Can change their own password via `/api/auth/change-password`
- Can update their profile via `/api/auth/profile`
- Can view their own information via `/api/auth/me`

### SubAdmin
- Can create customers and other SubAdmins via `/api/auth/users`
- Can deactivate/activate users via `/api/auth/users/{id}/deactivate` and `/api/auth/users/{id}/activate`
- Can change any user's password via `/api/auth/users/{id}/change-password`
- Can view all users via `/api/auth/users`
- Cannot create Admin users (only Admin can do that)

### Admin
- Can create users of any role (Admin, SubAdmin, Customer) via `/api/auth/users`
- Can deactivate/activate any user via `/api/auth/users/{id}/deactivate` and `/api/auth/users/{id}/activate`
- Can change any user's password via `/api/auth/users/{id}/change-password`
- Can view all users via `/api/auth/users`
- Can view available roles via `/api/auth/users/roles`

## User Registration Flow

1. **Customer Registration** (Public)
   ```
   POST /api/auth/register
   {
     "email": "customer@example.com",
     "username": "customer123",
     "password": "Password123",
     "confirmPassword": "Password123",
     "firstName": "John",
     "lastName": "Doe",
     "phone": "+1234567890"
   }
   ```
   - Automatically assigns "Customer" role
   - No authentication required

2. **Admin/SubAdmin Creation** (Admin/SubAdmin only)
   ```
   POST /api/auth/users
   {
     "email": "subadmin@example.com",
     "username": "subadmin123",
     "password": "Password123",
     "confirmPassword": "Password123",
     "role": "SubAdmin", // or "Customer"
     "firstName": "Jane",
     "lastName": "Smith",
     "phone": "+1234567890"
   }
   ```
   - Requires authentication with Admin or SubAdmin role
   - Can specify role for new user

## User Management Actions

### Changing Passwords
1. **User changes their own password**
   ```
   POST /api/auth/change-password
   {
     "currentPassword": "OldPassword123",
     "newPassword": "NewPassword123",
     "confirmNewPassword": "NewPassword123"
   }
   ```
   - Available to all authenticated users
   - Invalidates all refresh tokens

2. **Admin/SubAdmin changes another user's password**
   ```
   POST /api/auth/users/{userId}/change-password
   {
     "newPassword": "NewPassword123",
     "confirmPassword": "NewPassword123"
   }
   ```
   - Requires Admin or SubAdmin role
   - Invalidates all refresh tokens for that user

### Deactivating/Activating Users
```
PUT /api/auth/users/{userId}/deactivate
PUT /api/auth/users/{userId}/activate
```
- Requires Admin or SubAdmin role
- Deactivated users cannot login or perform any actions
- Activation restores user access

## Default Admin Account
The system includes a default admin account for initial setup:
- Email: `admin@ecom.com`
- Password: `Admin@123`
- Role: `Admin`

## Security Notes
- All password changes automatically invalidate refresh tokens for security
- User creation tracks who created each user via the `CreatedBy` field
- Only Admin users can create other Admin users
- SubAdmin users cannot create other SubAdmin users (only Customers)