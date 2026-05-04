using FluentAssertions;
using Moq;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Integrations.Weather.Query;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.Weather;

public class GetWeatherQueryHandlerTests
{
    private readonly Mock<IWeatherService> _weatherServiceMock;
    private readonly GetWeatherQueryHandler _handler;

    public GetWeatherQueryHandlerTests()
    {
        _weatherServiceMock = new Mock<IWeatherService>();
        _handler = new GetWeatherQueryHandler(_weatherServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ShouldCallWeatherService()
    {
        var query = new GetWeatherQuery(-27.59, -48.55, "America/Sao_Paulo");
        var expectedWeather = new WeatherDto
        {
            CurrentTemperature = 25,
            MinTemperature = 18,
            MaxTemperature = 28,
            Description = "Céu limpo",
            WeatherEmoji = "☀️",
            WindSpeed = 10,
            Humidity = 70,
        };

        _weatherServiceMock
            .Setup(s => s.GetWeatherAsync(
                query.Latitude,
                query.Longitude,
                query.Timezone,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedWeather);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeEquivalentTo(expectedWeather);
        _weatherServiceMock.Verify(
            s => s.GetWeatherAsync(
                query.Latitude,
                query.Longitude,
                query.Timezone,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_ShouldPropagateException()
    {
        var query = new GetWeatherQuery(-27.59, -48.55, "America/Sao_Paulo");

        _weatherServiceMock
            .Setup(s => s.GetWeatherAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Serviço indisponível"));

        var act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}