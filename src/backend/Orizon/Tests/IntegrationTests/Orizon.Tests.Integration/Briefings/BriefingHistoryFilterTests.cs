using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
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
public class BriefingHistoryFilterTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Guid _userId;

    private readonly WeatherDto _weather = new()
    {
        CurrentTemperature = 22,
        MinTemperature = 18,
        MaxTemperature = 26,
        Description = "Ensolarado",
        WeatherEmoji = "☀️",
        LocationName = "Blumenau"
    };

    public BriefingHistoryFilterTests()
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
            "Aurel", "history-filter@orizonapp.io", "Test@12345");
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

    private async Task SeedBriefingAsync(DateOnly date, BriefingStatus status = BriefingStatus.Generated)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrizonDbContext>();

        var briefing = new BriefingEntry
        {
            UserId = _userId,
            Date = date,
            Status = status,
            GeneratedAt = status == BriefingStatus.Generated ? DateTime.UtcNow : null,
            WeatherJson = JsonSerializer.Serialize(_weather),
            AISummary = "Bom dia, Aurel!",
        };

        await context.BriefingEntries.AddAsync(briefing);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetHistory_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/briefings/history");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistory_WhenNoBriefings_ShouldReturnEmptyWithZeroTotal()
    {
        var response = await _client.GetAsync("/briefings/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetHistory_WhenBriefingsExist_ShouldReturnAllWithCorrectTotal()
    {
        // Arrange — 3 briefings em dias diferentes
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBriefingAsync(today);
        await SeedBriefingAsync(today.AddDays(-1));
        await SeedBriefingAsync(today.AddDays(-2));

        var response = await _client.GetAsync("/briefings/history");
        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();

        result!.Items.Should().HaveCount(3);
        result.Total.Should().Be(3);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetHistory_WithDateFromFilter_ShouldReturnOnlyBriefingsAfterDate()
    {
        // Arrange — briefings de 10, 5, 3 e 1 dias atrás
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBriefingAsync(today.AddDays(-10));
        await SeedBriefingAsync(today.AddDays(-5));
        await SeedBriefingAsync(today.AddDays(-3));
        await SeedBriefingAsync(today.AddDays(-1));

        // Filtra a partir de 7 dias atrás — deve retornar apenas 3 briefings
        var dateFrom = today.AddDays(-7).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/briefings/history?dateFrom={dateFrom}");
        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();

        result!.Items.Should().HaveCount(3);
        result.Total.Should().Be(3);
    }

    [Fact]
    public async Task GetHistory_WithDateToFilter_ShouldReturnOnlyBriefingsBeforeDate()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBriefingAsync(today.AddDays(-10));
        await SeedBriefingAsync(today.AddDays(-5));
        await SeedBriefingAsync(today.AddDays(-1));

        // Filtra até 7 dias atrás — deve retornar apenas 1 briefing (o de 10 dias atrás)
        var dateTo = today.AddDays(-7).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/briefings/history?dateTo={dateTo}");
        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();

        result!.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetHistory_WithDateFromAndDateToFilter_ShouldReturnOnlyBriefingsInRange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBriefingAsync(today.AddDays(-15));
        await SeedBriefingAsync(today.AddDays(-10));
        await SeedBriefingAsync(today.AddDays(-5));
        await SeedBriefingAsync(today.AddDays(-1));

        // Filtra entre 12 e 4 dias atrás — deve retornar apenas 2 briefings
        var dateFrom = today.AddDays(-12).ToString("yyyy-MM-dd");
        var dateTo = today.AddDays(-4).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync(
            $"/briefings/history?dateFrom={dateFrom}&dateTo={dateTo}");
        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();

        result!.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetHistory_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange — 5 briefings
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 5; i++)
            await SeedBriefingAsync(today.AddDays(-i));

        // Página 1 com pageSize 2 — deve retornar 2 itens
        var response = await _client.GetAsync("/briefings/history?page=1&pageSize=2");
        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();

        result!.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetHistory_ShouldReturnItemsOrderedByDateDescending()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBriefingAsync(today.AddDays(-2));
        await SeedBriefingAsync(today.AddDays(-1));
        await SeedBriefingAsync(today);

        var response = await _client.GetAsync("/briefings/history");
        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();

        // O primeiro item deve ser o mais recente
        var items = result!.Items.ToList();
        items[0].Date.Should().Be(today);
        items[1].Date.Should().Be(today.AddDays(-1));
        items[2].Date.Should().Be(today.AddDays(-2));
    }

    [Fact]
    public async Task GetHistory_ShouldReturnWeatherEmojiAndGreeting()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBriefingAsync(today);

        var response = await _client.GetAsync("/briefings/history");
        var result = await response.Content
            .ReadFromJsonAsync<GetBriefingHistoryResult>();

        var item = result!.Items.First();
        item.WeatherEmoji.Should().Be("☀️");
        item.Greeting.Should().Be("Bom dia, Aurel!");
    }
}