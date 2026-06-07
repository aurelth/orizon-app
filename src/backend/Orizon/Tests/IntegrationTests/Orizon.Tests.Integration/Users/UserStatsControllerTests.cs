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
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Users;

[Collection("Integration Tests")]
public class UserStatsControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Guid _userId;

    public UserStatsControllerTests()
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
                builder.UseSetting("Worker:InternalUrl", "http://localhost:5011");
                builder.UseSetting("App:FrontendUrl", "http://localhost:4200");
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
            "Aurel", "stats@orizonapp.io", "Test@12345");
        var registerResponse = await _client
            .PostAsJsonAsync("/auth/register", register);
        var auth = await registerResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        _userId = Guid.Parse(auth!.UserId);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task SeedBriefingsAsync(IEnumerable<(DateOnly Date, BriefingStatus Status)> entries)
    {
        // Cria briefings diretamente no banco para os testes de stats
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrizonDbContext>();

        foreach (var (date, status) in entries)
        {
            var briefing = new BriefingEntry
            {
                UserId = _userId,
                Date = date,
                Status = status,
                GeneratedAt = status == BriefingStatus.Generated ? DateTime.UtcNow : null,
                AISummary = status == BriefingStatus.Generated ? "Bom dia!" : null,
                WeatherJson = JsonSerializer.Serialize(new { WeatherEmoji = "☀️" }),
            };
            await context.BriefingEntries.AddAsync(briefing);
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStats_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/users/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_WhenNoBriefings_ShouldReturnZeroStats()
    {
        var response = await _client.GetAsync("/users/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserStatsDto>();
        result.Should().NotBeNull();
        result!.TotalGenerated.Should().Be(0);
        result.CurrentStreak.Should().Be(0);
        result.MaxStreak.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_WhenBriefingsExist_ShouldReturnCorrectTotal()
    {
        // Arrange — 3 briefings gerados e 1 com falha (total = 3 pois só conta Generated)
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));

        await SeedBriefingsAsync(new[]
        {
            (today.AddDays(-3), BriefingStatus.Generated),
            (today.AddDays(-2), BriefingStatus.Generated),
            (today.AddDays(-1), BriefingStatus.Generated),
            (today, BriefingStatus.Failed),
        });

        var response = await _client.GetAsync("/users/stats");
        var result = await response.Content.ReadFromJsonAsync<UserStatsDto>();

        result!.TotalGenerated.Should().Be(3);
    }

    [Fact]
    public async Task GetStats_WhenConsecutiveDaysIncludingToday_ShouldReturnCorrectStreak()
    {
        // Arrange — 4 dias consecutivos terminando em hoje
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));

        await SeedBriefingsAsync(Enumerable.Range(0, 4)
            .Select(i => (today.AddDays(-i), BriefingStatus.Generated)));

        var response = await _client.GetAsync("/users/stats");
        var result = await response.Content.ReadFromJsonAsync<UserStatsDto>();

        result!.CurrentStreak.Should().Be(4);
        result.MaxStreak.Should().Be(4);
    }

    [Fact]
    public async Task GetStats_WhenStreakBroken_ShouldReturnZeroCurrentStreak()
    {
        // Arrange — briefings de 3 a 5 dias atrás, sem hoje nem ontem
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));

        await SeedBriefingsAsync(Enumerable.Range(2, 3)
            .Select(i => (today.AddDays(-i), BriefingStatus.Generated)));

        var response = await _client.GetAsync("/users/stats");
        var result = await response.Content.ReadFromJsonAsync<UserStatsDto>();

        result!.CurrentStreak.Should().Be(0);
        result.MaxStreak.Should().Be(3);
    }
}