using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.External;

public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleOAuthService> _logger;

    private string ClientId => _configuration["Google:ClientId"]!;
    private string ClientSecret => _configuration["Google:ClientSecret"]!;
    private string RedirectUri => _configuration["Google:RedirectUri"]!;

    public GoogleOAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleOAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetAuthorizationUrl(string userId, string state)
    {
        var scopes = Uri.EscapeDataString(
            "openid email profile " +
            "https://www.googleapis.com/auth/gmail.readonly " +
            "https://www.googleapis.com/auth/calendar.readonly");

        return "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&response_type=code" +
            $"&scope={scopes}" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&state={state}";
    }

    public async Task<GoogleTokensDto> ExchangeCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Trocando código de autorização por tokens Google");

        var parameters = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["redirect_uri"] = RedirectUri,
            ["grant_type"] = "authorization_code",
        };

        return await PostTokenRequestAsync(parameters, ct);
    }

    public async Task<GoogleTokensDto> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Renovando access token Google");

        var parameters = new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["grant_type"] = "refresh_token",
        };

        return await PostTokenRequestAsync(parameters, ct);
    }

    public async Task RevokeTokenAsync(
        string accessToken,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Revogando token Google");

        var url = $"https://oauth2.googleapis.com/revoke?token={accessToken}";
        await _httpClient.PostAsync(url, null, ct);
    }

    private async Task<GoogleTokensDto> PostTokenRequestAsync(
        Dictionary<string, string> parameters,
        CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token", content, ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        var accessToken = data.GetProperty("access_token").GetString()!;
        var expiresIn = data.GetProperty("expires_in").GetInt32();

        var refreshToken = data.TryGetProperty("refresh_token", out var rt)
            ? rt.GetString()!
            : parameters.GetValueOrDefault("refresh_token", "");

        return new GoogleTokensDto(
            accessToken,
            refreshToken!,
            DateTime.UtcNow.AddSeconds(expiresIn));
    }
}