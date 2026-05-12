using FluentAssertions;
using Moq;
using Orizon.Application.DTOs.Calendar;
using Orizon.Application.DTOs.Email;
using Orizon.Application.DTOs.Tasks;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Briefings.Queries.GetBriefingByDate;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using System.Text.Json;

namespace Orizon.Tests.Unit.Application.UseCases.Briefings;

public class GetBriefingByDateQueryHandlerTests
{
    private readonly Mock<IBriefingRepository> _briefingRepoMock = new();
    private readonly GetBriefingByDateQueryHandler _handler;

    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly WeatherDto _weather = new()
    {
        CurrentTemperature = 22,
        MinTemperature = 18,
        MaxTemperature = 26,
        Description = "Parcialmente nublado",
        WeatherEmoji = "⛅",
        LocationName = "Blumenau"
    };

    public GetBriefingByDateQueryHandlerTests()
    {
        _handler = new GetBriefingByDateQueryHandler(_briefingRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBriefingExists_ShouldReturnBriefingResultDto()
    {
        var userId = Guid.NewGuid();
        var briefingId = Guid.NewGuid();

        var briefing = new BriefingEntry
        {
            Id = briefingId,
            UserId = userId,
            Date = _today,
            Status = BriefingStatus.Generated,
            GeneratedAt = DateTime.UtcNow,
            WeatherJson = JsonSerializer.Serialize(_weather),
            EmailSummaryJson = JsonSerializer.Serialize(new List<EmailSummaryDto>()),
            CalendarEventsJson = JsonSerializer.Serialize(new List<CalendarEventDto>()),
            AISummary = "Bom dia, Aurel!",
            AISuggestions = "Leve guarda-chuva.",
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                userId.ToString(), _today, default))
            .ReturnsAsync(briefing);

        var query = new GetBriefingByDateQuery(
            userId.ToString(), "Aurel", _today);

        var result = await _handler.Handle(query, default);

        result.Should().NotBeNull();
        result!.BriefingId.Should().Be(briefingId);
        result.UserName.Should().Be("Aurel");
        result.AISummary.Greeting.Should().Be("Bom dia, Aurel!");
        result.AISummary.Suggestions.Should().Be("Leve guarda-chuva.");
        result.Weather.Should().NotBeNull();
        result.Weather.CurrentTemperature.Should().Be(22);
    }

    [Fact]
    public async Task Handle_WhenBriefingNotFound_ShouldReturnNull()
    {
        var userId = Guid.NewGuid();

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                userId.ToString(), _today, default))
            .ReturnsAsync((BriefingEntry?)null);

        var query = new GetBriefingByDateQuery(
            userId.ToString(), "Aurel", _today);

        var result = await _handler.Handle(query, default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTrelloTasksExist_ShouldDeserializeTrelloTasks()
    {
        var userId = Guid.NewGuid();
        var tasks = new List<TrelloTaskDto>
        {
            new() { CardId = "card-1", Title = "Implementar Fase 7", BoardName = "Orizon" }
        };

        var briefing = new BriefingEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = _today,
            Status = BriefingStatus.Generated,
            GeneratedAt = DateTime.UtcNow,
            WeatherJson = JsonSerializer.Serialize(_weather),
            EmailSummaryJson = JsonSerializer.Serialize(new List<EmailSummaryDto>()),
            CalendarEventsJson = JsonSerializer.Serialize(new List<CalendarEventDto>()),
            TrelloTasksJson = JsonSerializer.Serialize(tasks),
            AISummary = "Bom dia!",
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                userId.ToString(), _today, default))
            .ReturnsAsync(briefing);

        var result = await _handler.Handle(
            new GetBriefingByDateQuery(userId.ToString(), "Aurel", _today), default);

