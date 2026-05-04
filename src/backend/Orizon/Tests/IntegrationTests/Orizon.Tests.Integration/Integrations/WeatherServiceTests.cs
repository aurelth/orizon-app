using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Orizon.Infrastructure.Services.External;
using Xunit;

namespace Orizon.Tests.Integration.Integrations;

public class WeatherServiceTests
{
    private readonly WeatherService _service;

    public WeatherServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Weather:BaseUrl", "https://api.open-meteo.com/v1" }
            })
            .Build();

        var httpClient = new HttpClient();
        _service = new WeatherService(
            httpClient,
            configuration,
            NullLogger<WeatherService>.Instance);
    }

    [Fact]
    public async Task GetWeatherAsync_WhenValidCoordinates_ShouldReturnWeatherData()
    {
        // Coordenadas de Blumenau, SC
        var result = await _service.GetWeatherAsync(
            -26.9195,
            -49.0661,
            "America/Sao_Paulo");

        result.Should().NotBeNull();
        result.CurrentTemperature.Should().BeInRange(-20, 50);
        result.MinTemperature.Should().BeLessThanOrEqualTo(result.MaxTemperature);
        result.Description.Should().NotBeNullOrEmpty();
        result.WeatherEmoji.Should().NotBeNullOrEmpty();
        result.WindSpeed.Should().BeGreaterThanOrEqualTo(0);
        result.Humidity.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetWeatherAsync_WhenValidCoordinates_ShouldReturnHourlyPrecipitation()
    {
        var result = await _service.GetWeatherAsync(
            -26.9195,
            -49.0661,
            "America/Sao_Paulo");

        result.HourlyPrecipitation.Should().NotBeNull();
        result.HourlyPrecipitation.Keys.Should().AllSatisfy(h =>
            h.Should().BeInRange(0, 23));
    }

    [Fact]
    public async Task GetWeatherAsync_WhenInvalidCoordinates_ShouldThrowException()
    {
        var act = async () => await _service.GetWeatherAsync(
            999, 999, "America/Sao_Paulo");

        await act.Should().ThrowAsync<Exception>();
    }
}