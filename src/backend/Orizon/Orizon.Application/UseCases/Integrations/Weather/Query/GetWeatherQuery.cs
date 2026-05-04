using MediatR;
using Orizon.Application.DTOs.Weather;

namespace Orizon.Application.UseCases.Integrations.Weather.Query;

public record GetWeatherQuery(
    double Latitude,
    double Longitude,
    string Timezone
) : IRequest<WeatherDto>;