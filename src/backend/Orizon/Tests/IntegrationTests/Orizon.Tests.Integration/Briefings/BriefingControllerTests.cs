using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.DTOs.Briefing;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using Orizon.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Briefings;

[Collection("Integration Tests")]
public class BriefingControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private readonly WeatherDto _weather = new()
    {
        CurrentTemperature = 22,
        MinTemperature = 18,
        MaxTemperature = 26,
        Description = "Ensolarado",
        WeatherEmoji = "☀️",
        LocationName = "Blumenau"
    };

    public BriefingControllerTests()
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
                    "http://localhost:5010/auth/google/callback");
                builder.UseSetting("Anthropic:ApiKey", "test-api-key");
                builder.UseSetting("Trello:ApiKey", "test-api-key");
                builder.UseSetting("Trello:Token", "test-token");
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

    private async Task<string> RegisterAndLoginAsync(
        string email = "aurel@orizonapp.io")
    {
        var registerCommand = new RegisterUserCommand(
            "Aurel", email, "Test@12345");
        var registerResponse = await _client
            .PostAsJsonAsync("/auth/register", registerCommand);
        var auth = await registerResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();
        return auth!.AccessToken;
    }

    private async Task<(Guid UserId, string Token)> RegisterAndGetUserIdAsync(
    string email = "briefing@orizonapp.io")
    {
        var token = await RegisterAndLoginAsync(email);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<OrizonDbContext>();
        
        var identityUser = await context.Users
            .FirstAsync(u => u.Email == email);

        return (Guid.Parse(identityUser.Id), token);
    }

    private async Task SeedBriefingAsync(
    Guid userId,
    DateOnly date,
    BriefingStatus status = BriefingStatus.Generated)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<OrizonDbContext>();
        
        var appUserExists = await context.Set<Orizon.Domain.Entities.AppUser>()
            .AnyAsync(u => u.Id == userId);

        if (!appUserExists)
        {
            context.Set<Orizon.Domain.Entities.AppUser>().Add(new Orizon.Domain.Entities.AppUser
            {
                Id = userId,
                Email = "seed@orizonapp.io",
                DisplayName = "Seed User",
                LocationName = "Blumenau",
                Timezone = "America/Sao_Paulo",
            });
            await context.SaveChangesAsync();
        }

        var briefing = new BriefingEntry
        {
            UserId = userId,
            Date = date,
            Status = status,
            GeneratedAt = status == BriefingStatus.Generated
                ? DateTime.UtcNow : null,
            WeatherJson = JsonSerializer.Serialize(_weather),
            EmailSummaryJson = "[]",
            CalendarEventsJson = "[]",
            AISummary = "Bom dia, Aurel!",
            AISuggestions = "Ótimo dia para trabalhar.",
        };

        await context.BriefingEntries.AddAsync(briefing);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetToday_WhenNotAuthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/briefings/today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetToday_WhenBriefingExists_ShouldReturn200()
    {
        // Arrange
        var (userId, token) = await RegisterAndGetUserIdAsync(
            "today@orizonapp.io");

        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));
        await SeedBriefingAsync(userId, today);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/briefings/today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<BriefingResultDto>();
        result.Should().NotBeNull();
        result!.AISummary.Greeting.Should().Be("Bom dia, Aurel!");
    }

    [Fact]
    public async Task GetToday_WhenBriefingNotFound_ShouldReturn404()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("notfound@orizonapp.io");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/briefings/today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByDate_WhenValidDate_ShouldReturn200()
    {
        // Arrange
        var (userId, token) = await RegisterAndGetUserIdAsync(
            "bydate@orizonapp.io");

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        await SeedBriefingAsync(userId, date);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client
            .GetAsync($"/briefings/{date:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<BriefingResultDto>();
        result.Should().NotBeNull();
        result!.Date.Should().Be(date);
    }

    [Fact]
    public async Task GetByDate_WhenInvalidDateFormat_ShouldReturn400()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("invaliddate@orizonapp.io");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/briefings/data-invalida");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHistory_WhenAuthenticated_ShouldReturn200()
    {
        // Arrange
        var (userId, token) = await RegisterAndGetUserIdAsync(
            "history@orizonapp.io");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBriefingAsync(userId, today);
        await SeedBriefingAsync(userId, today.AddDays(-1));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/briefings/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetHistory_WhenNotAuthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/briefings/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistory_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var (userId, token) = await RegisterAndGetUserIdAsync(
            "pagination@orizonapp.io");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 3; i++)
            await SeedBriefingAsync(userId, today.AddDays(-i));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client
            .GetAsync("/briefings/history?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }
}