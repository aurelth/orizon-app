using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Auth.Commands.ForgotPassword;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Application.UseCases.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IEmailNotificationService> _emailServiceMock = new();
    private readonly ForgotPasswordCommandHandler _handler;

    private readonly AppUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "aurel@orizonapp.io",
        DisplayName = "Aurel",
    };

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(
            _identityServiceMock.Object,
            _emailServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldGenerateTokenAndSendEmail()
    {
        _identityServiceMock
            .Setup(s => s.GetUserByEmailAsync(
                _testUser.Email, default))
            .ReturnsAsync(_testUser);

        _identityServiceMock
            .Setup(s => s.GeneratePasswordResetTokenAsync(
                _testUser.Id.ToString(), default))
            .ReturnsAsync("reset-token-123");

        _emailServiceMock
            .Setup(s => s.SendPasswordResetEmailAsync(
                _testUser.Email, _testUser.DisplayName,
                "reset-token-123", default))
            .Returns(Task.CompletedTask);

        await _handler.Handle(
            new ForgotPasswordCommand(_testUser.Email), default);

        _identityServiceMock.Verify(
            s => s.GeneratePasswordResetTokenAsync(
                _testUser.Id.ToString(), default),
            Times.Once);

        _emailServiceMock.Verify(
            s => s.SendPasswordResetEmailAsync(
                _testUser.Email, _testUser.DisplayName,
                "reset-token-123", default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldNotSendEmail()
    {
        _identityServiceMock
            .Setup(s => s.GetUserByEmailAsync(
                It.IsAny<string>(), default))
            .ReturnsAsync((AppUser?)null);

        await _handler.Handle(
            new ForgotPasswordCommand("notfound@orizonapp.io"), default);

        _emailServiceMock.Verify(
            s => s.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldNotThrow()
    {
        _identityServiceMock
            .Setup(s => s.GetUserByEmailAsync(
                It.IsAny<string>(), default))
            .ReturnsAsync((AppUser?)null);

        var act = async () => await _handler.Handle(
            new ForgotPasswordCommand("notfound@orizonapp.io"), default);

        await act.Should().NotThrowAsync();
    }
}