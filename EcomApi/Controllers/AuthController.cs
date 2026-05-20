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

    public AuthController(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponseDto>> Register([FromBody] RegisterDto dto)
    {
        if (await _userRepository.EmailExistsAsync(dto.Email))
            return BadRequest(new { error = "Email is already registered." });

        if (await _userRepository.UsernameExistsAsync(dto.Username))
            return BadRequest(new { error = "Username is already taken." });

        var user = new User
        {
            Email = dto.Email,
            Username = dto.Username,
            PasswordHash = dto.Password,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Role = "Customer",
            IsActive = true
        };

        user = await _userRepository.CreateAsync(user);
        _logger.LogInformation("User registered: {Email}", user.Email);

        return await GenerateTokenResponse(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _userRepository.GetByEmailOrUsernameAsync(dto.EmailOrUsername);
        if (user == null)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Invalid credentials." });

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("User logged in: {Email}", user.Email);

        return await GenerateTokenResponse(user);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshTokenDto dto)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(dto.Token);
        if (user == null)
            return Unauthorized(new { error = "Invalid refresh token." });

        var refreshToken = user.RefreshTokens.First(rt => rt.Token == dto.Token);
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = "rotation";

        return await GenerateTokenResponse(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
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
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound();

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            return BadRequest(new { error = "Current password is incorrect." });

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
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
    public async Task<ActionResult<UserDto>> GetCurrentUser()
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
        [FromQuery] string? role = null)
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var result = await _userRepository.DeactivateAsync(id);
        if (!result)
            return NotFound();

        return Ok(new { message = "User deactivated." });
    }

    [HttpPut("users/{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateUser(int id)
    {
        var result = await _userRepository.ActivateAsync(id);
        if (!result)
            return NotFound();

        return Ok(new { message = "User activated." });
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
            TokenType = "Bearer"
        });
    }
}
