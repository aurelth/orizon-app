using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Orizon.Application.Interfaces.Services;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Mappers;
using System.Text;

namespace Orizon.Infrastructure.Services.Auth;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly UserMapper _mapper;

    public IdentityService(UserManager<AppIdentityUser> userManager)
    {
        _userManager = userManager;
        _mapper = new UserMapper();
    }

    public async Task<(bool Success, string UserId, string[] Errors)> CreateUserAsync(
        string email,
        string displayName,
        string password,
        CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return (false, string.Empty, new[] { $"Email '{email}' já está em uso." });

        var identityUser = new AppIdentityUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName
        };

        var result = await _userManager.CreateAsync(identityUser, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return (false, string.Empty, errors);
        }

        return (true, identityUser.Id, Array.Empty<string>());
    }

    public async Task<(bool Success, string UserId)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        var identityUser = await _userManager.FindByEmailAsync(email);
        if (identityUser is null)
            return (false, string.Empty);

        var isValid = await _userManager.CheckPasswordAsync(identityUser, password);
        if (!isValid)
            return (false, string.Empty);

        return (true, identityUser.Id);
    }

    public async Task<AppUser?> GetUserByIdAsync(
        string userId,
        CancellationToken ct = default)
    {
        var identityUser = await _userManager.FindByIdAsync(userId);
        if (identityUser is null) return null;
        return _mapper.ToAppUser(identityUser);
    }

    public async Task<AppUser?> GetUserByEmailAsync(
        string email,
        CancellationToken ct = default)
    {
        var identityUser = await _userManager.FindByEmailAsync(email);
        if (identityUser is null) return null;
        return _mapper.ToAppUser(identityUser);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is not null;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(
        string userId,
        CancellationToken ct = default)
    {
        var identityUser = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);

        // encode para URL-safe
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    public async Task<bool> ResetPasswordAsync(
    string userId,
    string token,
    string newPassword,
    CancellationToken ct = default)
    {
        var identityUser = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        try
        {
            var decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ResetPasswordAsync(
                identityUser, decodedToken, newPassword);

            return result.Succeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task<bool> CheckPasswordAsync(
    string userId,
    string password,
    CancellationToken ct = default)
    {
        var identityUser = await _userManager.FindByIdAsync(userId);
        if (identityUser is null) return false;
        return await _userManager.CheckPasswordAsync(identityUser, password);
    }
}