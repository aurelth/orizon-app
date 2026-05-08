using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.UseCases.Integrations.Google.Command;
using Orizon.Application.UseCases.Integrations.Google.Query;
using Orizon.Infrastructure.Repositories;
using System.Security.Claims;
using System.Text;

namespace Orizon.API.Controllers;

[ApiController]
[Route("google")]
[Authorize]
public class GoogleController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;

    public GoogleController(IMediator mediator, IUserRepository userRepository)
    {
        _mediator = mediator;
        _userRepository = userRepository;
    }

    [HttpGet("auth-url")]
    public async Task<IActionResult> GetAuthUrl(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";        
        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(userId));

        var url = await _mediator.Send(
            new GetGoogleAuthUrlQuery(userId, state), ct);

        return Ok(new { url });
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct = default)
    {        
        var userId = string.Empty;
        try
        {
            userId = Encoding.UTF8.GetString(Convert.FromBase64String(state));
        }
        catch
        {
            return Redirect("http://localhost:4200/settings/integrations?google=error");
        }

        await _mediator.Send(new ExchangeGoogleCodeCommand(userId, code), ct);

        return Redirect("http://localhost:4200/settings/integrations?google=success");
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId), ct);

        var connected = user?.GoogleAccessToken != null;
        return Ok(new { connected });
    }
}