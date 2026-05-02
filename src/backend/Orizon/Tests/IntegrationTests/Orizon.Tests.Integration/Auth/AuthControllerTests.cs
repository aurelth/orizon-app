using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.UseCases.Auth.Commands.Login;
using Orizon.Application.UseCases.Auth.Commands.RefreshToken;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Auth;

public class AuthControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public AuthControllerTests()
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

    [Fact]
    public async Task Register_WhenValidRequest_ShouldReturn201()
    {
        var command = new RegisterUserCommand(
            "Aurel", "aurel@orizonapp.io", "Test@12345");

        var response = await _client
            .PostAsJsonAsync("/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturn400()
    {
        var command = new RegisterUserCommand(
            "Aurel", "duplicate@orizonapp.io", "Test@12345");

        await _client.PostAsJsonAsync("/auth/register", command);

        var response = await _client
            .PostAsJsonAsync("/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WhenInvalidData_ShouldReturn400()
    {
        var command = new RegisterUserCommand(
            "Aurel", "email-invalido", "Test@12345");

        var response = await _client
            .PostAsJsonAsync("/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WhenValidCredentials_ShouldReturn200()
    {
        var registerCommand = new RegisterUserCommand(
            "Aurel", "login@orizonapp.io", "Test@12345");
        await _client.PostAsJsonAsync("/auth/register", registerCommand);

        var loginCommand = new LoginCommand(
            "login@orizonapp.io", "Test@12345");

        var response = await _client
            .PostAsJsonAsync("/auth/login", loginCommand);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WhenInvalidCredentials_ShouldReturn401()
    {
        var command = new LoginCommand(
            "inexistente@orizonapp.io", "SenhaErrada");

        var response = await _client
            .PostAsJsonAsync("/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WhenValidToken_ShouldReturn200()
    {
        var registerCommand = new RegisterUserCommand(
            "Aurel", "refresh@orizonapp.io", "Test@12345");
        var registerResponse = await _client
            .PostAsJsonAsync("/auth/register", registerCommand);
        var authResult = await registerResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        var refreshCommand = new RefreshTokenCommand(
            authResult!.RefreshToken);

        var response = await _client
            .PostAsJsonAsync("/auth/refresh", refreshCommand);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBe(authResult.AccessToken);
    }

    [Fact]
    public async Task Refresh_WhenInvalidToken_ShouldReturn401()
    {
        var command = new RefreshTokenCommand("token-invalido");

        var response = await _client
            .PostAsJsonAsync("/auth/refresh", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WhenAuthenticated_ShouldReturn200()
    {
        var registerCommand = new RegisterUserCommand(
            "Aurel", "logout@orizonapp.io", "Test@12345");
        var registerResponse = await _client
            .PostAsJsonAsync("/auth/register", registerCommand);
        var authResult = await registerResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", authResult!.AccessToken);

        var response = await _client.PostAsync("/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}