using MediatR;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using RefreshTokenEntity = Orizon.Domain.Entities.RefreshToken;

namespace Orizon.Application.UseCases.Auth.Commands.RegisterUser;

public class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RegisterUserCommandHandler(
        IIdentityService identityService,
        IJwtService jwtService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponseDto> Handle(
        RegisterUserCommand request,
        CancellationToken ct)
    {
        // Criar usuário via IIdentityService
        var (success, userId, errors) = await _identityService.CreateUserAsync(
            request.Email,
            request.DisplayName,
            request.Password,
            ct);

        if (!success)
            throw new InvalidOperationException(string.Join(", ", errors));

        // Buscar o AppUser criado
        var appUser = await _identityService.GetUserByIdAsync(userId, ct)
            ?? throw new InvalidOperationException("Erro ao recuperar usuário criado.");

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