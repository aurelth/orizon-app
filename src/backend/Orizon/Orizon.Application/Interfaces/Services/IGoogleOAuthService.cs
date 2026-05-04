namespace Orizon.Application.Interfaces.Services;

public interface IGoogleOAuthService
{
    string GetAuthorizationUrl(string userId, string state);
    Task<GoogleTokensDto> ExchangeCodeAsync(string code, CancellationToken ct = default);
    Task<GoogleTokensDto> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeTokenAsync(string accessToken, CancellationToken ct = default);
}

public record GoogleTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);