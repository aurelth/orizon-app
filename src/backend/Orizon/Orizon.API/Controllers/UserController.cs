using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.API.Requests.Users;
using Orizon.Application.UseCases.Users.Commands.CompleteOnboarding;
using Orizon.Application.UseCases.Users.Commands.UpdateUserProfile;
using Orizon.Application.UseCases.Users.Queries.GetUserProfile;
using System.Security.Claims;

namespace Orizon.API.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetUserProfileQuery(userId), ct);

        if (result is null)
            return NotFound(new { message = "Perfil não encontrado." });

        return Ok(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        await _mediator.Send(new UpdateUserProfileCommand(
            userId,
            request.DisplayName,
            request.ProfilePictureUrl,
            request.ThemePreference), ct);

        return NoContent();
    }

    [HttpPost("onboarding/complete")]
    public async Task<IActionResult> CompleteOnboarding(CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        await _mediator.Send(new CompleteOnboardingCommand(userId), ct);

        return NoContent();
    }
}