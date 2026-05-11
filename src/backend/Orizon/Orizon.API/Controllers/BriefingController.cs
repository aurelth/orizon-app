using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Application.UseCases.Briefings.Commands.GenerateBriefing;
using Orizon.Application.UseCases.Briefings.Queries.GetBriefingByDate;
using Orizon.Application.UseCases.Briefings.Queries.GetBriefingHistory;
using System.Security.Claims;

namespace Orizon.API.Controllers;

[ApiController]
[Route("briefings")]
[Authorize]
public class BriefingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BriefingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";

        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brasiliaZone));

        var result = await _mediator.Send(
            new GetBriefingByDateQuery(userId, userName, date), ct);

        if (result is null)
            return NotFound(new { message = "Briefing não encontrado para hoje." });

        return Ok(result);
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> GetByDate(
        [FromRoute] string date,
        CancellationToken ct = default)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest(new { message = "Data inválida. Use o formato yyyy-MM-dd." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";

        var result = await _mediator.Send(
            new GetBriefingByDateQuery(userId, userName, parsedDate), ct);

        if (result is null)
            return NotFound(new { message = "Briefing não encontrado para esta data." });

        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        var result = await _mediator.Send(
            new GetBriefingHistoryQuery(userId, page, pageSize), ct);

        return Ok(result);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var result = await _mediator.Send(new GenerateBriefingCommand(userId), ct);
        return Accepted(result);
    }
}