        result!.TrelloTasks.Should().NotBeNull();
        result.TrelloTasks!.Should().HaveCount(1);
        result.TrelloTasks!.First().Title.Should().Be("Implementar Fase 7");
    }

    [Fact]
    public async Task Handle_WhenTrelloTasksNull_ShouldReturnNullTrelloTasks()
    {
        var userId = Guid.NewGuid();

        var briefing = new BriefingEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = _today,
            Status = BriefingStatus.Generated,
            GeneratedAt = DateTime.UtcNow,
            WeatherJson = JsonSerializer.Serialize(_weather),
            EmailSummaryJson = JsonSerializer.Serialize(new List<EmailSummaryDto>()),
            CalendarEventsJson = JsonSerializer.Serialize(new List<CalendarEventDto>()),
            TrelloTasksJson = null,
            AISummary = "Bom dia!",
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                userId.ToString(), _today, default))
            .ReturnsAsync(briefing);

        var result = await _handler.Handle(
            new GetBriefingByDateQuery(userId.ToString(), "Aurel", _today), default);

        result!.TrelloTasks.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenGoogleTasksExist_ShouldDeserializeGoogleTasks()
    {
        var userId = Guid.NewGuid();
        var googleTasks = new List<GoogleTaskDto>
        {
            new()
            {
                Id = "task-1",
                Title = "Revisar PR do Orizon",
                TaskListName = "Minha lista",
                DueDate = DateTime.Today,
                IsOverdue = false,
            }
        };

        var briefing = new BriefingEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = _today,
            Status = BriefingStatus.Generated,
            GeneratedAt = DateTime.UtcNow,
            WeatherJson = JsonSerializer.Serialize(_weather),
            EmailSummaryJson = JsonSerializer.Serialize(new List<EmailSummaryDto>()),
            CalendarEventsJson = JsonSerializer.Serialize(new List<CalendarEventDto>()),
            GoogleTasksJson = JsonSerializer.Serialize(googleTasks),
            AISummary = "Bom dia!",
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                userId.ToString(), _today, default))
            .ReturnsAsync(briefing);

        var result = await _handler.Handle(
            new GetBriefingByDateQuery(userId.ToString(), "Aurel", _today), default);

        result!.GoogleTasks.Should().NotBeNull();
        result.GoogleTasks!.Should().HaveCount(1);
        result.GoogleTasks!.First().Title.Should().Be("Revisar PR do Orizon");
    }

    [Fact]
    public async Task Handle_WhenGoogleTasksNull_ShouldReturnNullGoogleTasks()
    {
        var userId = Guid.NewGuid();

        var briefing = new BriefingEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = _today,
            Status = BriefingStatus.Generated,
            GeneratedAt = DateTime.UtcNow,
            WeatherJson = JsonSerializer.Serialize(_weather),
            EmailSummaryJson = JsonSerializer.Serialize(new List<EmailSummaryDto>()),
            CalendarEventsJson = JsonSerializer.Serialize(new List<CalendarEventDto>()),
            GoogleTasksJson = null,
            AISummary = "Bom dia!",
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                userId.ToString(), _today, default))
            .ReturnsAsync(briefing);

        var result = await _handler.Handle(
            new GetBriefingByDateQuery(userId.ToString(), "Aurel", _today), default);

        result!.GoogleTasks.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenBirthdayEventExists_ShouldDeserializeWithIsBirthdayTrue()
    {
        var userId = Guid.NewGuid();
        var events = new List<CalendarEventDto>
        {
            new()
            {
                Title = "Aniversário de João",
                StartTime = DateTime.Today,
                EndTime = DateTime.Today.AddDays(1),
                IsBirthday = true,
                IsAllDay = true,
            }
        };

        var briefing = new BriefingEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = _today,
            Status = BriefingStatus.Generated,
            GeneratedAt = DateTime.UtcNow,
            WeatherJson = JsonSerializer.Serialize(_weather),
            EmailSummaryJson = JsonSerializer.Serialize(new List<EmailSummaryDto>()),
            CalendarEventsJson = JsonSerializer.Serialize(events),
            AISummary = "Bom dia!",
        };

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                userId.ToString(), _today, default))
            .ReturnsAsync(briefing);

        var result = await _handler.Handle(
            new GetBriefingByDateQuery(userId.ToString(), "Aurel", _today), default);

        result!.CalendarEvents.Should().HaveCount(1);
        result.CalendarEvents.First().IsBirthday.Should().BeTrue();
        result.CalendarEvents.First().IsAllDay.Should().BeTrue();
    }
}