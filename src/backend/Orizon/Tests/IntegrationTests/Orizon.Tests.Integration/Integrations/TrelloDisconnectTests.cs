using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Integrations;

[Collection("Integration Tests")]
public class TrelloDisconnectTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public TrelloDisconnectTests()
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

    private async Task<(Guid UserId, string Token)> RegisterAndGetUserIdAsync(
        string email = "trello@orizonapp.io")
    {
        var command = new RegisterUserCommand("Aurel", email, "Test@12345");
        var response = await _client.PostAsJsonAsync("/auth/register", command);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrizonDbContext>();
        var user = await context.Users.FirstAsync(u => u.Email == email);

        return (Guid.Parse(user.Id), auth!.AccessToken);
    }

    private async Task SeedTrelloConnectionAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrizonDbContext>();

        var appUser = await context.Set<AppUser>()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (appUser is null)
        {
            context.Set<AppUser>().Add(new AppUser
            {
                Id = userId,
                Email = "trello@orizonapp.io",
                DisplayName = "Aurel",
                LocationName = "Blumenau",
                Timezone = "America/Sao_Paulo",
                TrelloApiKey = "api-key-123",
                TrelloToken = "token-123",
                TrelloEnabled = true,
            });
        }
        else
        {
            appUser.TrelloApiKey = "api-key-123";
            appUser.TrelloToken = "token-123";
            appUser.TrelloEnabled = true;
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Disconnect_WhenAuthenticated_ShouldReturn204()
    {
        var (userId, token) = await RegisterAndGetUserIdAsync("disconnect@orizonapp.io");
        await SeedTrelloConnectionAsync(userId);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync("/trello/disconnect");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Disconnect_WhenNotAuthenticated_ShouldReturn401()
    {
        var response = await _client.DeleteAsync("/trello/disconnect");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disconnect_WhenDisconnected_StatusShouldReturnFalse()
    {
        var (userId, token) = await RegisterAndGetUserIdAsync("disconnect2@orizonapp.io");
        await SeedTrelloConnectionAsync(userId);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        await _client.DeleteAsync("/trello/disconnect");

        var statusResponse = await _client.GetAsync("/trello/status");
        var status = await statusResponse.Content
            .ReadFromJsonAsync<Dictionary<string, bool>>();

        status!["connected"].Should().BeFalse();
    }
}