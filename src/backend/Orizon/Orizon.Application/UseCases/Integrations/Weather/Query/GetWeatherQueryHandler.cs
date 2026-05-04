using MediatR;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Integrations.Weather.Query;

public class GetWeatherQueryHandler : IRequestHandler<GetWeatherQuery, WeatherDto>
{
    private readonly IWeatherService _weatherService;

    public GetWeatherQueryHandler(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task<WeatherDto> Handle(
        GetWeatherQuery request,
        CancellationToken cancellationToken)
    {
        return await _weatherService.GetWeatherAsync(
            request.Latitude,
            request.Longitude,
            request.Timezone,
            cancellationToken);
    }
}