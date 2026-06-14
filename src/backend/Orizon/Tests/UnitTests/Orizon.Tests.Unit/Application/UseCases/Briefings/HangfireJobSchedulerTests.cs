using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Orizon.Infrastructure.Services;
using System.Net;

namespace Orizon.Tests.Unit.Briefings;

public class HangfireJobSchedulerTests
{
    private readonly Mock<IConfiguration> _configMock = new();

    private HangfireJobScheduler CreateScheduler(HttpClient httpClient)
    {
        _configMock.Setup(c => c["Worker:InternalUrl"])
            .Returns("http://worker:5011");
        return new HangfireJobScheduler(httpClient, _configMock.Object);
    }

    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode = HttpStatusCode.Accepted)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));

        return new HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task EnqueueBriefingGenerationAsync_ShouldCallCorrectEndpointWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        var httpClient = new HttpClient(handlerMock.Object);
        var scheduler = CreateScheduler(httpClient);

        // Act
        await scheduler.EnqueueBriefingGenerationAsync(userId);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString()
            .Should().Be($"http://worker:5011/internal/briefing/trigger/{userId}");
        capturedRequest.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task EnqueueBriefingGenerationAsync_ShouldReturnTriggered()
    {
        // Arrange
        var httpClient = CreateMockHttpClient();
        var scheduler = CreateScheduler(httpClient);

        // Act
        var result = await scheduler
            .EnqueueBriefingGenerationAsync(Guid.NewGuid().ToString());

        // Assert
        result.Should().Be("triggered");
    }

    [Fact]
    public async Task EnqueueBriefingGenerationAsync_WhenWorkerUrlNotConfigured_ShouldUseFallback()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Worker:InternalUrl"]).Returns((string?)null);

        var httpClient = new HttpClient(handlerMock.Object);
        var scheduler = new HangfireJobScheduler(httpClient, configMock.Object);

        // Act
        await scheduler.EnqueueBriefingGenerationAsync(userId);

        // Assert
        capturedRequest!.RequestUri!.ToString()
            .Should().Be($"http://localhost:5011/internal/briefing/trigger/{userId}");
    }
}