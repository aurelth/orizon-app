using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Application.UseCases.Integrations.Weather.Query;

namespace Orizon.API.Controllers;

[ApiController]
[Route("weather")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IMediator _mediator;

    public WeatherController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetWeather(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] string timezone = "America/Sao_Paulo",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetWeatherQuery(latitude, longitude, timezone), ct);

        return Ok(result);
    }
}