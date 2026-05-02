using Orizon.Domain.Entities;

namespace Orizon.Application.Interfaces.Services;

public interface IIdentityService
{    
    Task<(bool Success, string UserId, string[] Errors)> CreateUserAsync(
        string email,
        string displayName,
        string password,
        CancellationToken ct = default);

    Task<(bool Success, string UserId)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken ct = default);

    Task<AppUser?> GetUserByIdAsync(
        string userId,
        CancellationToken ct = default);

    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken ct = default);
}