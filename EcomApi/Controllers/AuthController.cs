using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using EcomApi.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthController> _logger;
    private readonly IEmailService _emailService;
    private readonly ISettingsProvider _settings;
    private readonly INotificationService _notificationService;
    private readonly TwoFactorService _twoFactorService;

    public AuthController(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthController> logger,
        IEmailService emailService,
        ISettingsProvider settings,
        INotificationService notificationService,
        TwoFactorService twoFactorService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _emailService = emailService;
        _settings = settings;
        _notificationService = notificationService;
        _twoFactorService = twoFactorService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponseDto>> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.EmailExistsAsync(dto.Email))
            return BadRequest(new { error = "Email is already registered." });

        if (await _userRepository.UsernameExistsAsync(dto.Username))
            return BadRequest(new { error = "Username is already taken." });

        var user = new User
        {
            Email = dto.Email,
            Username = dto.Username,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Role = "Customer",
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        user = await _userRepository.CreateAsync(user);
        _logger.LogInformation("User registered: {Email}", user.Email);

        await _notificationService.SendWelcomeAsync(user);
        await GenerateAndSendVerificationEmailAsync(user, cancellationToken);

        return await GenerateTokenResponse(user);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if (user == null || user.EmailVerificationTokenExpiry == null ||
            !PasswordResetToken.IsValid(dto.Token, user.EmailVerificationTokenHash, user.EmailVerificationTokenExpiry))
        {
            return BadRequest(new { error = "Invalid or expired verification link." });
        }

        user.EmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiry = null;
        await _userRepository.UpdateAsync(user);

        return Ok(new { message = "Email verified successfully. You can now place orders." });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if (user != null && !user.EmailVerified)
        {
            await GenerateAndSendVerificationEmailAsync(user, cancellationToken);
        }

        // Generic response regardless of whether the account exists — no enumeration.
        return Ok(new { message = "If an unverified account exists for this email, a verification link has been sent." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailOrUsernameAsync(dto.EmailOrUsername);
        if (user == null)
        {
            return Unauthorized(new { error = "Invalid credentials." });
        }

        if (user.IsLockedOut)
        {
            var remaining = (int)Math.Ceiling((user.LockoutEnd!.Value - DateTime.UtcNow).TotalSeconds);
            _logger.LogWarning("Locked-out login attempt: {Email}, {RemainingSeconds}s remaining", user.Email, remaining);
            return StatusCode(StatusCodes.Status423Locked, new
            {
                error = "Account is temporarily locked due to too many failed login attempts.",
                lockoutEnd = user.LockoutEnd,
                remainingSeconds = remaining,
                failedAttempts = user.FailedLoginAttempts
            });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts += 1;

            if (user.FailedLoginAttempts >= LockoutPolicy.MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutPolicy.LockoutMinutes);
                user.LockoutReason = $"Locked after {LockoutPolicy.MaxFailedAttempts} failed login attempts.";
                _logger.LogWarning("User {Email} locked out for {Minutes}min after {Count} failed attempts",
                    user.Email, LockoutPolicy.LockoutMinutes, user.FailedLoginAttempts);

                await _userRepository.UpdateAsync(user);

                return StatusCode(StatusCodes.Status423Locked, new
                {
                    error = $"Account locked due to {LockoutPolicy.MaxFailedAttempts} failed login attempts. Try again in {LockoutPolicy.LockoutMinutes} minutes.",
                    lockoutEnd = user.LockoutEnd,
                    remainingSeconds = LockoutPolicy.LockoutMinutes * 60,
                    failedAttempts = user.FailedLoginAttempts
                });
            }

            await _userRepository.UpdateAsync(user);

            var remaining = LockoutPolicy.MaxFailedAttempts - user.FailedLoginAttempts;
            return Unauthorized(new
            {
                error = $"Invalid credentials. {remaining} attempt{(remaining == 1 ? "" : "s")} remaining before lockout.",
                failedAttempts = user.FailedLoginAttempts
            });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { error = "Account is deactivated. Contact an administrator." });
        }

        var enforce2fa = bool.TryParse(await _settings.GetRawAsync("Auth:Enforce2FA", cancellationToken), out var enforce) && enforce;
        var isPrivilegedRole = user.Role == UserRoles.Admin || user.Role == UserRoles.SubAdmin;
        if (user.TwoFactorEnabled || (enforce2fa && isPrivilegedRole))
        {
            if (!user.TwoFactorEnabled)
                return StatusCode(StatusCodes.Status423Locked, new
                {
                    error = "Two-factor authentication is required for your account. Please enable it in your profile first, or contact an administrator.",
                    code = "TWO_FACTOR_REQUIRED"
                });

            var twoFactorToken = _tokenService.GenerateTwoFactorToken(user);
            return Ok(new { requiresTwoFactor = true, twoFactorToken });
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LockoutReason = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("User logged in: {Email}", user.Email);

        return await GenerateTokenResponse(user);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(dto.Token);
        if (user == null)
            return Unauthorized(new { error = "Invalid refresh token." });

        var refreshToken = user.RefreshTokens.First(rt => rt.TokenHash == PasswordResetToken.HashToken(dto.Token));
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = "rotation";

        return await GenerateTokenResponse(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound();

        var activeTokens = user.RefreshTokens.Where(rt => rt.IsActive).ToList();
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("User logged out: {Email}", user.Email);
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound();

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            return BadRequest(new { error = "Current password is incorrect." });

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LockoutReason = null;
        await _userRepository.UpdateAsync(user);

        var activeTokens = user.RefreshTokens.Where(rt => rt.IsActive).ToList();
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User changed password: {Email}", user.Email);
        return Ok(new { message = "Password changed successfully. Please login again." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(user.Adapt<UserDto>());
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _userRepository.GetAllAsync(pageNumber, pageSize, search, role);

        return Ok(new
        {
            items = items.Adapt<List<UserDto>>(),
            totalCount,
            pageNumber,
            pageSize
        });
    }

    [HttpPut("users/{id}/deactivate")]
    [Authorize(Roles = "Admin,SubAdmin")]
    public async Task<IActionResult> DeactivateUser(int id, CancellationToken cancellationToken = default)
    {
        var result = await _userRepository.DeactivateAsync(id);
        if (!result)
            return NotFound();

        return Ok(new { message = "User deactivated." });
    }

    [HttpPut("users/{id}/activate")]
    [Authorize(Roles = "Admin,SubAdmin")]
    public async Task<IActionResult> ActivateUser(int id, CancellationToken cancellationToken = default)
    {
        var result = await _userRepository.ActivateAsync(id);
        if (!result)
            return NotFound();

        return Ok(new { message = "User activated." });
    }

    [HttpPost("users/{id}/unlock")]
    [Authorize(Roles = "Admin,SubAdmin")]
    public async Task<IActionResult> UnlockUser(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { error = "User not found." });

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LockoutReason = null;
        await _userRepository.UpdateAsync(user);

        var currentUserEmail = User.FindFirstValue(ClaimTypes.Email)!;
        _logger.LogInformation("User {Email} unlocked by {AdminEmail}", user.Email, currentUserEmail);
        return Ok(new { message = "User account unlocked." });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepository.UpdateProfileAsync(userId, dto.FirstName, dto.LastName, dto.Phone, dto.Gender, dto.DateOfBirth);
        _logger.LogInformation("User updated profile: {Email}", user.Email);
        return Ok(user.Adapt<UserDto>());
    }

    [HttpPost("users")]
    [Authorize(Roles = "Admin,SubAdmin")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var currentUserEmail = User.FindFirstValue(ClaimTypes.Email)!;
        var currentUserRole = User.FindFirstValue(ClaimTypes.Role)!;

        if (!UserRoles.IsValidRole(dto.Role))
        {
            return BadRequest(new { error = "Invalid role specified." });
        }

        if (!await _userRepository.CanCreateUsersAsync(currentUserRole, dto.Role))
        {
            return Forbid();
        }

        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            return BadRequest(new { error = "Email is already registered." });
        }

        if (await _userRepository.UsernameExistsAsync(dto.Username))
        {
            return BadRequest(new { error = "Username is already taken." });
        }

        var user = new User
        {
            Email = dto.Email,
            Username = dto.Username,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Role = dto.Role,
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        user = await _userRepository.CreateUserWithRoleAsync(user, dto.Role, currentUserEmail);
        _logger.LogInformation("User created by {AdminEmail}: {Email}", currentUserEmail, user.Email);

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user.Adapt<UserDto>());
    }

    [HttpPost("users/{id}/change-password")]
    [Authorize(Roles = "Admin,SubAdmin")]
    public async Task<IActionResult> ChangeUserPassword(int id, [FromBody] AdminChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var currentUserEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.Email == currentUserEmail)
        {
            return BadRequest(new { error = "Use /change-password endpoint to change your own password." });
        }

        var passwordHash = _passwordHasher.HashPassword(user, dto.NewPassword);

        var success = await _userRepository.ChangePasswordAsync(id, passwordHash);
        if (!success)
        {
            return NotFound();
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LockoutReason = null;
        await _userRepository.UpdateAsync(user);

        var activeTokens = user.RefreshTokens.Where(rt => rt.IsActive).ToList();
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Password changed for user {Email} by {AdminEmail}", user.Email, currentUserEmail);
        return Ok(new { message = "Password changed successfully." });
    }

    [HttpGet("users/roles")]
    [Authorize(Roles = "Admin")]
    public ActionResult GetRoles()
    {
        return Ok(new { roles = UserRoles.AllRoles });
    }

    [HttpPost("profile/picture")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UploadProfilePicture(IFormFile file, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Invalid file type. Allowed: jpg, jpeg, png, webp." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "File size exceeds 5MB limit." });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var imageUrl = $"/uploads/profiles/{fileName}";
        var user = await _userRepository.UpdateProfilePictureAsync(userId, imageUrl);
        _logger.LogInformation("User uploaded profile picture: {Email}", user.Email);
        return Ok(user.Adapt<UserDto>());
    }

    [HttpDelete("profile/picture")]
    [Authorize]
    public async Task<ActionResult<UserDto>> RemoveProfilePicture(CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepository.UpdateProfilePictureAsync(userId, null);
        _logger.LogInformation("User removed profile picture: {Email}", user.Email);
        return Ok(user.Adapt<UserDto>());
    }

    [HttpGet("addresses")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AddressDto>>> GetAddresses(CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var addresses = await _userRepository.GetAddressesAsync(userId);
        return Ok(addresses.Adapt<List<AddressDto>>());
    }

    [HttpPost("addresses")]
    [Authorize]
    public async Task<ActionResult<AddressDto>> AddAddress([FromBody] CreateAddressDto dto, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var address = dto.Adapt<Address>();
        address.UserId = userId;
        var created = await _userRepository.AddAddressAsync(address);
        return CreatedAtAction(nameof(GetAddresses), new { id = created.Id }, created.Adapt<AddressDto>());
    }

    [HttpPut("addresses/{id}")]
    [Authorize]
    public async Task<ActionResult<AddressDto>> UpdateAddress(int id, [FromBody] UpdateAddressDto dto, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var address = await _userRepository.GetAddressByIdAsync(id, userId);
        if (address == null)
            return NotFound();

        if (dto.Label != null) address.Label = dto.Label;
        if (dto.Street != null) address.Street = dto.Street;
        if (dto.City != null) address.City = dto.City;
        if (dto.State != null) address.State = dto.State;
        if (dto.ZipCode != null) address.ZipCode = dto.ZipCode;
        if (dto.Country != null) address.Country = dto.Country;
        if (dto.IsDefault.HasValue) address.IsDefault = dto.IsDefault.Value;

        var updated = await _userRepository.UpdateAddressAsync(address);
        return Ok(updated.Adapt<AddressDto>());
    }

    [HttpDelete("addresses/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteAddress(int id, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userRepository.DeleteAddressAsync(id, userId);
        if (!result)
            return NotFound();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if (user != null)
        {
            var token = PasswordResetToken.GenerateToken();
            var hash = PasswordResetToken.HashToken(token);

            user.PasswordResetTokenHash = hash;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            await _userRepository.UpdateAsync(user);

            var clientBase = await _settings.GetRawAsync("Client:BaseUrl", cancellationToken) ?? "http://localhost:4200";
            var link = $"{clientBase.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
            var name = user.FirstName ?? user.Username;
            await _emailService.SendAsync(user.Email, "Reset your password - Ecom", EmailTemplates.PasswordReset(name, link));
        }

        // Identical response regardless of whether the account exists (no enumeration)
        return Ok(new { message = "If the account exists, a password reset link has been sent to your email." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if (user == null || !PasswordResetToken.IsValid(dto.Token, user.PasswordResetTokenHash, user.PasswordResetTokenExpiry))
        {
            return BadRequest(new { error = "Invalid or expired token." });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiry = null;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LockoutReason = null;

        foreach (var rt in user.RefreshTokens.Where(r => r.IsActive))
            rt.RevokedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return Ok(new { message = "Password reset successful. Please log in." });
    }

    [HttpPost("2fa/validate")]
    public async Task<ActionResult<TokenResponseDto>> ValidateTwoFactor([FromBody] ValidateTwoFactorDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _tokenService.GetUserIdFromTwoFactorToken(dto.TwoFactorToken);
        if (userId == null)
            return Unauthorized(new { error = "Invalid or expired two-factor session." });

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null || !user.IsActive)
            return Unauthorized(new { error = "Invalid or expired two-factor session." });

        var isTotp = _twoFactorService.VerifyCode(user.TwoFactorSecretEncrypted, dto.Code);
        var isRecovery = _twoFactorService.TryConsumeRecoveryCode(user, dto.Code);
        if (!isTotp && !isRecovery)
            return BadRequest(new { error = "Invalid verification code." });

        await _userRepository.UpdateAsync(user);
        return await GenerateTokenResponse(user);
    }

    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<ActionResult<SetupTwoFactorResponseDto>> SetupTwoFactor(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        if (user == null) return Unauthorized();

        var (secret, uri) = _twoFactorService.GenerateSetup(user.Email);
        user.TwoFactorSecretEncrypted = _twoFactorService.ProtectSecret(secret);
        await _userRepository.UpdateAsync(user);

        return Ok(new SetupTwoFactorResponseDto { Secret = secret, OtpAuthUri = uri });
    }

    [HttpPost("2fa/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorDto dto, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        if (user == null) return Unauthorized();

        if (!_twoFactorService.VerifyCode(user.TwoFactorSecretEncrypted, dto.Code))
            return BadRequest(new { error = "Invalid verification code." });

        user.TwoFactorEnabled = true;
        var codes = _twoFactorService.GenerateRecoveryCodes();
        await _userRepository.ClearRecoveryCodesAsync(user.Id, cancellationToken);
        foreach (var (_, hash) in codes)
            user.RecoveryCodes.Add(new RecoveryCode { CodeHash = hash });

        await _userRepository.UpdateAsync(user);
        return Ok(new { recoveryCodes = codes.Select(c => c.Plain).ToList() });
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorDto dto, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        if (user == null) return Unauthorized();

        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) != PasswordVerificationResult.Success)
            return BadRequest(new { error = "Incorrect password." });

        var isTotp = _twoFactorService.VerifyCode(user.TwoFactorSecretEncrypted, dto.Code);
        var isRecovery = _twoFactorService.TryConsumeRecoveryCode(user, dto.Code);
        if (!isTotp && !isRecovery)
            return BadRequest(new { error = "Invalid verification code." });

        user.TwoFactorEnabled = false;
        user.TwoFactorSecretEncrypted = null;
        await _userRepository.ClearRecoveryCodesAsync(user.Id, cancellationToken);

        await _userRepository.UpdateAsync(user);
        return Ok(new { message = "Two-factor authentication disabled." });
    }

    [HttpPost("2fa/recovery-codes")]
    [Authorize]
    public async Task<IActionResult> RegenerateRecoveryCodes(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        if (user == null) return Unauthorized();
        if (!user.TwoFactorEnabled)
            return BadRequest(new { error = "Two-factor authentication is not enabled." });

        var codes = _twoFactorService.GenerateRecoveryCodes();
        await _userRepository.ClearRecoveryCodesAsync(user.Id, cancellationToken);
        foreach (var (_, hash) in codes)
            user.RecoveryCodes.Add(new RecoveryCode { CodeHash = hash });

        await _userRepository.UpdateAsync(user);
        return Ok(new { recoveryCodes = codes.Select(c => c.Plain).ToList() });
    }

    private async Task<User?> GetCurrentUserEntityAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        return await _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    private async Task<ActionResult<TokenResponseDto>> GenerateTokenResponse(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress, userAgent);

        user.RefreshTokens.Add(refreshToken);
        await _userRepository.UpdateAsync(user);

        return Ok(new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = _tokenService.GetAccessTokenExpiry(),
            TokenType = "Bearer",
            EmailVerified = user.EmailVerified
        });
    }

    private async Task GenerateAndSendVerificationEmailAsync(User user, CancellationToken cancellationToken)
    {
        var token = PasswordResetToken.GenerateToken();
        user.EmailVerificationTokenHash = PasswordResetToken.HashToken(token);
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _userRepository.UpdateAsync(user);

        var baseUrl = await _settings.GetRawAsync("Client:BaseUrl", cancellationToken) ?? "http://localhost:4200";
        var verifyUrl = $"{baseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
        await _notificationService.SendVerificationEmailAsync(user, verifyUrl);
    }
}
