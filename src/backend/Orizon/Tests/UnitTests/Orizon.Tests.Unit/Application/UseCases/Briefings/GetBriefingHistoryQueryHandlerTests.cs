using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;

namespace Orizon.Tests.Unit.Application.UseCases.Briefings;

public class GetBriefingHistoryQueryHandlerTests
{
    private readonly Mock<IBriefingRepository> _briefingRepoMock = new();
    private readonly GetBriefingHistoryQueryHandler _handler;

    public GetBriefingHistoryQueryHandlerTests()
    {
        _handler = new GetBriefingHistoryQueryHandler(_briefingRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBriefingsExist_ShouldReturnHistoryItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var briefings = new List<BriefingEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = today,
                Status = BriefingStatus.Generated,
                GeneratedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = today.AddDays(-1),
                Status = BriefingStatus.Failed,
                GeneratedAt = null,
            },
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 1, 10, default))
            .ReturnsAsync(briefings);

        var query = new GetBriefingHistoryQuery(userId.ToString(), 1, 10);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.First().Status.Should().Be("Generated");
        result.Items.Last().Status.Should().Be("Failed");
    }

    [Fact]
    public async Task Handle_WhenNoBriefings_ShouldReturnEmptyItems()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 1, 10, default))
            .ReturnsAsync(new List<BriefingEntry>());

        var query = new GetBriefingHistoryQuery(userId.ToString(), 1, 10);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldPassPaginationParametersToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 2, 5, default))
            .ReturnsAsync(new List<BriefingEntry>());

        var query = new GetBriefingHistoryQuery(userId.ToString(), 2, 5);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        _briefingRepoMock.Verify(
            r => r.GetByUserAsync(userId.ToString(), 2, 5, default),
            Times.Once);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
    }
}