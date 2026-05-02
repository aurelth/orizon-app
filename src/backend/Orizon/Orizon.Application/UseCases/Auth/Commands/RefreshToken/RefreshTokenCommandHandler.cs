using MediatR;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using RefreshTokenEntity = Orizon.Domain.Entities.RefreshToken;

namespace Orizon.Application.UseCases.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RefreshTokenCommandHandler(
        IIdentityService identityService,
        IJwtService jwtService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponseDto> Handle(
        RefreshTokenCommand request,
        CancellationToken ct)
    {
        // Buscar e validar o RefreshToken
        var storedToken = await _refreshTokenRepository
            .GetByTokenAsync(request.Token, ct);

        if (storedToken is null || !storedToken.IsActive)
            throw new UnauthorizedAccessException(
                "RefreshToken inválido ou expirado.");

        // Revogar o token atual
        await _refreshTokenRepository
            .RevokeAllUserTokensAsync(storedToken.UserId, ct);

        // Buscar AppUser
        var appUser = await _identityService
            .GetUserByIdAsync(storedToken.UserId, ct)
            ?? throw new UnauthorizedAccessException("Usuário não encontrado.");

        // Gerar novo JWT
        var accessToken = _jwtService.GenerateToken(appUser);

        // Gerar novo RefreshToken
        var newRefreshToken = new RefreshTokenEntity { UserId = storedToken.UserId };
        await _refreshTokenRepository.AddAsync(newRefreshToken, ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UserId = storedToken.UserId,
            Email = appUser.Email,
            DisplayName = appUser.DisplayName,
            ThemePreference = appUser.ThemePreference.ToString()
        };
    }
}