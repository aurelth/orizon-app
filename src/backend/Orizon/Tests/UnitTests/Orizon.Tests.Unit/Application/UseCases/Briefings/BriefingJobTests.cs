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

    // Token válido por padrão — expira daqui 1 hora
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
        // Arrange
        _userRepoMock
            .Setup(r => r.GetActiveUsersAsync(default))
            .ReturnsAsync(new List<AppUser>());

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        _briefingRepoMock.Verify(
            r => r.AddAsync(It.IsAny<BriefingEntry>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldCreateBriefingWithPendingStatus()
    {
        // Arrange
        SetupDefaultMocks();

        BriefingEntry? capturedBriefing = null;
        _briefingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<BriefingEntry>(), default))
            .Callback<BriefingEntry, CancellationToken>(
                (b, _) => capturedBriefing = b)
            .Returns(Task.CompletedTask);

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        capturedBriefing.Should().NotBeNull();
        capturedBriefing!.UserId.Should().Be(_testUser.Id);
        capturedBriefing.Date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPipelineSucceeds_ShouldUpdateBriefingWithGeneratedStatus()
    {
        // Arrange
        SetupDefaultMocks();

        BriefingEntry? updatedBriefing = null;
        _briefingRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<BriefingEntry>(), default))
            .Callback<BriefingEntry, CancellationToken>(
                (b, _) => updatedBriefing = b)
            .Returns(Task.CompletedTask);

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        updatedBriefing.Should().NotBeNull();
        updatedBriefing!.Status.Should().Be(BriefingStatus.Generated);
        updatedBriefing.AISummary.Should().Be(_aiSummary.Greeting);
        updatedBriefing.GeneratedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenWeatherServiceFails_ShouldUpdateBriefingWithFailedStatus()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetActiveUsersAsync(default))
            .ReturnsAsync(new List<AppUser> { _testUser });

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                _testUser.Id.ToString(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync((BriefingEntry?)null);

        _briefingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<BriefingEntry>(), default))
            .Returns(Task.CompletedTask);

        _gmailMock
            .Setup(s => s.GetRecentEmailsWithTokenAsync(
                _testUser.GoogleAccessToken!, It.IsAny<int>(), default))
            .ReturnsAsync(new List<EmailSummaryDto>());

        _calendarMock
            .Setup(s => s.GetTodayEventsWithTokenAsync(
                _testUser.GoogleAccessToken!, default))
            .ReturnsAsync(new List<CalendarEventDto>());

        _weatherMock
            .Setup(s => s.GetWeatherAsync(
                _testUser.Latitude, _testUser.Longitude, _testUser.Timezone, default))
            .ThrowsAsync(new Exception("Weather API indisponível"));

        BriefingEntry? updatedBriefing = null;
        _briefingRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<BriefingEntry>(), default))
            .Callback<BriefingEntry, CancellationToken>(
                (b, _) => updatedBriefing = b)
            .Returns(Task.CompletedTask);

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        updatedBriefing.Should().NotBeNull();
        updatedBriefing!.Status.Should().Be(BriefingStatus.Failed);
        updatedBriefing.ErrorMessage.Should().Be("Weather API indisponível");
    }

    [Fact]
    public async Task ExecuteAsync_WhenTrelloDisabled_ShouldNotCallTrelloService()
    {
        // Arrange
        _testUser.TrelloEnabled = false;
        SetupDefaultMocks();

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        _trelloMock.Verify(
            s => s.GetActiveTasksAsync(It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTrelloEnabled_ShouldCallTrelloService()
    {
        // Arrange
        _testUser.TrelloEnabled = true;
        SetupDefaultMocks();

        _trelloMock
            .Setup(s => s.GetActiveTasksAsync(_testUser.Id.ToString(), default))
            .ReturnsAsync(new List<TrelloTaskDto>());

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        _trelloMock.Verify(
            s => s.GetActiveTasksAsync(_testUser.Id.ToString(), default),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsTraveling_ShouldUseTravelCoordinates()
    {
        // Arrange
        _testUser.IsTraveling = true;
        _testUser.TravelLatitude = 38.7169;
        _testUser.TravelLongitude = -9.1395;

        SetupDefaultMocks();

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        _weatherMock.Verify(
            s => s.GetWeatherAsync(38.7169, -9.1395, _testUser.Timezone, default),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPipelineSucceeds_ShouldSendEmail()
    {
        // Arrange
        SetupDefaultMocks();

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        _emailMock.Verify(
            s => s.SendBriefingEmailAsync(
                _testUser.Email,
                _testUser.DisplayName,
                It.IsAny<BriefingResultDto>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenExpired_ShouldRefreshAndUseNewToken()
    {
        // Arrange
        _testUser.GoogleTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1); // expirado
        var newToken = "refreshed-access-token";

        _googleOAuthMock
            .Setup(s => s.RefreshAccessTokenAsync(_testUser.GoogleRefreshToken!, default))
            .ReturnsAsync(new GoogleTokensDto(newToken, _testUser.GoogleRefreshToken!, DateTime.UtcNow.AddHours(1)));

        _userRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), default))
            .Returns(Task.CompletedTask);

        SetupDefaultMocks(accessToken: newToken);

        // Act
        await _job.ExecuteAsync(default);

        // Assert
        _googleOAuthMock.Verify(
            s => s.RefreshAccessTokenAsync(_testUser.GoogleRefreshToken!, default),
            Times.Once);

        _gmailMock.Verify(
            s => s.GetRecentEmailsWithTokenAsync(newToken, It.IsAny<int>(), default),
            Times.Once);
    }

    private void SetupDefaultMocks(string? accessToken = null)
    {
        var token = accessToken ?? _testUser.GoogleAccessToken!;

        _userRepoMock
            .Setup(r => r.GetActiveUsersAsync(default))
            .ReturnsAsync(new List<AppUser> { _testUser });

        _briefingRepoMock
            .Setup(r => r.GetByUserAndDateAsync(
                _testUser.Id.ToString(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync((BriefingEntry?)null);

        _briefingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<BriefingEntry>(), default))
            .Returns(Task.CompletedTask);

        _briefingRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<BriefingEntry>(), default))
            .Returns(Task.CompletedTask);

        _gmailMock
            .Setup(s => s.GetRecentEmailsWithTokenAsync(token, It.IsAny<int>(), default))
            .ReturnsAsync(new List<EmailSummaryDto>());

        _calendarMock
            .Setup(s => s.GetTodayEventsWithTokenAsync(token, default))
            .ReturnsAsync(new List<CalendarEventDto>());

        _weatherMock
            .Setup(s => s.GetWeatherAsync(
                It.IsAny<double>(), It.IsAny<double>(), _testUser.Timezone, default))
            .ReturnsAsync(_weather);

        _claudeMock
            .Setup(s => s.GenerateDailySummaryAsync(
                It.IsAny<IEnumerable<EmailSummaryDto>>(),
                It.IsAny<IEnumerable<CalendarEventDto>>(),
                It.IsAny<IEnumerable<TrelloTaskDto>?>(),
                It.IsAny<WeatherDto>(),
                _testUser.DisplayName,
                default))
            .ReturnsAsync(_aiSummary);

        _emailMock
            .Setup(s => s.SendBriefingEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<BriefingResultDto>(),
                default))
            .Returns(Task.CompletedTask);
    }
}