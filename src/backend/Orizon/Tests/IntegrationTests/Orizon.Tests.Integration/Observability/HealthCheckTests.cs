using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Infrastructure.Data;
using System.Net;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Observability;

[Collection("Integration Tests")]
public class HealthCheckTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public HealthCheckTests()
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

    [Fact]
    public async Task HealthReady_ShouldRespond()
    {
        var response = await _client.GetAsync("/health/ready");

        // 200 = tudo OK, 503 = alguma dependência indisponível (ex: Redis não configurado), o importante é que o endpoint existe e responde — não lança 404 nem 500        
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthLive_ShouldReturn200()
    {
        var response = await _client.GetAsync("/health/live");

        // /health/live não verifica dependências externas, deve sempre retornar 200 enquanto a aplicação estiver de pé        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Metrics_ShouldReturn200()
    {
        var response = await _client.GetAsync("/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Metrics_ShouldReturnPrometheusFormat()
    {
        var response = await _client.GetAsync("/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // verifica que o endpoint retorna formato Prometheus válido
        content.Should().Contain("# HELP");
        content.Should().Contain("# TYPE");
    }
}