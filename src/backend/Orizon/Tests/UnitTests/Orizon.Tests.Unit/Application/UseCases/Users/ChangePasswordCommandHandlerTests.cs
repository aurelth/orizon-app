using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Users.Commands.ChangePassword;

namespace Orizon.Tests.Unit.Application.UseCases.Users;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPasswordChangedSuccessfully_ShouldNotThrow()
    {
        // Arrange
        var command = new ChangePasswordCommand(
            Guid.NewGuid(),
            "SenhaAtual@123",
            "NovaSenha@456");

        _identityServiceMock
            .Setup(s => s.ChangePasswordAsync(
                command.UserId.ToString(),
                command.CurrentPassword,
                command.NewPassword,
                default))
            .ReturnsAsync((true, Array.Empty<string>()));

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordIsWrong_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new ChangePasswordCommand(
            Guid.NewGuid(),
            "SenhaErrada@123",
            "NovaSenha@456");

        _identityServiceMock
            .Setup(s => s.ChangePasswordAsync(
                command.UserId.ToString(),
                command.CurrentPassword,
                command.NewPassword,
                default))
            .ReturnsAsync((false, new[] { "Senha atual incorreta." }));

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Senha atual incorreta.");
    }

    [Fact]
    public async Task Handle_WhenNewPasswordSameAsCurrent_ShouldThrowInvalidOperationException()
    {
        // Arrange — mesma senha
        var command = new ChangePasswordCommand(
            Guid.NewGuid(),
            "MesmaSenha@123",
            "MesmaSenha@123");

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert — deve falhar antes mesmo de chamar o serviço
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A nova senha deve ser diferente da senha atual.");

        _identityServiceMock.Verify(
            s => s.ChangePasswordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallIdentityServiceWithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ChangePasswordCommand(userId, "SenhaAtual@123", "NovaSenha@456");

        _identityServiceMock
            .Setup(s => s.ChangePasswordAsync(
                userId.ToString(),
                "SenhaAtual@123",
                "NovaSenha@456",
                default))
            .ReturnsAsync((true, Array.Empty<string>()));

        // Act
        await _handler.Handle(command, default);

        // Assert
        _identityServiceMock.Verify(
            s => s.ChangePasswordAsync(
                userId.ToString(),
                "SenhaAtual@123",
                "NovaSenha@456",
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsMultipleErrors_ShouldThrowWithFirstError()
    {
        // Arrange
        var command = new ChangePasswordCommand(
            Guid.NewGuid(),
            "SenhaAtual@123",
            "fraca");

        _identityServiceMock
            .Setup(s => s.ChangePasswordAsync(
                command.UserId.ToString(),
                command.CurrentPassword,
                command.NewPassword,
                default))
            .ReturnsAsync((false, new[] { "Senha muito fraca.", "Mínimo 8 caracteres." }));

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert — usa o primeiro erro
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Senha muito fraca.");
    }
}