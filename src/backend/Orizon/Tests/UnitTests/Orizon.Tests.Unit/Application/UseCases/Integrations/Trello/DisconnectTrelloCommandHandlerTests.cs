using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Integrations.Trello.Command;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.Trello;

public class DisconnectTrelloCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITrelloBoardConfigRepository> _boardConfigRepoMock = new();
    private readonly DisconnectTrelloCommandHandler _handler;

    public DisconnectTrelloCommandHandlerTests()
    {
        _handler = new DisconnectTrelloCommandHandler(
            _userRepoMock.Object,
            _boardConfigRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldClearTrelloCredentials()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            TrelloApiKey = "api-key-123",
            TrelloToken = "token-123",
            TrelloEnabled = true,
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        _boardConfigRepoMock
            .Setup(r => r.GetByUserAsync(userId, default))
            .ReturnsAsync(new List<TrelloBoardConfig>());

        await _handler.Handle(new DisconnectTrelloCommand(userId), default);

        user.TrelloApiKey.Should().BeNull();
        user.TrelloToken.Should().BeNull();
        user.TrelloEnabled.Should().BeFalse();

        _userRepoMock.Verify(
            r => r.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserHasBoards_ShouldDeleteAllBoards()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser { Id = userId, TrelloEnabled = true };

        var boards = new List<TrelloBoardConfig>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, BoardId = "board-1" },
            new() { Id = Guid.NewGuid(), UserId = userId, BoardId = "board-2" },
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        _boardConfigRepoMock
            .Setup(r => r.GetByUserAsync(userId, default))
            .ReturnsAsync(boards);

        _boardConfigRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), default))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new DisconnectTrelloCommand(userId), default);

        _boardConfigRepoMock.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), default),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldNotCallUpdate()
    {
        var userId = Guid.NewGuid();

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((AppUser?)null);

        await _handler.Handle(new DisconnectTrelloCommand(userId), default);

        _userRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<AppUser>(), default),
            Times.Never);
    }
}