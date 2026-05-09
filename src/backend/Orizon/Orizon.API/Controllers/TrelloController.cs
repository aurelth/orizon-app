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

    [HttpPost("connect")]
    public async Task<IActionResult> Connect(
        [FromBody] ConnectTrelloRequest request,
        CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        await _mediator.Send(
            new ConnectTrelloCommand(userId, request.ApiKey, request.Token), ct);

        return Ok();
    }

    [HttpGet("boards")]
    public async Task<IActionResult> GetBoards(
        [FromQuery] string? apiKey,
        [FromQuery] string? token,
        CancellationToken ct = default)
    {
        // usa credenciais da query ou busca do banco via comando
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(token))
            return BadRequest(new { message = "apiKey e token são obrigatórios." });

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

public record ConnectTrelloRequest(string ApiKey, string Token);