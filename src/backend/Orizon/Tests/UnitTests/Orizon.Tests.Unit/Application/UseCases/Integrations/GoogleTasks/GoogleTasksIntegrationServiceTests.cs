using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Services.External;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.GoogleTasks;

public class GoogleTasksIntegrationServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ILogger<GoogleTasksIntegrationService>> _loggerMock = new();
    private readonly GoogleTasksIntegrationService _service;

    public GoogleTasksIntegrationServiceTests()
    {
        _service = new GoogleTasksIntegrationService(
            _userRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetTodayTasksAsync_WhenInvalidUserId_ShouldReturnEmpty()
    {
        var result = await _service.GetTodayTasksAsync("invalid-guid");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTodayTasksAsync_WhenUserNotFound_ShouldReturnEmpty()
    {
        var userId = Guid.NewGuid();
        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((AppUser?)null);

        var result = await _service.GetTodayTasksAsync(userId.ToString());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTodayTasksAsync_WhenUserHasNoGoogleToken_ShouldReturnEmpty()
    {
        var userId = Guid.NewGuid();
        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(new AppUser
            {
                Id = userId,
                GoogleAccessToken = null,
            });

        var result = await _service.GetTodayTasksAsync(userId.ToString());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTodayTasksWithTokenAsync_WhenApiThrows_ShouldReturnEmpty()
    {
        // token inválido faz a API do Google retornar erro — o serviço deve retornar vazio
        var result = await _service.GetTodayTasksWithTokenAsync("invalid-token-that-will-fail");

        result.Should().BeEmpty();
    }

    [Fact]
    public void CalculateIsOverdue_WhenDueDateIsToday_ShouldReturnFalse()
    {
        var nowBrasilia = DateTime.Now.Date;
        var dueUtcMidnight = nowBrasilia.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");

        var result = GoogleTasksIntegrationService.CalculateIsOverdue(
            dueUtcMidnight, nowBrasilia);

        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateIsOverdue_WhenDueDateIsYesterday_ShouldReturnTrue()
    {
        var nowBrasilia = DateTime.Now.Date;
        var yesterdayUtc = nowBrasilia.AddDays(-1).ToUniversalTime()
            .ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");

        var result = GoogleTasksIntegrationService.CalculateIsOverdue(
            yesterdayUtc, nowBrasilia);

        result.Should().BeTrue();
    }

    [Fact]
    public void CalculateIsOverdue_WhenDueDateIsTomorrow_ShouldReturnFalse()
    {
        var nowBrasilia = DateTime.Now.Date;
        var tomorrowUtc = nowBrasilia.AddDays(1).ToUniversalTime()
            .ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");

        var result = GoogleTasksIntegrationService.CalculateIsOverdue(
            tomorrowUtc, nowBrasilia);

        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateIsOverdue_WhenGoogleTasksDueFormat_ShouldReturnFalse()
    {
        // formato exato que a Google Tasks API retorna para tasks de hoje
        var nowBrasilia = DateTime.UtcNow.Date;
        var googleFormat = $"{nowBrasilia:yyyy-MM-dd}T00:00:00.000Z";

        var result = GoogleTasksIntegrationService.CalculateIsOverdue(
            googleFormat, nowBrasilia);

        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateIsOverdue_WhenInvalidDate_ShouldReturnFalse()
    {
        var result = GoogleTasksIntegrationService.CalculateIsOverdue(
            "invalid-date", DateTime.Now);

        result.Should().BeFalse();
    }
}