using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Repositories;
using Testcontainers.PostgreSql;
using Xunit;

namespace Orizon.Tests.Integration.Auth;

public class RefreshTokenRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private OrizonDbContext _context = null!;
    private RefreshTokenRepository _repository = null!;
    private string _testUserId = string.Empty;

    public RefreshTokenRepositoryTests()
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

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<OrizonDbContext>();

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

        var userManager = provider.GetRequiredService<UserManager<AppIdentityUser>>();
        var testUser = new AppIdentityUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "aurel@orizonapp.io",
            Email = "aurel@orizonapp.io",
            DisplayName = "Aurel"
        };
        await userManager.CreateAsync(testUser, "Test@12345");
        _testUserId = testUser.Id;

        _repository = new RefreshTokenRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_WhenCalled_ShouldPersistTokenInDatabase()
    {
        var refreshToken = new RefreshToken { UserId = _testUserId };

        await _repository.AddAsync(refreshToken);

        var saved = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == refreshToken.Id);

        saved.Should().NotBeNull();
        saved!.Token.Should().Be(refreshToken.Token);
        saved.UserId.Should().Be(_testUserId);
        saved.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task GetByTokenAsync_WhenTokenExists_ShouldReturnToken()
    {
        var refreshToken = new RefreshToken { UserId = _testUserId };
        await _repository.AddAsync(refreshToken);

        var found = await _repository.GetByTokenAsync(refreshToken.Token);

        found.Should().NotBeNull();
        found!.Token.Should().Be(refreshToken.Token);
    }

    [Fact]
    public async Task GetByTokenAsync_WhenTokenDoesNotExist_ShouldReturnNull()
    {
        var found = await _repository.GetByTokenAsync("token-inexistente");

        found.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WhenCalled_ShouldRevokeAllActiveTokens()
    {
        var token1 = new RefreshToken { UserId = _testUserId };
        var token2 = new RefreshToken { UserId = _testUserId };
        var token3 = new RefreshToken { UserId = _testUserId };

        await _repository.AddAsync(token1);
        await _repository.AddAsync(token2);
        await _repository.AddAsync(token3);

        await _repository.RevokeAllUserTokensAsync(_testUserId);

        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == _testUserId)
            .ToListAsync();

        tokens.Should().HaveCount(3);
        tokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
        tokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task DeleteExpiredTokensAsync_WhenCalled_ShouldRemoveExpiredTokens()
    {
        var expiredToken = new RefreshToken
        {
            UserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        var validToken = new RefreshToken
        {
            UserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _repository.AddAsync(expiredToken);
        await _repository.AddAsync(validToken);

        await _repository.DeleteExpiredTokensAsync();

        var remaining = await _context.RefreshTokens
            .Where(t => t.UserId == _testUserId)
            .ToListAsync();

        remaining.Should().HaveCount(1);
        remaining[0].Token.Should().Be(validToken.Token);
    }
}