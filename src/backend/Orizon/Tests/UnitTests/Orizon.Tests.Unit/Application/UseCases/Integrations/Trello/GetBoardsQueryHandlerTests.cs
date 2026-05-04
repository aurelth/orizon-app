using FluentAssertions;
using Moq;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Integrations.Trello.Query;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.Trello;

public class GetBoardsQueryHandlerTests
{
    private readonly Mock<ITrelloService> _trelloServiceMock;
    private readonly GetBoardsQueryHandler _handler;

    public GetBoardsQueryHandlerTests()
    {
        _trelloServiceMock = new Mock<ITrelloService>();
        _handler = new GetBoardsQueryHandler(_trelloServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldReturnBoards()
    {
        var query = new GetBoardsQuery("api-key", "token");
        var expectedBoards = new List<TrelloBoardDto>
        {
            new() { BoardId = "board1", Name = "Dev", IsActive = true },
            new() { BoardId = "board2", Name = "Personal", IsActive = true },
        };

        _trelloServiceMock
            .Setup(s => s.GetBoardsAsync(
                query.ApiKey,
                query.Token,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBoards);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedBoards);
    }

    [Fact]
    public async Task Handle_WhenNoBoardsFound_ShouldReturnEmptyList()
    {
        var query = new GetBoardsQuery("api-key", "token");

        _trelloServiceMock
            .Setup(s => s.GetBoardsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }
}