using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;

namespace Orizon.Tests.Unit.Application.UseCases.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly RegisterUserCommandHandler _handler;

    private readonly AppUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "aurel@orizonapp.io",
        DisplayName = "Aurel",
        ThemePreference = ThemePreference.Dark
    };

    public RegisterUserCommandHandlerTests()
    {
        _handler = new RegisterUserCommandHandler(
            _identityServiceMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldReturnAuthResponse()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel", "aurel@orizonapp.io", "Test@12345");

        _identityServiceMock
            .Setup(s => s.CreateUserAsync(
                command.Email, command.DisplayName,
                command.Password, default))
            .ReturnsAsync((true, _testUser.Id.ToString(),
                Array.Empty<string>()));

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
        result.DisplayName.Should().Be(command.DisplayName);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel", "aurel@orizonapp.io", "Test@12345");

        _identityServiceMock
            .Setup(s => s.CreateUserAsync(
                command.Email, command.DisplayName,
                command.Password, default))
            .ReturnsAsync((false, string.Empty,
                new[] { "Email já está em uso." }));

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email já está em uso*");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveRefreshToken()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel", "aurel@orizonapp.io", "Test@12345");

        _identityServiceMock
            .Setup(s => s.CreateUserAsync(
                command.Email, command.DisplayName,
                command.Password, default))
            .ReturnsAsync((true, _testUser.Id.ToString(),
                Array.Empty<string>()));

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
        await _handler.Handle(command, default);

        // Assert — verifica que o RefreshToken foi salvo
        _refreshTokenRepoMock.Verify(
            r => r.AddAsync(It.IsAny<RefreshToken>(), default),
            Times.Once);
    }
}