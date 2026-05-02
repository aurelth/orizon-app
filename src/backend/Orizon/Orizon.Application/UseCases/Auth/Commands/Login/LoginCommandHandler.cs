using MediatR;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using RefreshTokenEntity = Orizon.Domain.Entities.RefreshToken;

namespace Orizon.Application.UseCases.Auth.Commands.Login;

public class LoginCommandHandler
    : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtService jwtService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponseDto> Handle(
        LoginCommand request,
        CancellationToken ct)
    {
        // Validar credenciais
        var (success, userId) = await _identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            ct);

        if (!success)
            throw new UnauthorizedAccessException("Email ou senha inválidos.");

        // Buscar AppUser
        var appUser = await _identityService.GetUserByIdAsync(userId, ct)
            ?? throw new UnauthorizedAccessException("Usuário não encontrado.");

        // Gerar JWT
        var accessToken = _jwtService.GenerateToken(appUser);

        // Gerar e salvar RefreshToken
        var refreshToken = new RefreshTokenEntity { UserId = userId };
        await _refreshTokenRepository.AddAsync(refreshToken, ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UserId = userId,
            Email = appUser.Email,
            DisplayName = appUser.DisplayName,
            ThemePreference = appUser.ThemePreference.ToString()
        };
    }
}