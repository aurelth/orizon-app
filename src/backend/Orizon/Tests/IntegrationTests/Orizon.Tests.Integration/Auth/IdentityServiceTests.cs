using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Repositories;
using Orizon.Infrastructure.Services.Auth;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Auth;

public class IdentityServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private ServiceProvider _provider = null!;
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
        services.AddLogging();

        services.AddDbContext<OrizonDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()),
            ServiceLifetime.Singleton);

        services.AddIdentity<AppIdentityUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<OrizonDbContext>()
        .AddDefaultTokenProviders();

        services.AddSingleton<IIdentityService, IdentityService>();
        services.AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>();

        _provider = services.BuildServiceProvider();
        _context = _provider.GetRequiredService<OrizonDbContext>();

        var maxRetries = 5;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await _context.Database.MigrateAsync();
                break;
            }
            catch (Exception) when (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        _identityService = _provider.GetRequiredService<IIdentityService>();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CreateUserAsync_WhenValidData_ShouldCreateUser()
    {
        var (success, userId, errors) = await _identityService
            .CreateUserAsync("aurel@orizonapp.io", "Aurel", "Test@12345");

        success.Should().BeTrue();
        userId.Should().NotBeNullOrEmpty();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailAlreadyExists_ShouldReturnFailure()
    {
        await _identityService.CreateUserAsync(
            "duplicate@orizonapp.io", "Aurel", "Test@12345");

        var (success, userId, errors) = await _identityService
            .CreateUserAsync("duplicate@orizonapp.io", "Aurel", "Test@12345");

        success.Should().BeFalse();
        userId.Should().BeNullOrEmpty();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenValidCredentials_ShouldReturnSuccess()
    {
        await _identityService.CreateUserAsync(
            "valid@orizonapp.io", "Aurel", "Test@12345");

        var (success, userId) = await _identityService
            .ValidateCredentialsAsync("valid@orizonapp.io", "Test@12345");

        success.Should().BeTrue();
        userId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenInvalidPassword_ShouldReturnFailure()
    {
        await _identityService.CreateUserAsync(
            "wrongpass@orizonapp.io", "Aurel", "Test@12345");

        var (success, userId) = await _identityService
            .ValidateCredentialsAsync("wrongpass@orizonapp.io", "SenhaErrada");

        success.Should().BeFalse();
        userId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenEmailNotFound_ShouldReturnFailure()
    {
        var (success, userId) = await _identityService
            .ValidateCredentialsAsync("inexistente@orizonapp.io", "Test@12345");

        success.Should().BeFalse();
        userId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ShouldReturnAppUser()
    {
        var (_, userId, _) = await _identityService
            .CreateUserAsync("getuser@orizonapp.io", "Aurel", "Test@12345");

        var appUser = await _identityService.GetUserByIdAsync(userId);

        appUser.Should().NotBeNull();
        appUser!.Email.Should().Be("getuser@orizonapp.io");
        appUser.DisplayName.Should().Be("Aurel");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var appUser = await _identityService
            .GetUserByIdAsync(Guid.NewGuid().ToString());

        appUser.Should().BeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_WhenEmailExists_ShouldReturnTrue()
    {
        await _identityService.CreateUserAsync(
            "exists@orizonapp.io", "Aurel", "Test@12345");

        var exists = await _identityService
            .EmailExistsAsync("exists@orizonapp.io");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WhenEmailDoesNotExist_ShouldReturnFalse()
    {
        var exists = await _identityService
            .EmailExistsAsync("inexistente@orizonapp.io");

        exists.Should().BeFalse();
    }
}