using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Auth.Commands.RefreshToken;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;

namespace Orizon.Tests.Unit.Application.UseCases.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly RefreshTokenCommandHandler _handler;

    private readonly AppUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "aurel@orizonapp.io",
        DisplayName = "Aurel",
        ThemePreference = ThemePreference.Dark
    };

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _identityServiceMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldReturnNewAuthResponse()
    {
        // Arrange
        var existingToken = new RefreshToken
        {
            UserId = _testUser.Id.ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        var command = new RefreshTokenCommand(existingToken.Token);

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync(existingToken.Token, default))
            .ReturnsAsync(existingToken);

        _refreshTokenRepoMock
            .Setup(r => r.RevokeAllUserTokensAsync(
                existingToken.UserId, default))
            .Returns(Task.CompletedTask);

        _identityServiceMock
            .Setup(s => s.GetUserByIdAsync(
                existingToken.UserId, default))
            .ReturnsAsync(_testUser);

        _jwtServiceMock
            .Setup(s => s.GenerateToken(_testUser))
            .Returns("novo-jwt-token");

        _refreshTokenRepoMock
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("novo-jwt-token");
        result.Email.Should().Be(_testUser.Email);
    }

    [Fact]
    public async Task Handle_WhenTokenDoesNotExist_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var command = new RefreshTokenCommand("token-inexistente");

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync("token-inexistente", default))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*inválido*");
    }

    [Fact]
    public async Task Handle_WhenTokenIsRevoked_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var revokedToken = new RefreshToken
        {
            UserId = _testUser.Id.ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true
        };

        var command = new RefreshTokenCommand(revokedToken.Token);

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync(revokedToken.Token, default))
            .ReturnsAsync(revokedToken);

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*inválido*");
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpired_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var expiredToken = new RefreshToken
        {
            UserId = _testUser.Id.ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // expirado
            IsRevoked = false
        };

        var command = new RefreshTokenCommand(expiredToken.Token);

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync(expiredToken.Token, default))
            .ReturnsAsync(expiredToken);

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expirado*");
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldRevokeOldTokenAndSaveNew()
    {
        // Arrange
        var existingToken = new RefreshToken
        {
            UserId = _testUser.Id.ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        var command = new RefreshTokenCommand(existingToken.Token);

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync(existingToken.Token, default))
            .ReturnsAsync(existingToken);

        _refreshTokenRepoMock
            .Setup(r => r.RevokeAllUserTokensAsync(
                existingToken.UserId, default))
            .Returns(Task.CompletedTask);

        _identityServiceMock
            .Setup(s => s.GetUserByIdAsync(
                existingToken.UserId, default))
            .ReturnsAsync(_testUser);

        _jwtServiceMock
            .Setup(s => s.GenerateToken(_testUser))
            .Returns("novo-jwt-token");

        _refreshTokenRepoMock
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, default);

        // Assert — token antigo revogado e novo salvo
        _refreshTokenRepoMock.Verify(
            r => r.RevokeAllUserTokensAsync(existingToken.UserId, default),
            Times.Once);

        _refreshTokenRepoMock.Verify(
            r => r.AddAsync(It.IsAny<RefreshToken>(), default),
            Times.Once);
    }
}