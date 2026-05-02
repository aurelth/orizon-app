using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Repositories;
using Orizon.Infrastructure.Services;
using Orizon.Infrastructure.Services.Auth;
using Testcontainers.PostgreSql;

namespace Orizon.Tests.Integration.Auth;

public class AuthServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private OrizonDbContext _context = null!;
    private IAuthService _authService = null!;

    public AuthServiceTests()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("orizon_test")
            .WithUsername("orizon")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "TestSecretKey2026SuperSeguroParaJwtOrizon!!" },
                { "Jwt:ExpiryHours", "1" },
                { "Jwt:Issuer", "orizonapp.io" },
                { "Jwt:Audience", "orizonapp.io" }
            })
            .Build();

        var services = new ServiceCollection();

        services.AddDbContext<OrizonDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));

        services.AddIdentityCore<AppIdentityUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<OrizonDbContext>();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthService, AuthService>();

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<OrizonDbContext>();

        await _context.Database.MigrateAsync();

        _authService = provider.GetRequiredService<IAuthService>();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task RegisterAsync_WhenValidRequest_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            DisplayName = "Aurel",
            Email = "aurel@orizonapp.io",
            Password = "Test@12345"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(request.Email);
        result.DisplayName.Should().Be(request.DisplayName);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            DisplayName = "Aurel",
            Email = "duplicate@orizonapp.io",
            Password = "Test@12345"
        };

        await _authService.RegisterAsync(request);

        // Act
        var act = async () => await _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoginAsync_WhenValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange — registrar primeiro
        var registerRequest = new RegisterRequestDto
        {
            DisplayName = "Aurel",
            Email = "login@orizonapp.io",
            Password = "Test@12345"
        };
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequestDto
        {
            Email = "login@orizonapp.io",
            Password = "Test@12345"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(loginRequest.Email);
    }

    [Fact]
    public async Task LoginAsync_WhenInvalidPassword_ShouldThrowException()
    {
        // Arrange
        var registerRequest = new RegisterRequestDto
        {
            DisplayName = "Aurel",
            Email = "wrongpass@orizonapp.io",
            Password = "Test@12345"
        };
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequestDto
        {
            Email = "wrongpass@orizonapp.io",
            Password = "SenhaErrada123"
        };

        // Act
        var act = async () => await _authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenValidToken_ShouldReturnNewTokens()
    {
        // Arrange — registrar e obter tokens
        var registerRequest = new RegisterRequestDto
        {
            DisplayName = "Aurel",
            Email = "refresh@orizonapp.io",
            Password = "Test@12345"
        };
        var authResponse = await _authService.RegisterAsync(registerRequest);

        // Act
        var result = await _authService.RefreshTokenAsync(authResponse.RefreshToken);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        // Novos tokens devem ser diferentes dos anteriores
        result.AccessToken.Should().NotBe(authResponse.AccessToken);
        result.RefreshToken.Should().NotBe(authResponse.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenInvalidToken_ShouldThrowException()
    {
        // Act
        var act = async () => await _authService.RefreshTokenAsync("token-invalido");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LogoutAsync_WhenCalled_ShouldRevokeAllTokens()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            DisplayName = "Aurel",
            Email = "logout@orizonapp.io",
            Password = "Test@12345"
        };
        var authResponse = await _authService.RegisterAsync(request);

        // Act
        await _authService.LogoutAsync(authResponse.UserId);

        // Assert — refresh token deve estar revogado
        var act = async () => await _authService
            .RefreshTokenAsync(authResponse.RefreshToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}