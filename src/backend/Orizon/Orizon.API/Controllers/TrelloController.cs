using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Application.Interfaces.Repositories;
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
    private readonly IUserRepository _userRepository;

    public TrelloController(IMediator mediator, IUserRepository userRepository)
    {
        _mediator = mediator;
        _userRepository = userRepository;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId), ct);

        var connected = user?.TrelloEnabled == true && user.TrelloApiKey != null;
        return Ok(new { connected });
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
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        // se não foram passados apiKey e token, busca do banco
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(token))
        {
            var result = await _mediator.Send(new GetUserBoardsQuery(userId), ct);
            return Ok(result);
        }

        var resultWithParams = await _mediator.Send(new GetBoardsQuery(apiKey, token), ct);
        return Ok(resultWithParams);
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