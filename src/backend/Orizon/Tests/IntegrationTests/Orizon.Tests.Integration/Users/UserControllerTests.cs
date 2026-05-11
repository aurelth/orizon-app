using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.DTOs.User;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using Orizon.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Users;

[Collection("Integration Tests")]
public class UserControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Guid _userId;

    public UserControllerTests()
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
                builder.UseSetting("Worker:InternalUrl",
                    "http://localhost:5011");
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

        var register = new RegisterUserCommand(
            "Aurel", "user@orizonapp.io", "Test@12345");
        var registerResponse = await _client
            .PostAsJsonAsync("/auth/register", register);
        var auth = await registerResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        _userId = Guid.Parse(auth!.UserId);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // insere AppUser para satisfazer FK
        using var scope2 = _factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider
            .GetRequiredService<OrizonDbContext>();
        context2.Set<AppUser>().Add(new AppUser
        {
            Id = _userId,
            Email = "user@orizonapp.io",
            DisplayName = "Aurel",
            LocationName = "Blumenau",
            Timezone = "America/Sao_Paulo",
            ThemePreference = ThemePreference.Dark,
        });
        await context2.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // --- GetProfile ---

    [Fact]
    public async Task GetProfile_WhenAuthenticated_ShouldReturn200WithProfile()
    {
        var response = await _client.GetAsync("/users/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<UserProfileDto>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("user@orizonapp.io");
        result.DisplayName.Should().Be("Aurel");
        result.ThemePreference.Should().Be("Dark");
    }

    [Fact]
    public async Task GetProfile_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync("/users/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- UpdateProfile ---

    [Fact]
    public async Task UpdateProfile_WhenValidData_ShouldReturn204()
    {
        var response = await _client.PutAsJsonAsync("/users/profile", new
        {
            displayName = "Aurel Lossou",
            profilePictureUrl = (string?)null,
            themePreference = "Light"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProfile_WhenUpdated_ShouldReflectChanges()
    {
        await _client.PutAsJsonAsync("/users/profile", new
        {
            displayName = "Nome Atualizado",
            profilePictureUrl = "https://example.com/photo.jpg",
            themePreference = "Light"
        });

        var response = await _client.GetAsync("/users/profile");
        var result = await response.Content
            .ReadFromJsonAsync<UserProfileDto>();

        result!.DisplayName.Should().Be("Nome Atualizado");
        result.ProfilePictureUrl.Should().Be("https://example.com/photo.jpg");
        result.ThemePreference.Should().Be("Light");
    }

    [Fact]
    public async Task UpdateProfile_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.PutAsJsonAsync("/users/profile", new
        {
            displayName = "Aurel",
            profilePictureUrl = (string?)null,
            themePreference = "Dark"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WhenThemeIsInvalid_ShouldReturn204AndKeepOldTheme()
    {
        await _client.PutAsJsonAsync("/users/profile", new
        {
            displayName = "Aurel",
            profilePictureUrl = (string?)null,
            themePreference = "InvalidTheme"
        });

        var response = await _client.GetAsync("/users/profile");
        var result = await response.Content
            .ReadFromJsonAsync<UserProfileDto>();

        result!.ThemePreference.Should().Be("Dark");
    }
}