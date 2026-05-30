using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername);
    Task<User?> GetByRefreshTokenAsync(string token);
    Task<User> CreateAsync(User user);
    Task<User> CreateUserWithRoleAsync(User user, string role, string createdBy);
    Task UpdateAsync(User user);
    Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? search = null, string? role = null);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> DeactivateAsync(int id);
    Task<bool> ActivateAsync(int id);
    Task<User> UpdateProfileAsync(int id, string? firstName, string? lastName, string? phone, string? gender, DateTime? dateOfBirth);
    Task<User> UpdateProfilePictureAsync(int userId, string? imageUrl);
    Task<bool> ChangePasswordAsync(int id, string newPasswordHash);
    Task<bool> CanCreateUsersAsync(string creatorRole, string targetRole);
    Task<Address> AddAddressAsync(Address address);
    Task<Address?> GetAddressByIdAsync(int addressId, int userId);
    Task<IEnumerable<Address>> GetAddressesAsync(int userId);
    Task<Address> UpdateAddressAsync(Address address);
    Task<bool> DeleteAddressAsync(int addressId, int userId);
    Task UnsetDefaultAddressesAsync(int userId);
}
