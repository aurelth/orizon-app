using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using System.Text.Json;

namespace Orizon.Tests.Unit.Application.UseCases.Briefings;

public class GetBriefingHistoryQueryHandlerTests
{
    private readonly Mock<IBriefingRepository> _briefingRepoMock = new();
    private readonly GetBriefingHistoryQueryHandler _handler;

    public GetBriefingHistoryQueryHandlerTests()
    {
        _handler = new GetBriefingHistoryQueryHandler(_briefingRepoMock.Object);
    }

    private static BriefingEntry CreateBriefing(
        Guid userId,
        DateOnly date,
        BriefingStatus status = BriefingStatus.Generated,
        string? weatherEmoji = "☀️")
    {
        var weatherJson = weatherEmoji is not null
            ? JsonSerializer.Serialize(new { WeatherEmoji = weatherEmoji })
            : null;

        return new BriefingEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = date,
            Status = status,
            GeneratedAt = status == BriefingStatus.Generated ? DateTime.UtcNow : null,
            AISummary = status == BriefingStatus.Generated ? "Bom dia, Aurel!" : null,
            WeatherJson = weatherJson,
        };
    }

    [Fact]
    public async Task Handle_WhenBriefingsExist_ShouldReturnHistoryItemsWithTotals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var briefings = new List<BriefingEntry>
        {
            CreateBriefing(userId, today),
            CreateBriefing(userId, today.AddDays(-1), BriefingStatus.Failed),
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 1, 10, null, null, default))
            .ReturnsAsync((briefings, 2));

        var query = new GetBriefingHistoryQuery(userId.ToString(), 1, 10);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Total.Should().Be(2);
        result.TotalPages.Should().Be(1);
        result.Items.First().Status.Should().Be("Generated");
        result.Items.Last().Status.Should().Be("Failed");
    }

    [Fact]
    public async Task Handle_WhenNoBriefings_ShouldReturnEmptyItemsAndZeroTotals()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 1, 10, null, null, default))
            .ReturnsAsync((new List<BriefingEntry>(), 0));

        var query = new GetBriefingHistoryQuery(userId.ToString(), 1, 10);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalPagesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 1, 5, null, null, default))
            .ReturnsAsync((new List<BriefingEntry>(), 11));

        var query = new GetBriefingHistoryQuery(userId.ToString(), 1, 5);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert        
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldPassPaginationAndFiltersToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var dateTo = DateOnly.FromDateTime(DateTime.UtcNow);

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 2, 5, dateFrom, dateTo, default))
            .ReturnsAsync((new List<BriefingEntry>(), 0));

        var query = new GetBriefingHistoryQuery(userId.ToString(), 2, 5, dateFrom, dateTo);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        _briefingRepoMock.Verify(
            r => r.GetByUserAsync(userId.ToString(), 2, 5, dateFrom, dateTo, default),
            Times.Once);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenWeatherJsonPresent_ShouldExtractWeatherEmoji()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var briefings = new List<BriefingEntry>
        {
            CreateBriefing(userId, today, weatherEmoji: "🌧️"),
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 1, 10, null, null, default))
            .ReturnsAsync((briefings, 1));

        var query = new GetBriefingHistoryQuery(userId.ToString(), 1, 10);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.Items.First().WeatherEmoji.Should().Be("🌧️");
    }

    [Fact]
    public async Task Handle_WhenWeatherJsonNull_ShouldReturnNullWeatherEmoji()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var briefings = new List<BriefingEntry>
        {
            CreateBriefing(userId, today, weatherEmoji: null),
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 1, 10, null, null, default))
            .ReturnsAsync((briefings, 1));

        var query = new GetBriefingHistoryQuery(userId.ToString(), 1, 10);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.Items.First().WeatherEmoji.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldIncludeGreetingFromAISummary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var briefing = CreateBriefing(userId, today);
        briefing.AISummary = "Bom dia, Aurel! Hoje é um ótimo dia.";

        _briefingRepoMock
            .Setup(r => r.GetByUserAsync(
                userId.ToString(), 1, 10, null, null, default))
            .ReturnsAsync((new List<BriefingEntry> { briefing }, 1));

        var query = new GetBriefingHistoryQuery(userId.ToString(), 1, 10);

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        result.Items.First().Greeting.Should().Be("Bom dia, Aurel! Hoje é um ótimo dia.");
    }
}