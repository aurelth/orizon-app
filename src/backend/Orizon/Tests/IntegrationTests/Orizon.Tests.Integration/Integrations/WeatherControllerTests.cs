using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Integrations;

[Collection("Integration Tests")]
public class WeatherControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public WeatherControllerTests()
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
                    "http://localhost:5000/auth/google/callback");
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

    private async Task<string> GetAuthTokenAsync()
    {
        var command = new RegisterUserCommand(
            "Aurel", "weather-test@orizonapp.io", "Test@12345");

        var response = await _client
            .PostAsJsonAsync("/auth/register", command);

        var auth = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        return auth!.AccessToken;
    }

    [Fact]
    public async Task GetWeather_WhenAuthenticated_ShouldReturn200WithWeatherData()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync(
            "/weather?latitude=-26.9195&longitude=-49.0661&timezone=America/Sao_Paulo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<WeatherDto>();

        result.Should().NotBeNull();
        result!.CurrentTemperature.Should().BeInRange(-20, 50);
        result.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWeather_WhenNotAuthenticated_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/weather?latitude=-26.9195&longitude=-49.0661");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}