using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Application.UseCases.Integrations.Google.Command;
using Orizon.Application.UseCases.Integrations.Google.Query;
using System.Security.Claims;

namespace Orizon.API.Controllers;

[ApiController]
[Route("google")]
[Authorize]
public class GoogleController : ControllerBase
{
    private readonly IMediator _mediator;

    public GoogleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("auth-url")]
    public async Task<IActionResult> GetAuthUrl(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var state = Guid.NewGuid().ToString();

        var url = await _mediator.Send(
            new GetGoogleAuthUrlQuery(userId, state), ct);

        return Ok(new { url });
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromBody] GoogleCallbackRequest request,
        CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        var result = await _mediator.Send(
            new ExchangeGoogleCodeCommand(userId, request.Code), ct);

        return Ok(result);
    }
}

public record GoogleCallbackRequest(string Code, string State);