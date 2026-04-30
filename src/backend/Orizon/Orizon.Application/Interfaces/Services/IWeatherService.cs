using Orizon.Application.DTOs.Weather;

namespace Orizon.Application.Interfaces.Services;

public interface IWeatherService
{    
    Task<WeatherDto> GetWeatherAsync(
        double latitude,
        double longitude,
        string timezone,
        CancellationToken cancellationToken = default);
}