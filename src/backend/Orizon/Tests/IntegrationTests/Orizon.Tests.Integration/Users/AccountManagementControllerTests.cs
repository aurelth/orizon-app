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

namespace Orizon.Tests.Integration.Users;

[Collection("Integration Tests")]
public class AccountManagementControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Guid _userId;

    public AccountManagementControllerTests()
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
            "Aurel", "account@orizonapp.io", "Test@12345");
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

    // --- ChangePassword ---

    [Fact]
    public async Task ChangePassword_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.PutAsJsonAsync("/users/change-password", new
        {
            currentPassword = "Test@12345",
            newPassword = "NovaSenha@678"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WhenCurrentPasswordIsCorrect_ShouldReturn204()
    {
        var response = await _client.PutAsJsonAsync("/users/change-password", new
        {
            currentPassword = "Test@12345",
            newPassword = "NovaSenha@678"
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WhenCurrentPasswordIsWrong_ShouldReturn400()
    {
        var response = await _client.PutAsJsonAsync("/users/change-password", new
        {
            currentPassword = "SenhaErrada@000",
            newPassword = "NovaSenha@678"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WhenNewPasswordSameAsCurrent_ShouldReturn400()
    {
        var response = await _client.PutAsJsonAsync("/users/change-password", new
        {
            currentPassword = "Test@12345",
            newPassword = "Test@12345"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WhenChanged_ShouldBeAbleToLoginWithNewPassword()
    {
        // Altera a senha
        await _client.PutAsJsonAsync("/users/change-password", new
        {
            currentPassword = "Test@12345",
            newPassword = "NovaSenha@678"
        });

        // Tenta logar com a nova senha
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "account@orizonapp.io",
            password = "NovaSenha@678"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- DeleteAccount ---

    [Fact]
    public async Task DeleteAccount_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, "/users/account")
        {
            Content = JsonContent.Create(new { password = "Test@12345" })
        };

        var response = await unauthClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_WhenPasswordIsWrong_ShouldReturn400()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/users/account")
        {
            Content = JsonContent.Create(new { password = "SenhaErrada@000" })
        };
        request.Headers.Authorization =
            _client.DefaultRequestHeaders.Authorization;

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteAccount_WhenPasswordIsCorrect_ShouldReturn204()
    {
        // Cria um usuário dedicado para o teste de exclusão
        var registerResponse = await _factory.CreateClient()
            .PostAsJsonAsync("/auth/register",
                new RegisterUserCommand("Delete User", "delete@orizonapp.io", "Test@12345"));

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var deleteClient = _factory.CreateClient();
        deleteClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Delete, "/users/account")
        {
            Content = JsonContent.Create(new { password = "Test@12345" })
        };

        var response = await deleteClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAccount_WhenDeleted_ShouldNotBeAbleToLogin()
    {
        // Cria um usuário dedicado para o teste de exclusão
        var registerResponse = await _factory.CreateClient()
            .PostAsJsonAsync("/auth/register",
                new RegisterUserCommand("Delete User 2", "delete2@orizonapp.io", "Test@12345"));

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var deleteClient = _factory.CreateClient();
        deleteClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Exclui a conta
        var request = new HttpRequestMessage(HttpMethod.Delete, "/users/account")
        {
            Content = JsonContent.Create(new { password = "Test@12345" })
        };
        await deleteClient.SendAsync(request);

        // Tenta logar após exclusão
        var loginResponse = await _factory.CreateClient()
            .PostAsJsonAsync("/auth/login", new
            {
                email = "delete2@orizonapp.io",
                password = "Test@12345"
            });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}