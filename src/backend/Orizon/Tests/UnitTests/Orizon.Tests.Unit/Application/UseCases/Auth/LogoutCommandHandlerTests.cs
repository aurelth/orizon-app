using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Auth.Commands.Logout;

namespace Orizon.Tests.Unit.Application.UseCases.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(_refreshTokenRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldRevokeAllUserTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new LogoutCommand(userId);

        _refreshTokenRepoMock
            .Setup(r => r.RevokeAllUserTokensAsync(userId, default))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, default);

        // Assert
        _refreshTokenRepoMock.Verify(
            r => r.RevokeAllUserTokensAsync(userId, default),
            Times.Once);
    }
}