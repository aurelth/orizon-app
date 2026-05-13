using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Auth;

[Collection("Integration Tests")]
public class AuthControllerForgotResetPasswordTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public AuthControllerForgotResetPasswordTests()
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

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType ==
                            typeof(DbContextOptions<OrizonDbContext>));

                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<OrizonDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });

                builder.UseSetting("Jwt:Secret",
                    "TestSecretKey2026SuperSeguroParaJwtOrizon!!");
                builder.UseSetting("Jwt:ExpiryHours", "1");
                builder.UseSetting("Jwt:Issuer", "orizonapp.io");
                builder.UseSetting("Jwt:Audience", "orizonapp.io");
                builder.UseSetting("ConnectionStrings:PostgreSQL",
                    _postgres.GetConnectionString());
                builder.UseSetting("ConnectionStrings:Redis", "");
                builder.UseSetting("Weather:BaseUrl",
                    "https://api.open-meteo.com/v1");
                builder.UseSetting("Google:ClientId", "test-client-id");
                builder.UseSetting("Google:ClientSecret", "test-client-secret");
                builder.UseSetting("Google:RedirectUri",
                    "http://localhost:5010/google/callback");
                builder.UseSetting("Anthropic:ApiKey", "test-api-key");
                builder.UseSetting("Trello:ApiKey", "test-api-key");
                builder.UseSetting("Trello:Token", "test-token");
                builder.UseSetting("App:FrontendUrl", "http://localhost:4200");
                builder.UseSetting("Worker:InternalUrl", "http://localhost:5011");
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<OrizonDbContext>();

        var maxRetries = 5;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();
                break;
            }
            catch (Exception) when (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task RegisterUserAsync(
        string email = "aurel@orizonapp.io",
        string password = "Test@12345")
    {
        var command = new RegisterUserCommand("Aurel", email, password);
        await _client.PostAsJsonAsync("/auth/register", command);
    }

    private async Task<string> GetResetTokenAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<OrizonDbContext>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppIdentityUser>>();

        var user = await userManager.FindByIdAsync(userId);
        var token = await userManager.GeneratePasswordResetTokenAsync(user!);

        return Microsoft.AspNetCore.WebUtilities.WebEncoders
            .Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
    }

    private async Task<string> GetUserIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<OrizonDbContext>();

        var user = await context.Users.FirstAsync(u => u.Email == email);
        return user.Id;
    }

    // --- ForgotPassword ---

    [Fact]
    public async Task ForgotPassword_WhenValidEmail_ShouldReturn200()
    {
        await RegisterUserAsync("forgot@orizonapp.io");

        var response = await _client.PostAsJsonAsync(
            "/auth/forgot-password",
            new { email = "forgot@orizonapp.io" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailNotFound_ShouldReturn200()
    {
        // não revela se email existe ou não
        var response = await _client.PostAsJsonAsync(
            "/auth/forgot-password",
            new { email = "notfound@orizonapp.io" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WhenInvalidEmail_ShouldReturn200()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/forgot-password",
            new { email = "email-invalido" });

        // sempre retorna 200 para não revelar informações
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmptyEmail_ShouldReturn200()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/forgot-password",
            new { email = "" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- ResetPassword ---

    [Fact]
    public async Task ResetPassword_WhenValidToken_ShouldReturn200()
    {
        await RegisterUserAsync("reset@orizonapp.io");
        var userId = await GetUserIdByEmailAsync("reset@orizonapp.io");
        var token = await GetResetTokenAsync(userId);

        var response = await _client.PostAsJsonAsync(
            "/auth/reset-password",
            new
            {
                email = "reset@orizonapp.io",
                token,
                newPassword = "NewPass@12345"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WhenSamePassword_ShouldReturn400()
    {
        await RegisterUserAsync("samepass@orizonapp.io", "Test@12345");
        var userId = await GetUserIdByEmailAsync("samepass@orizonapp.io");
        var token = await GetResetTokenAsync(userId);

        var response = await _client.PostAsJsonAsync(
            "/auth/reset-password",
            new
            {
                email = "samepass@orizonapp.io",
                token,
                newPassword = "Test@12345"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content
            .ReadFromJsonAsync<Dictionary<string, string>>();
        result!["message"].Should().Be(
            "A nova senha não pode ser igual à senha atual.");
    }

    [Fact]
    public async Task ResetPassword_WhenInvalidToken_ShouldReturn400()
    {
        await RegisterUserAsync("invalidtoken@orizonapp.io");

        var response = await _client.PostAsJsonAsync(
            "/auth/reset-password",
            new
            {
                email = "invalidtoken@orizonapp.io",
                token = "token-invalido",
                newPassword = "NewPass@12345"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WhenUserNotFound_ShouldReturn400()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/reset-password",
            new
            {
                email = "notfound@orizonapp.io",
                token = "qualquer-token",
                newPassword = "NewPass@12345"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WhenWeakPassword_ShouldReturn400()
    {
        await RegisterUserAsync("weakpass@orizonapp.io");
        var userId = await GetUserIdByEmailAsync("weakpass@orizonapp.io");
        var token = await GetResetTokenAsync(userId);

        var response = await _client.PostAsJsonAsync(
            "/auth/reset-password",
            new
            {
                email = "weakpass@orizonapp.io",
                token,
                newPassword = "fraca"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_AfterReset_ShouldLoginWithNewPassword()
    {
        await RegisterUserAsync("newlogin@orizonapp.io", "OldPass@12345");
        var userId = await GetUserIdByEmailAsync("newlogin@orizonapp.io");
        var token = await GetResetTokenAsync(userId);

        await _client.PostAsJsonAsync("/auth/reset-password", new
        {
            email = "newlogin@orizonapp.io",
            token,
            newPassword = "NewPass@12345"
        });

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "newlogin@orizonapp.io",
            password = "NewPass@12345"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await loginResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
    }
}