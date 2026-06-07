using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.DTOs.Auth;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Infrastructure.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Users;

[Collection("Integration Tests")]
public class ProfilePictureControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public ProfilePictureControllerTests()
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
                        d => d.ServiceType == typeof(DbContextOptions<OrizonDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<OrizonDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));

                    // Substitui o file storage por uma implementação fake para os testes
                    var fileStorageDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IFileStorageService));
                    if (fileStorageDescriptor != null)
                        services.Remove(fileStorageDescriptor);

                    services.AddSingleton<IFileStorageService>(new FakeFileStorageService());
                });

                builder.UseSetting("Jwt:Secret",
                    "TestSecretKey2026SuperSeguroParaJwtOrizon!!");
                builder.UseSetting("Jwt:ExpiryHours", "1");
                builder.UseSetting("Jwt:Issuer", "orizonapp.io");
                builder.UseSetting("Jwt:Audience", "orizonapp.io");
                builder.UseSetting("ConnectionStrings:PostgreSQL",
                    _postgres.GetConnectionString());
                builder.UseSetting("ConnectionStrings:Redis", "");
                builder.UseSetting("Weather:BaseUrl", "https://api.open-meteo.com/v1");
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
        var context = scope.ServiceProvider.GetRequiredService<OrizonDbContext>();

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

        var register = new RegisterUserCommand("Aurel", "photo@orizonapp.io", "Test@12345");
        var registerResponse = await _client.PostAsJsonAsync("/auth/register", register);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static MultipartFormDataContent CreateImageContent(
        string contentType = "image/jpeg",
        string fileName = "photo.jpg",
        int sizeBytes = 100)
    {
        var content = new MultipartFormDataContent();
        var imageBytes = new byte[sizeBytes];
        // Cabeçalho JPEG válido
        if (sizeBytes >= 3) { imageBytes[0] = 0xFF; imageBytes[1] = 0xD8; imageBytes[2] = 0xFF; }
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task UploadProfilePicture_WhenNotAuthenticated_ShouldReturn401()
    {
        var unauthClient = _factory.CreateClient();
        var content = CreateImageContent();
        var response = await unauthClient.PostAsync("/users/profile-picture", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadProfilePicture_WhenValidJpeg_ShouldReturn200WithUrl()
    {
        var content = CreateImageContent("image/jpeg", "photo.jpg");
        var response = await _client.PostAsync("/users/profile-picture", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("url").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UploadProfilePicture_WhenValidPng_ShouldReturn200()
    {
        var content = CreateImageContent("image/png", "photo.png");
        var response = await _client.PostAsync("/users/profile-picture", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadProfilePicture_WhenValidWebp_ShouldReturn200()
    {
        var content = CreateImageContent("image/webp", "photo.webp");
        var response = await _client.PostAsync("/users/profile-picture", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadProfilePicture_WhenNoFile_ShouldReturn400()
    {
        var content = new MultipartFormDataContent();
        var response = await _client.PostAsync("/users/profile-picture", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadProfilePicture_WhenInvalidContentType_ShouldReturn400()
    {
        var content = CreateImageContent("application/pdf", "document.pdf");
        var response = await _client.PostAsync("/users/profile-picture", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadProfilePicture_WhenUploaded_ShouldUpdateProfilePictureUrl()
    {
        var content = CreateImageContent("image/jpeg", "photo.jpg");
        await _client.PostAsync("/users/profile-picture", content);

        var profileResponse = await _client.GetAsync("/users/profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();

        profile.GetProperty("profilePictureUrl").GetString().Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Implementação fake do IFileStorageService para uso nos testes de integração.
    /// Não salva arquivos em disco — apenas retorna uma URL simulada.
    /// </summary>
    private sealed class FakeFileStorageService : IFileStorageService
    {
        private static readonly string[] AllowedTypes =
        [
            "image/jpeg", "image/jpg", "image/png", "image/webp"
        ];

        public Task<string> SaveAsync(
            byte[] fileBytes,
            string fileName,
            string contentType,
            string folder,
            CancellationToken ct = default)
        {
            if (!AllowedTypes.Contains(contentType.ToLowerInvariant()))
                throw new InvalidOperationException(
                    "Tipo de arquivo não permitido. Use JPG, PNG ou WebP.");

            return Task.FromResult($"/{folder}/test-{Guid.NewGuid()}.jpg");
        }

        public Task DeleteAsync(string relativePath, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}