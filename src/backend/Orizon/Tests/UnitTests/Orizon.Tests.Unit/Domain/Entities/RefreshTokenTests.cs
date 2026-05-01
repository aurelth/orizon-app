using FluentAssertions;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Domain.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Constructor_WhenCreated_ShouldGenerateToken()
    {
        // Arrange & Act
        var token = new RefreshToken();

        // Assert
        token.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldNotBeRevoked()
    {
        // Arrange & Act
        var token = new RefreshToken();

        // Assert
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldExpireInSevenDays()
    {
        // Arrange
        var before = DateTime.UtcNow.AddDays(7);

        // Act
        var token = new RefreshToken();

        // Assert
        var after = DateTime.UtcNow.AddDays(7);
        token.ExpiresAt.Should().BeOnOrAfter(before);
        token.ExpiresAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInThePast_ShouldReturnTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Assert
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInTheFuture_ShouldReturnFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Assert
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        // Assert
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true
        };

        // Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };

        // Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenCalled_ShouldSetIsRevokedToTrue()
    {
        // Arrange
        var token = new RefreshToken();

        // Act
        token.Revoke();

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}