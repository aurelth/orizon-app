using Microsoft.AspNetCore.Identity;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Identity;

namespace Orizon.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(
        UserManager<AppIdentityUser> userManager,
        IJwtService jwtService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken ct = default)
    {
        // Verificar se email já existe
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new InvalidOperationException(
                $"Email '{request.Email}' já está em uso.");

        // Criar novo usuário
        var identityUser = new AppIdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(identityUser, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Erro ao criar usuário: {errors}");
        }

        return await GenerateAuthResponseAsync(identityUser, ct);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken ct = default)
    {
        // Buscar usuário pelo email
        var identityUser = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Email ou senha inválidos.");

        // Verificar senha
        var isPasswordValid = await _userManager
            .CheckPasswordAsync(identityUser, request.Password);

        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Email ou senha inválidos.");

        return await GenerateAuthResponseAsync(identityUser, ct);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        // Buscar e validar o RefreshToken
        var storedToken = await _refreshTokenRepository
            .GetByTokenAsync(refreshToken, ct);

        if (storedToken is null || !storedToken.IsActive)
            throw new UnauthorizedAccessException("RefreshToken inválido ou expirado.");

        // Buscar usuário
        var identityUser = await _userManager.FindByIdAsync(storedToken.UserId)
            ?? throw new UnauthorizedAccessException("Usuário não encontrado.");

        // Revogar o token atual
        storedToken.Revoke();
        await _refreshTokenRepository.RevokeAllUserTokensAsync(storedToken.UserId, ct);

        return await GenerateAuthResponseAsync(identityUser, ct);
    }

    public async Task LogoutAsync(
        string userId,
        CancellationToken ct = default)
    {
        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, ct);
    }
    
    private async Task<AuthResponseDto> GenerateAuthResponseAsync(
        AppIdentityUser identityUser,
        CancellationToken ct = default)
    {        
        var appUser = new Domain.Entities.AppUser
        {
            Id = Guid.Parse(identityUser.Id),
            Email = identityUser.Email ?? string.Empty,
            DisplayName = identityUser.DisplayName,
            ThemePreference = identityUser.ThemePreference,
            TrelloEnabled = identityUser.TrelloEnabled
        };
      
        var accessToken = _jwtService.GenerateToken(appUser);
        
        var refreshToken = new RefreshToken
        {
            UserId = identityUser.Id
        };
        await _refreshTokenRepository.AddAsync(refreshToken, ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UserId = identityUser.Id,
            Email = identityUser.Email ?? string.Empty,
            DisplayName = identityUser.DisplayName,
            ThemePreference = identityUser.ThemePreference.ToString()
        };
    }
}