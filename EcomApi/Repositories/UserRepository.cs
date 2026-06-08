using EcomApi.Data;
using EcomApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserRepository(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                (u.Email == emailOrUsername || u.Username == emailOrUsername) && u.IsActive);
    }

    public async Task<User?> GetByRefreshTokenAsync(string token)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u =>
                u.RefreshTokens.Any(rt => rt.Token == token && rt.IsActive));
    }

    public async Task<User> CreateAsync(User user)
    {
        user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(
        int pageNumber, int pageSize, string? search = null, string? role = null)
    {
        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.Email.Contains(search) ||
                u.Username.Contains(search) ||
                (u.FirstName != null && u.FirstName.Contains(search)) ||
                (u.LastName != null && u.LastName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AsNoTracking().AnyAsync(u => u.Email == email);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users.AsNoTracking().AnyAsync(u => u.Username == username);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<User> UpdateProfileAsync(int id, string? firstName, string? lastName, string? phone, string? gender, DateTime? dateOfBirth)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (firstName != null) user.FirstName = firstName;
        if (lastName != null) user.LastName = lastName;
        if (phone != null) user.Phone = phone;
        if (gender != null) user.Gender = gender;
        if (dateOfBirth.HasValue) user.DateOfBirth = dateOfBirth;

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateProfilePictureAsync(int userId, string? imageUrl)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (user.ProfilePictureUrl != null && user.ProfilePictureUrl != imageUrl)
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePictureUrl.TrimStart('/'));
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        }

        user.ProfilePictureUrl = imageUrl;
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> CreateUserWithRoleAsync(User user, string role, string createdBy)
    {
        if (!UserRoles.IsValidRole(role))
        {
            throw new ArgumentException($"Invalid role: {role}");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);
        user.Role = role;
        user.CreatedBy = createdBy;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ChangePasswordAsync(int id, string newPasswordHash)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.PasswordHash = newPasswordHash;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CanCreateUsersAsync(string creatorRole, string targetRole)
    {
        if (creatorRole == UserRoles.Admin) return true;
        if (creatorRole == UserRoles.SubAdmin && targetRole == UserRoles.Customer) return true;
        return false;
    }

    public async Task<Address> AddAddressAsync(Address address)
    {
        if (address.IsDefault)
            await UnsetDefaultAddressesAsync(address.UserId);

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();
        return address;
    }

    public async Task<Address?> GetAddressByIdAsync(int addressId, int userId)
    {
        return await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
    }

    public async Task<IEnumerable<Address>> GetAddressesAsync(int userId)
    {
        return await _context.Addresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Address> UpdateAddressAsync(Address address)
    {
        if (address.IsDefault)
            await UnsetDefaultAddressesAsync(address.UserId);

        _context.Addresses.Update(address);
        await _context.SaveChangesAsync();
        return address;
    }

    public async Task<bool> DeleteAddressAsync(int addressId, int userId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
        if (address == null) return false;

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UnsetDefaultAddressesAsync(int userId)
    {
        var defaults = await _context.Addresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync();
        foreach (var addr in defaults)
            addr.IsDefault = false;
    }
}
