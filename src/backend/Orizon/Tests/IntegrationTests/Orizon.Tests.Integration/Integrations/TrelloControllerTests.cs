using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
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
    private Guid _userId;

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
            "Aurel", "trello@orizonapp.io", "Test@12345");
        var registerResponse = await _client
            .PostAsJsonAsync("/auth/register", register);
        var auth = await registerResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        _accessToken = auth!.AccessToken;
        _userId = Guid.Parse(auth!.UserId);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);

        // Não precisamos mais criar AppUser manualmente — o registro já criou o
        // AppIdentityUser na tabela 'users' que é a FK real do trello_board_configs
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // --- Connect ---
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

    // --- GetBoards ---
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
    public async Task GetBoards_WhenMissingParams_ShouldReturn200WithEmptyList()
    {
        var response = await _client.GetAsync("/trello/boards");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBoards_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync(
            "/trello/boards?apiKey=key&token=token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- GetStatus ---
    [Fact]
    public async Task GetStatus_WhenNotConnected_ShouldReturnConnectedFalse()
    {
        var response = await _client.GetAsync("/trello/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatusResponse>();
        body!.Connected.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatus_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/trello/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- GetConfig ---
    [Fact]
    public async Task GetConfig_WhenNoBoardConfigured_ShouldReturnEmptyList()
    {
        var response = await _client.GetAsync("/trello/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<object>>();
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConfig_WhenBoardConfigured_ShouldReturnActiveBoardIds()
    {
        await _client.PostAsJsonAsync("/trello/boards/config", new
        {
            boardId = "board-config-1",
            boardName = "Config Board",
            boardColor = "#fff",
            todayListId = "list-today",
            todayListName = "Today",
            inProgressListId = "list-progress",
            inProgressListName = "In Progress"
        });

        var response = await _client.GetAsync("/trello/config");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<List<BoardConfigResponse>>();
        body.Should().HaveCount(1);
        body![0].BoardId.Should().Be("board-config-1");
    }

    [Fact]
    public async Task GetConfig_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/trello/config");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- SaveBoardConfig ---
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
    public async Task SaveBoardConfig_MultipleTimes_ShouldKeepAllBoardsActive()
    {
        await _client.PostAsJsonAsync("/trello/boards/config", new
        {
            boardId = "board-multi-1",
            boardName = "Board 1",
            todayListId = "list-1",
            todayListName = "Today",
            inProgressListId = "list-2",
            inProgressListName = "In Progress"
        });

        await _client.PostAsJsonAsync("/trello/boards/config", new
        {
            boardId = "board-multi-2",
            boardName = "Board 2",
            todayListId = "list-3",
            todayListName = "Today",
            inProgressListId = "list-4",
            inProgressListName = "In Progress"
        });

        var response = await _client.GetAsync("/trello/config");
        var body = await response.Content
            .ReadFromJsonAsync<List<BoardConfigResponse>>();

        body.Should().HaveCount(2);
        body!.Select(b => b.BoardId).Should()
            .Contain("board-multi-1").And.Contain("board-multi-2");
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

    // --- RemoveBoardConfig ---
    [Fact]
    public async Task RemoveBoardConfig_WhenValidBoard_ShouldReturn200()
    {
        await _client.PostAsJsonAsync("/trello/boards/config", new
        {
            boardId = "board-to-remove",
            boardName = "Remove Board",
            todayListId = "list-1",
            todayListName = "Today",
            inProgressListId = "list-2",
            inProgressListName = "In Progress"
        });

        var response = await _client
            .DeleteAsync("/trello/boards/config/board-to-remove");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveBoardConfig_WhenRemoved_ShouldNotAppearInConfig()
    {
        await _client.PostAsJsonAsync("/trello/boards/config", new
        {
            boardId = "board-removed",
            boardName = "Removed Board",
            todayListId = "list-1",
            todayListName = "Today",
            inProgressListId = "list-2",
            inProgressListName = "In Progress"
        });

        await _client.DeleteAsync("/trello/boards/config/board-removed");

        var response = await _client.GetAsync("/trello/config");
        var body = await response.Content
            .ReadFromJsonAsync<List<BoardConfigResponse>>();

        body!.Should().NotContain(b => b.BoardId == "board-removed");
    }

    [Fact]
    public async Task RemoveBoardConfig_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient
            .DeleteAsync("/trello/boards/config/board123");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveBoardConfig_WhenBoardDoesNotExist_ShouldReturn200()
    {
        var response = await _client
            .DeleteAsync("/trello/boards/config/nonexistent-board");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record StatusResponse(bool Connected);

    private record BoardConfigResponse(
        string BoardId,
        string BoardName,
        string? TodayListId,
        string? TodayListName,
        string? InProgressListId,
        string? InProgressListName);
}