using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Application.UseCases.Integrations.Trello.Command;
using Orizon.Application.UseCases.Integrations.Trello.Query;
using System.Security.Claims;

namespace Orizon.API.Controllers;

[ApiController]
[Route("trello")]
[Authorize]
public class TrelloController : ControllerBase
{
    private readonly IMediator _mediator;

    public TrelloController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("boards")]
    public async Task<IActionResult> GetBoards(
        [FromQuery] string apiKey,
        [FromQuery] string token,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetBoardsQuery(apiKey, token), ct);

        return Ok(result);
    }

    [HttpPost("boards/config")]
    public async Task<IActionResult> SaveBoardConfig(
        [FromBody] SaveBoardConfigCommand command,
        CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var cmd = command with { UserId = userId };
        await _mediator.Send(cmd, ct);

        return Ok();
    }
}