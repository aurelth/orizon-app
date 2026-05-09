using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Orizon.API.Hubs;

namespace Orizon.API.Controllers;

[ApiController]
[Route("internal")]
[AllowAnonymous]
public class InternalController : ControllerBase
{
    private readonly IHubContext<BriefingHub> _hubContext;

    public InternalController(IHubContext<BriefingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("briefing-ready")]
    public async Task<IActionResult> BriefingReady(
        [FromBody] BriefingReadyRequest request,
        CancellationToken ct)
    {
        await _hubContext.Clients
            .Group(request.UserId)
            .SendAsync("BriefingReady", cancellationToken: ct);

        return Ok();
    }
}

public record BriefingReadyRequest(string UserId, Guid BriefingId);