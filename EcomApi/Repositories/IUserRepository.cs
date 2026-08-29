using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User> CreateUserWithRoleAsync(User user, string role, string createdBy, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? search = null, string? role = null, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ActivateAsync(int id, CancellationToken cancellationToken = default);
    Task<User> UpdateProfileAsync(int id, string? firstName, string? lastName, string? phone, string? gender, DateTime? dateOfBirth, CancellationToken cancellationToken = default);
    Task<User> UpdateProfilePictureAsync(int userId, string? imageUrl, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(int id, string newPasswordHash, CancellationToken cancellationToken = default);
    Task<bool> CanCreateUsersAsync(string creatorRole, string targetRole);
    Task<Address> AddAddressAsync(Address address, CancellationToken cancellationToken = default);
    Task<Address?> GetAddressByIdAsync(int addressId, int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Address>> GetAddressesAsync(int userId, CancellationToken cancellationToken = default);
    Task<Address> UpdateAddressAsync(Address address, CancellationToken cancellationToken = default);
    Task<bool> DeleteAddressAsync(int addressId, int userId, CancellationToken cancellationToken = default);
    Task UnsetDefaultAddressesAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> ClearRecoveryCodesAsync(int userId, CancellationToken cancellationToken = default);
}
