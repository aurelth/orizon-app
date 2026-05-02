using FluentAssertions;
using Orizon.Application.DTOs.Weather;

namespace Orizon.Tests.Unit.Application.DTOs;

public class WeatherDtoTests
{
    [Fact]
    public void WillRain_WhenRainStartHourIsNull_ShouldReturnFalse()
    {
        // Arrange
        var weather = new WeatherDto
        {
            RainStartHour = null
        };

        // Assert
        weather.WillRain.Should().BeFalse();
    }

    [Fact]
    public void WillRain_WhenRainStartHourIsSet_ShouldReturnTrue()
    {
        // Arrange
        var weather = new WeatherDto
        {
            RainStartHour = 14,
            RainEndHour = 17
        };

        // Assert
        weather.WillRain.Should().BeTrue();
    }

    [Fact]
    public void HourlyPrecipitation_WhenCreated_ShouldBeEmpty()
    {
        // Arrange & Act
        var weather = new WeatherDto();

        // Assert
        weather.HourlyPrecipitation.Should().NotBeNull();
        weather.HourlyPrecipitation.Should().BeEmpty();
    }
}