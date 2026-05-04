using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Integrations.Trello.Command;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.Trello;

public class SaveBoardConfigCommandHandlerTests
{
    private readonly Mock<ITrelloBoardConfigRepository> _repositoryMock;
    private readonly SaveBoardConfigCommandHandler _handler;

    public SaveBoardConfigCommandHandlerTests()
    {
        _repositoryMock = new Mock<ITrelloBoardConfigRepository>();
        _handler = new SaveBoardConfigCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenConfigDoesNotExist_ShouldAddNewConfig()
    {
        var userId = Guid.NewGuid();
        var command = new SaveBoardConfigCommand(
            userId, "board1", "Dev", "#fb923c",
            "list1", "Today", "list2", "In Progress");

        _repositoryMock
            .Setup(r => r.GetByUserAndBoardAsync(
                userId, "board1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrelloBoardConfig?)null);

        await _handler.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(
            r => r.AddAsync(
                It.Is<TrelloBoardConfig>(c =>
                    c.BoardId == "board1" &&
                    c.UserId == userId &&
                    c.TodayListId == "list1" &&
                    c.InProgressListId == "list2"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenConfigExists_ShouldUpdateExistingConfig()
    {
        var userId = Guid.NewGuid();
        var command = new SaveBoardConfigCommand(
            userId, "board1", "Dev", "#fb923c",
            "list1-new", "Today", "list2-new", "In Progress");

        var existing = new TrelloBoardConfig
        {
            UserId = userId,
            BoardId = "board1",
            TodayListId = "list1-old",
            InProgressListId = "list2-old",
        };

        _repositoryMock
            .Setup(r => r.GetByUserAndBoardAsync(
                userId, "board1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _handler.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(
            r => r.UpdateAsync(
                It.Is<TrelloBoardConfig>(c =>
                    c.TodayListId == "list1-new" &&
                    c.InProgressListId == "list2-new"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<TrelloBoardConfig>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}