using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.Interfaces.Services;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Services.Auth;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Auth;

public class IdentityServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private OrizonDbContext _context = null!;
    private IIdentityService _identityService = null!;

    public IdentityServiceTests()
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

        var services = new ServiceCollection();

        services.AddDbContext<OrizonDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));

        services.AddIdentityCore<AppIdentityUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<OrizonDbContext>();

        services.AddScoped<IIdentityService, IdentityService>();

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<OrizonDbContext>();

        await _context.Database.MigrateAsync();

        _identityService = provider.GetRequiredService<IIdentityService>();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CreateUserAsync_WhenValidData_ShouldCreateUser()
    {
        // Act
        var (success, userId, errors) = await _identityService
            .CreateUserAsync(
                "aurel@orizonapp.io",
                "Aurel",
                "Test@12345");

        // Assert
        success.Should().BeTrue();
        userId.Should().NotBeNullOrEmpty();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailAlreadyExists_ShouldReturnFailure()
    {
        // Arrange — criar primeiro
        await _identityService.CreateUserAsync(
            "duplicate@orizonapp.io", "Aurel", "Test@12345");

        // Act — tentar criar novamente
        var (success, userId, errors) = await _identityService
            .CreateUserAsync(
                "duplicate@orizonapp.io",
                "Aurel",
                "Test@12345");

        // Assert
        success.Should().BeFalse();
        userId.Should().BeNullOrEmpty();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        await _identityService.CreateUserAsync(
            "valid@orizonapp.io", "Aurel", "Test@12345");

        // Act
        var (success, userId) = await _identityService
            .ValidateCredentialsAsync("valid@orizonapp.io", "Test@12345");

        // Assert
        success.Should().BeTrue();
        userId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        await _identityService.CreateUserAsync(
            "wrongpass@orizonapp.io", "Aurel", "Test@12345");

        // Act
        var (success, userId) = await _identityService
            .ValidateCredentialsAsync(
                "wrongpass@orizonapp.io", "SenhaErrada");

        // Assert
        success.Should().BeFalse();
        userId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenEmailNotFound_ShouldReturnFailure()
    {
        // Act
        var (success, userId) = await _identityService
            .ValidateCredentialsAsync(
                "inexistente@orizonapp.io", "Test@12345");

        // Assert
        success.Should().BeFalse();
        userId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ShouldReturnAppUser()
    {
        // Arrange
        var (_, userId, _) = await _identityService
            .CreateUserAsync(
                "getuser@orizonapp.io", "Aurel", "Test@12345");

        // Act
        var appUser = await _identityService.GetUserByIdAsync(userId);

        // Assert
        appUser.Should().NotBeNull();
        appUser!.Email.Should().Be("getuser@orizonapp.io");
        appUser.DisplayName.Should().Be("Aurel");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Act
        var appUser = await _identityService
            .GetUserByIdAsync(Guid.NewGuid().ToString());

        // Assert
        appUser.Should().BeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_WhenEmailExists_ShouldReturnTrue()
    {
        // Arrange
        await _identityService.CreateUserAsync(
            "exists@orizonapp.io", "Aurel", "Test@12345");

        // Act
        var exists = await _identityService
            .EmailExistsAsync("exists@orizonapp.io");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WhenEmailDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var exists = await _identityService
            .EmailExistsAsync("inexistente@orizonapp.io");

        // Assert
        exists.Should().BeFalse();
    }
}