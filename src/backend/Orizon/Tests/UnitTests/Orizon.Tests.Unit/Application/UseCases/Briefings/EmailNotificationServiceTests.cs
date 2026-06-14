using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Orizon.Application.DTOs.Briefing;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Services;
using Orizon.Infrastructure.Services.Email;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace Orizon.Tests.Unit.Infrastructure.Services;

public class EmailNotificationServiceTests
{
    private readonly Mock<ISendGridClient> _sendGridMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<EmailNotificationService>> _loggerMock = new();
    private readonly Mock<IOrizonMetrics> _metricsMock = new();
    private readonly EmailNotificationService _service;

    public EmailNotificationServiceTests()
    {
        _configMock.Setup(c => c["Email:FromEmail"]).Returns("noreply@orizonapp.io");
        _configMock.Setup(c => c["Email:FromName"]).Returns("Orizon");
        _configMock.Setup(c => c["App:FrontendUrl"]).Returns("https://orizonapp.io");

        _service = new EmailNotificationService(
            _sendGridMock.Object,
            _configMock.Object,
            _loggerMock.Object,
            _metricsMock.Object);
    }

    private static BriefingResultDto CreateMockBriefing() => new()
    {
        BriefingId = Guid.NewGuid(),
        Date = DateOnly.FromDateTime(DateTime.Today),
        UserName = "Aurel",
        Weather = new WeatherDto
        {
            CurrentTemperature = 22,
            MinTemperature = 15,
            MaxTemperature = 28,
            Description = "Ensolarado",
            WeatherEmoji = "☀️",
        },
        Emails = [],
        CalendarEvents = [],
        AISummary = new BriefingAISummaryDto
        {
            Greeting = "Bom dia!",
            WeatherSummary = "Dia ensolarado.",
            Suggestions = "Ótimo dia.",
            ActionChips = [],
        },
        GeneratedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task SendBriefingEmailAsync_WhenSendGridSucceeds_ShouldNotThrow()
    {
        _sendGridMock
            .Setup(s => s.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(HttpStatusCode.Accepted, null, null));

        var act = async () => await _service.SendBriefingEmailAsync(
            "aurel@orizonapp.io", "Aurel", CreateMockBriefing());

        await act.Should().NotThrowAsync();
        _metricsMock.Verify(m => m.RecordEmailSent(), Times.Once);
    }

    [Fact]
    public async Task SendBriefingEmailAsync_WhenSendGridReturnsUnauthorized_ShouldThrow()
    {
        _sendGridMock
            .Setup(s => s.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(HttpStatusCode.Unauthorized, null, null));

        var act = async () => await _service.SendBriefingEmailAsync(
            "aurel@orizonapp.io", "Aurel", CreateMockBriefing());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unauthorized*");
        _metricsMock.Verify(m => m.RecordEmailFailed(), Times.Once);
        _metricsMock.Verify(m => m.RecordEmailSent(), Times.Never);
    }

    [Fact]
    public async Task SendBriefingEmailAsync_WhenSendGridReturnsForbidden_ShouldThrow()
    {
        _sendGridMock
            .Setup(s => s.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(HttpStatusCode.Forbidden, null, null));

        var act = async () => await _service.SendBriefingEmailAsync(
            "aurel@orizonapp.io", "Aurel", CreateMockBriefing());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _metricsMock.Verify(m => m.RecordEmailFailed(), Times.Once);
    }

    [Fact]
    public async Task SendBriefingEmailAsync_WhenSendGridReturnsInternalServerError_ShouldThrow()
    {
        _sendGridMock
            .Setup(s => s.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(HttpStatusCode.InternalServerError, null, null));

        var act = async () => await _service.SendBriefingEmailAsync(
            "aurel@orizonapp.io", "Aurel", CreateMockBriefing());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WhenSendGridSucceeds_ShouldNotThrow()
    {
        _sendGridMock
            .Setup(s => s.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(HttpStatusCode.Accepted, null, null));

        var act = async () => await _service.SendPasswordResetEmailAsync(
            "aurel@orizonapp.io", "Aurel", "reset-token-123");

        await act.Should().NotThrowAsync();
        _metricsMock.Verify(m => m.RecordEmailSent(), Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WhenSendGridFails_ShouldThrow()
    {
        _sendGridMock
            .Setup(s => s.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(HttpStatusCode.Unauthorized, null, null));

        var act = async () => await _service.SendPasswordResetEmailAsync(
            "aurel@orizonapp.io", "Aurel", "reset-token-123");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _metricsMock.Verify(m => m.RecordEmailFailed(), Times.Once);
    }
}