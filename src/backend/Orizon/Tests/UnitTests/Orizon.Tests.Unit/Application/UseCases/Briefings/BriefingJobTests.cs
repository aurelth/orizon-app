using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Orizon.Application.DTOs.Briefing;
using Orizon.Application.DTOs.Calendar;
using Orizon.Application.DTOs.Email;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using Orizon.Worker.Jobs;

namespace Orizon.Tests.Unit.Application.UseCases.Briefings;

public class BriefingJobTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IBriefingRepository> _briefingRepoMock = new();
    private readonly Mock<IGmailService> _gmailMock = new();
    private readonly Mock<ICalendarService> _calendarMock = new();
    private readonly Mock<ITrelloService> _trelloMock = new();
    private readonly Mock<IWeatherService> _weatherMock = new();
    private readonly Mock<IClaudeService> _claudeMock = new();
    private readonly Mock<IEmailNotificationService> _emailMock = new();
    private readonly Mock<IGoogleOAuthService> _googleOAuthMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<BriefingJob>> _loggerMock = new();
    private readonly BriefingJob _job;

    private readonly AppUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "aurel@orizonapp.io",
        DisplayName = "Aurel",
        Latitude = -26.9194,
        Longitude = -49.0661,
        Timezone = "America/Sao_Paulo",
        TrelloEnabled = false,
        GoogleAccessToken = "valid-access-token",
        GoogleRefreshToken = "valid-refresh-token",
        GoogleTokenExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    private readonly WeatherDto _weather = new()
    {
        CurrentTemperature = 22,
        Description = "Ensolarado",
        WeatherEmoji = "☀️",
        LocationName = "Blumenau"
    };

    private readonly BriefingAISummaryDto _aiSummary = new()
    {
        Greeting = "Bom dia, Aurel!",
        WeatherSummary = "Dia ensolarado.",
        Suggestions = "Ótimo dia para trabalhar.",
        ActionChips = new List<string> { "Revisar PRs", "Daily às 10h" }
    };

    public BriefingJobTests()
    {
        _configMock
            .Setup(c => c["SignalR:HubUrl"])
            .Returns("http://localhost:5010/hubs/briefing");

        _job = new BriefingJob(
            _userRepoMock.Object,
            _briefingRepoMock.Object,
            _gmailMock.Object,
            _calendarMock.Object,
            _trelloMock.Object,
            _weatherMock.Object,
            _claudeMock.Object,
            _emailMock.Object,
            _googleOAuthMock.Object,
            _configMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoUsers_ShouldNotProcessAnyBriefing()
    {
        _userRepoMock
            .Setup(r => r.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AppUser>());

        await _job.ExecuteAsync(default);

        _briefingRepoMock.Verify(
            r => r.AddAsync(It.IsAny<BriefingEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldCreateBriefingWithPendingStatus()
    {
        SetupDefaultMocks();

        BriefingEntry? capturedBriefing = null;
        _briefingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<BriefingEntry>(), It.IsAny<CancellationToken>()))
            .Callback<BriefingEntry, CancellationToken>(
                (b, _) => capturedBriefing = b)
            .Returns(Task.CompletedTask);

        await _job.ExecuteAsync(default);

        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var expectedDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));

        capturedBriefing.Should().NotBeNull();
        capturedBriefing!.UserId.Should().Be(_testUser.Id);
        capturedBriefing.Date.Should().Be(expectedDate);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPipelineSucceeds_ShouldUpdateBriefingWithGeneratedStatus()
    {
        SetupDefaultMocks();

        BriefingEntry? updatedBriefing = null;
        _briefingRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<BriefingEntry>(), It.IsAny<CancellationToken>()))
            .Callback<BriefingEntry, CancellationToken>(
                (b, _) => updatedBriefing = b)
            .Returns(Task.CompletedTask);

        await _job.ExecuteAsync(default);

        updatedBriefing.Should().NotBeNull();
        updatedBriefing!.Status.Should().Be(BriefingStatus.Generated);
        updatedBriefing.AISummary.Should().Be(_aiSummary.Greeting);
        updatedBriefing.GeneratedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenWeatherServiceFails_ShouldUpdateBriefingWithFailedStatus()
    {
        _userRepoMock
            .Setup(r => r.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AppUser> { _testUser });

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                _testUser.Id.ToString(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BriefingEntry?)null);

        _briefingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<BriefingEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _gmailMock
            .Setup(s => s.GetRecentEmailsWithTokenAsync(
                _testUser.GoogleAccessToken!, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailSummaryDto>());

        _calendarMock
            .Setup(s => s.GetTodayEventsWithTokenAsync(
                _testUser.GoogleAccessToken!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        _weatherMock
            .Setup(s => s.GetWeatherAsync(
                _testUser.Latitude, _testUser.Longitude, _testUser.Timezone,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Weather API indisponível"));

        BriefingEntry? updatedBriefing = null;
        _briefingRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<BriefingEntry>(), It.IsAny<CancellationToken>()))
            .Callback<BriefingEntry, CancellationToken>(
                (b, _) => updatedBriefing = b)
            .Returns(Task.CompletedTask);

        await _job.ExecuteAsync(default);

        updatedBriefing.Should().NotBeNull();
        updatedBriefing!.Status.Should().Be(BriefingStatus.Failed);
        updatedBriefing.ErrorMessage.Should().Be("Weather API indisponível");
    }

    [Fact]
    public async Task ExecuteAsync_WhenTrelloDisabled_ShouldNotCallTrelloService()
    {
        _testUser.TrelloEnabled = false;
        SetupDefaultMocks();

        await _job.ExecuteAsync(default);

        _trelloMock.Verify(
            s => s.GetActiveTasksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTrelloEnabled_ShouldCallTrelloService()
    {
        _testUser.TrelloEnabled = true;
        SetupDefaultMocks();

        _trelloMock
            .Setup(s => s.GetActiveTasksAsync(
                _testUser.Id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrelloTaskDto>());

        await _job.ExecuteAsync(default);

        _trelloMock.Verify(
            s => s.GetActiveTasksAsync(_testUser.Id.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsTraveling_ShouldUseTravelCoordinates()
    {
        _testUser.IsTraveling = true;
        _testUser.TravelLatitude = 38.7169;
        _testUser.TravelLongitude = -9.1395;

        SetupDefaultMocks();

        await _job.ExecuteAsync(default);

        _weatherMock.Verify(
            s => s.GetWeatherAsync(38.7169, -9.1395, _testUser.Timezone,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPipelineSucceeds_ShouldSendEmail()
    {
        SetupDefaultMocks();

        await _job.ExecuteAsync(default);

        _emailMock.Verify(
            s => s.SendBriefingEmailAsync(
                _testUser.Email,
                _testUser.DisplayName,
                It.IsAny<BriefingResultDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenExpired_ShouldRefreshAndUseNewToken()
    {
        _testUser.GoogleTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        var newToken = "refreshed-access-token";

        _googleOAuthMock
            .Setup(s => s.RefreshAccessTokenAsync(
                _testUser.GoogleRefreshToken!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleTokensDto(
                newToken, _testUser.GoogleRefreshToken!, DateTime.UtcNow.AddHours(1)));

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupDefaultMocks(accessToken: newToken);

        await _job.ExecuteAsync(default);

        _googleOAuthMock.Verify(
            s => s.RefreshAccessTokenAsync(
                _testUser.GoogleRefreshToken!, It.IsAny<CancellationToken>()),
            Times.Once);

        _gmailMock.Verify(
            s => s.GetRecentEmailsWithTokenAsync(
                newToken, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenValid_ShouldNotRefreshToken()
    {
        _testUser.GoogleTokenExpiresAt = DateTime.UtcNow.AddHours(2);
        SetupDefaultMocks();

        await _job.ExecuteAsync(default);

        _googleOAuthMock.Verify(
            s => s.RefreshAccessTokenAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoRefreshToken_ShouldSkipGoogleServices()
    {
        _testUser.GoogleTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        _testUser.GoogleRefreshToken = null;
        SetupDefaultMocks(accessToken: "");

        await _job.ExecuteAsync(default);

        _gmailMock.Verify(
            s => s.GetRecentEmailsWithTokenAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExistingBriefing_ShouldUpdateInsteadOfCreate()
    {
        var existingBriefing = new BriefingEntry
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = BriefingStatus.Generated,
        };

        SetupDefaultMocks();

        // sobrescreve após SetupDefaultMocks para retornar briefing existente
        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                _testUser.Id.ToString(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBriefing);

        await _job.ExecuteAsync(default);

        _briefingRepoMock.Verify(
            r => r.AddAsync(It.IsAny<BriefingEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupDefaultMocks(string? accessToken = null)
    {
        var token = accessToken ?? _testUser.GoogleAccessToken!;

        _userRepoMock
            .Setup(r => r.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AppUser> { _testUser });

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                _testUser.Id.ToString(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BriefingEntry?)null);

        _briefingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<BriefingEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _briefingRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<BriefingEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _gmailMock
            .Setup(s => s.GetRecentEmailsWithTokenAsync(
                token, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailSummaryDto>());

        _calendarMock
            .Setup(s => s.GetTodayEventsWithTokenAsync(
                token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        _weatherMock
            .Setup(s => s.GetWeatherAsync(
                It.IsAny<double>(), It.IsAny<double>(), _testUser.Timezone,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_weather);

        _claudeMock
            .Setup(s => s.GenerateDailySummaryAsync(
                It.IsAny<IEnumerable<EmailSummaryDto>>(),
                It.IsAny<IEnumerable<CalendarEventDto>>(),
                It.IsAny<IEnumerable<TrelloTaskDto>?>(),
                It.IsAny<WeatherDto>(),
                _testUser.DisplayName,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_aiSummary);

        _emailMock
            .Setup(s => s.SendBriefingEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<BriefingResultDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}