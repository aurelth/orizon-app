using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Auth.Commands.Login;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;

namespace Orizon.Tests.Unit.Application.UseCases.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly LoginCommandHandler _handler;

    private readonly AppUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "aurel@orizonapp.io",
        DisplayName = "Aurel",
        ThemePreference = ThemePreference.Dark
    };

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _identityServiceMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var command = new LoginCommand("aurel@orizonapp.io", "Test@12345");

        _identityServiceMock
            .Setup(s => s.ValidateCredentialsAsync(
                command.Email, command.Password, default))
            .ReturnsAsync((true, _testUser.Id.ToString()));

        _identityServiceMock
            .Setup(s => s.GetUserByIdAsync(
                _testUser.Id.ToString(), default))
            .ReturnsAsync(_testUser);

        _jwtServiceMock
            .Setup(s => s.GenerateToken(_testUser))
            .Returns("jwt-token");

        _refreshTokenRepoMock
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt-token");
        result.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_WhenInvalidCredentials_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var command = new LoginCommand("aurel@orizonapp.io", "SenhaErrada");

        _identityServiceMock
            .Setup(s => s.ValidateCredentialsAsync(
                command.Email, command.Password, default))
            .ReturnsAsync((false, string.Empty));

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}