using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Auth.Commands.ResetPassword;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Application.UseCases.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly ResetPasswordCommandHandler _handler;

    private readonly AppUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "aurel@orizonapp.io",
        DisplayName = "Aurel",
    };

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(
            _identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldResetPassword()
    {
        _identityServiceMock
            .Setup(s => s.GetUserByEmailAsync(
                _testUser.Email, default))
            .ReturnsAsync(_testUser);

        _identityServiceMock
            .Setup(s => s.CheckPasswordAsync(
                _testUser.Id.ToString(), "NewPass@123", default))
            .ReturnsAsync(false);

        _identityServiceMock
            .Setup(s => s.ResetPasswordAsync(
                _testUser.Id.ToString(), "valid-token",
                "NewPass@123", default))
            .ReturnsAsync(true);

        await _handler.Handle(new ResetPasswordCommand(
            _testUser.Email, "valid-token", "NewPass@123"), default);

        _identityServiceMock.Verify(
            s => s.ResetPasswordAsync(
                _testUser.Id.ToString(), "valid-token",
                "NewPass@123", default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrow()
    {
        _identityServiceMock
            .Setup(s => s.GetUserByEmailAsync(
                It.IsAny<string>(), default))
            .ReturnsAsync((AppUser?)null);

        var act = async () => await _handler.Handle(
            new ResetPasswordCommand(
                "notfound@orizonapp.io", "token", "NewPass@123"),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Usuário não encontrado.");
    }

    [Fact]
    public async Task Handle_WhenSamePassword_ShouldThrow()
    {
        _identityServiceMock
            .Setup(s => s.GetUserByEmailAsync(
                _testUser.Email, default))
            .ReturnsAsync(_testUser);

        _identityServiceMock
            .Setup(s => s.CheckPasswordAsync(
                _testUser.Id.ToString(), "SamePass@123", default))
            .ReturnsAsync(true);

        var act = async () => await _handler.Handle(
            new ResetPasswordCommand(
                _testUser.Email, "valid-token", "SamePass@123"),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A nova senha não pode ser igual à senha atual.");
    }

    [Fact]
    public async Task Handle_WhenInvalidToken_ShouldThrow()
    {
        _identityServiceMock
            .Setup(s => s.GetUserByEmailAsync(
                _testUser.Email, default))
            .ReturnsAsync(_testUser);

        _identityServiceMock
            .Setup(s => s.CheckPasswordAsync(
                _testUser.Id.ToString(), "NewPass@123", default))
            .ReturnsAsync(false);

        _identityServiceMock
            .Setup(s => s.ResetPasswordAsync(
                _testUser.Id.ToString(), "invalid-token",
                "NewPass@123", default))
            .ReturnsAsync(false);

        var act = async () => await _handler.Handle(
            new ResetPasswordCommand(
                _testUser.Email, "invalid-token", "NewPass@123"),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Token inválido ou expirado*");
    }
}