using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Users.Commands.DeleteAccount;

namespace Orizon.Tests.Unit.Application.UseCases.Users;

public class DeleteAccountCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly DeleteAccountCommandHandler _handler;

    public DeleteAccountCommandHandlerTests()
    {
        _handler = new DeleteAccountCommandHandler(
            _identityServiceMock.Object,
            _userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsCorrect_ShouldDeleteAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteAccountCommand(userId, "SenhaCorreta@123");

        _identityServiceMock
            .Setup(s => s.CheckPasswordAsync(userId.ToString(), "SenhaCorreta@123", default))
            .ReturnsAsync(true);

        _userRepoMock
            .Setup(r => r.DeleteAsync(userId, default))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteAccountCommand(userId, "SenhaErrada@123");

        _identityServiceMock
            .Setup(s => s.CheckPasswordAsync(userId.ToString(), "SenhaErrada@123", default))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Senha incorreta.");
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ShouldNotCallDeleteAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteAccountCommand(userId, "SenhaErrada@123");

        _identityServiceMock
            .Setup(s => s.CheckPasswordAsync(userId.ToString(), "SenhaErrada@123", default))
            .ReturnsAsync(false);

        // Act
        try { await _handler.Handle(command, default); } catch { }

        // Assert
        _userRepoMock.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsCorrect_ShouldCallDeleteWithCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteAccountCommand(userId, "SenhaCorreta@123");

        _identityServiceMock
            .Setup(s => s.CheckPasswordAsync(userId.ToString(), "SenhaCorreta@123", default))
            .ReturnsAsync(true);

        _userRepoMock
            .Setup(r => r.DeleteAsync(userId, default))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, default);

        // Assert
        _userRepoMock.Verify(
            r => r.DeleteAsync(userId, default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCheckPasswordBeforeDeleting()
    {
        // Arrange — verifica a ordem das chamadas
        var callOrder = new List<string>();
        var userId = Guid.NewGuid();
        var command = new DeleteAccountCommand(userId, "SenhaCorreta@123");

        _identityServiceMock
            .Setup(s => s.CheckPasswordAsync(userId.ToString(), "SenhaCorreta@123", default))
            .Callback(() => callOrder.Add("checkPassword"))
            .ReturnsAsync(true);

        _userRepoMock
            .Setup(r => r.DeleteAsync(userId, default))
            .Callback(() => callOrder.Add("deleteAsync"))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, default);

        // Assert
        callOrder.Should().ContainInOrder("checkPassword", "deleteAsync");
    }
}