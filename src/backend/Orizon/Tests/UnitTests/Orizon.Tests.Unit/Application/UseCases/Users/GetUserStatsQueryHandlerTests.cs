using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Users.Queries.GetUserStats;

namespace Orizon.Tests.Unit.Application.UseCases.Users;

public class GetUserStatsQueryHandlerTests
{
    private readonly Mock<IBriefingRepository> _briefingRepoMock = new();
    private readonly GetUserStatsQueryHandler _handler;

    public GetUserStatsQueryHandlerTests()
    {
        _handler = new GetUserStatsQueryHandler(_briefingRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoBriefings_ShouldReturnZeroStats()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        _briefingRepoMock
            .Setup(r => r.GetGeneratedDatesByUserAsync(userId, default))
            .ReturnsAsync(new List<DateOnly>());

        // Act
        var result = await _handler.Handle(new GetUserStatsQuery(userId), default);

        // Assert
        result.TotalGenerated.Should().Be(0);
        result.CurrentStreak.Should().Be(0);
        result.MaxStreak.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenSingleBriefingToday_ShouldReturnStreakOne()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById(
            "E. South America Standard Time");
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));

        _briefingRepoMock
            .Setup(r => r.GetGeneratedDatesByUserAsync(userId, default))
            .ReturnsAsync(new List<DateOnly> { today });

        // Act
        var result = await _handler.Handle(new GetUserStatsQuery(userId), default);

        // Assert
        result.TotalGenerated.Should().Be(1);
        result.CurrentStreak.Should().Be(1);
        result.MaxStreak.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenConsecutiveDays_ShouldReturnCorrectStreak()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById(
            "E. South America Standard Time");
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));
        
        var dates = Enumerable.Range(0, 5)
            .Select(i => today.AddDays(-i))
            .ToList();

        _briefingRepoMock
            .Setup(r => r.GetGeneratedDatesByUserAsync(userId, default))
            .ReturnsAsync(dates);

        // Act
        var result = await _handler.Handle(new GetUserStatsQuery(userId), default);

        // Assert
        result.TotalGenerated.Should().Be(5);
        result.CurrentStreak.Should().Be(5);
        result.MaxStreak.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenStreakBroken_ShouldReturnZeroCurrentStreak()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById(
            "E. South America Standard Time");
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));
        
        var dates = Enumerable.Range(2, 4)
            .Select(i => today.AddDays(-i))
            .ToList();

        _briefingRepoMock
            .Setup(r => r.GetGeneratedDatesByUserAsync(userId, default))
            .ReturnsAsync(dates);

        // Act
        var result = await _handler.Handle(new GetUserStatsQuery(userId), default);

        // Assert
        result.TotalGenerated.Should().Be(4);
        result.CurrentStreak.Should().Be(0);
        result.MaxStreak.Should().Be(4);
    }

    [Fact]
    public async Task Handle_WhenMaxStreakHigherThanCurrent_ShouldReturnBothCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById(
            "E. South America Standard Time");
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));
        
        var oldStreak = Enumerable.Range(24, 7)
            .Select(i => today.AddDays(-i))
            .ToList();
        
        var currentStreak = Enumerable.Range(0, 2)
            .Select(i => today.AddDays(-i))
            .ToList();

        var dates = oldStreak.Concat(currentStreak).ToList();

        _briefingRepoMock
            .Setup(r => r.GetGeneratedDatesByUserAsync(userId, default))
            .ReturnsAsync(dates);

        // Act
        var result = await _handler.Handle(new GetUserStatsQuery(userId), default);

        // Assert
        result.TotalGenerated.Should().Be(9);
        result.CurrentStreak.Should().Be(2);
        result.MaxStreak.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WhenOnlyYesterdayHasBriefing_ShouldReturnZeroCurrentStreak()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById(
            "E. South America Standard Time");
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));

        _briefingRepoMock
            .Setup(r => r.GetGeneratedDatesByUserAsync(userId, default))
            .ReturnsAsync(new List<DateOnly> { today.AddDays(-1) });

        // Act
        var result = await _handler.Handle(new GetUserStatsQuery(userId), default);

        // Assert        
        result.CurrentStreak.Should().Be(0);
        result.MaxStreak.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        _briefingRepoMock
            .Setup(r => r.GetGeneratedDatesByUserAsync(userId, default))
            .ReturnsAsync(new List<DateOnly>());

        // Act
        await _handler.Handle(new GetUserStatsQuery(userId), default);

        // Assert
        _briefingRepoMock.Verify(
            r => r.GetGeneratedDatesByUserAsync(userId, default),
            Times.Once);
    }
}