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
public class TrelloControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _accessToken = null!;

    private readonly string? _trelloApiKey =
        Environment.GetEnvironmentVariable("TRELLO_API_KEY");
    private readonly string? _trelloToken =
        Environment.GetEnvironmentVariable("TRELLO_TOKEN");

    public TrelloControllerTests()
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
                builder.UseSetting("Trello:ApiKey",
                    _trelloApiKey ?? "test-api-key");
                builder.UseSetting("Trello:Token",
                    _trelloToken ?? "test-token");
                builder.UseSetting("Worker:InternalUrl",
                    "http://localhost:5011");
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<OrizonDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        // registra e autentica usuário de teste
        var register = new RegisterUserCommand(
            "Aurel", "trello@orizonapp.io", "Test@12345");
        var registerResponse = await _client
            .PostAsJsonAsync("/auth/register", register);
        var auth = await registerResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        _accessToken = auth!.AccessToken;
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);

        // insere o usuário na tabela AppUser para satisfazer a FK de TrelloBoardConfig
        using var scope2 = _factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider
            .GetRequiredService<OrizonDbContext>();
        var userId = Guid.Parse(auth!.UserId);
        context2.Set<AppUser>().Add(new AppUser
        {
            Id = userId,
            Email = "trello@orizonapp.io",
            DisplayName = "Aurel",
            LocationName = "",
            Timezone = "America/Sao_Paulo",
        });
        await context2.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Connect_WhenValidCredentials_ShouldReturn200()
    {
        if (string.IsNullOrEmpty(_trelloApiKey) || string.IsNullOrEmpty(_trelloToken))
            return;

        var response = await _client.PostAsJsonAsync("/trello/connect", new
        {
            apiKey = _trelloApiKey,
            token = _trelloToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Connect_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/trello/connect", new
        {
            apiKey = "key",
            token = "token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBoards_WhenValidCredentials_ShouldReturn200WithBoards()
    {
        if (string.IsNullOrEmpty(_trelloApiKey) || string.IsNullOrEmpty(_trelloToken))
            return;

        var response = await _client.GetAsync(
            $"/trello/boards?apiKey={_trelloApiKey}&token={_trelloToken}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBoards_WhenMissingParams_ShouldReturn400()
    {
        var response = await _client.GetAsync("/trello/boards");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBoards_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync(
            "/trello/boards?apiKey=key&token=token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SaveBoardConfig_WhenValidData_ShouldReturn200()
    {
        var response = await _client.PostAsJsonAsync("/trello/boards/config", new
        {
            boardId = "board123",
            boardName = "Test Board",
            boardColor = "#6ee7b7",
            todayListId = "list-today",
            todayListName = "Today",
            inProgressListId = "list-progress",
            inProgressListName = "In Progress"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveBoardConfig_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/trello/boards/config", new
        {
            boardId = "board123",
            boardName = "Test Board"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}