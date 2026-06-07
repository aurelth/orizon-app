using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.API.Requests.Users;
using Orizon.Application.UseCases.Users.Commands.ChangePassword;
using Orizon.Application.UseCases.Users.Commands.CompleteOnboarding;
using Orizon.Application.UseCases.Users.Commands.DeleteAccount;
using Orizon.Application.UseCases.Users.Commands.UpdateBriefingPreferences;
using Orizon.Application.UseCases.Users.Commands.UpdateUserProfile;
using Orizon.Application.UseCases.Users.Commands.UploadProfilePicture;
using Orizon.Application.UseCases.Users.Queries.GetUserProfile;
using Orizon.Application.UseCases.Users.Queries.GetUserStats;
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

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetUserStatsQuery(userId.ToString()), ct);

        return Ok(result);
    }

    [HttpPut("briefing-preferences")]
    public async Task<IActionResult> UpdateBriefingPreferences(
        [FromBody] UpdateBriefingPreferencesRequest request,
        CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        try
        {
            await _mediator.Send(new UpdateBriefingPreferencesCommand(
                userId,
                request.BriefingHour,
                request.EmailSectionEnabled,
                request.CalendarSectionEnabled,
                request.TrelloSectionEnabled,
                request.TasksSectionEnabled,
                request.WeatherSectionEnabled), ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        try
        {
            await _mediator.Send(new ChangePasswordCommand(
                userId,
                request.CurrentPassword,
                request.NewPassword), ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount(
        [FromBody] DeleteAccountRequest request,
        CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        try
        {
            await _mediator.Send(new DeleteAccountCommand(userId, request.Password), ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("profile-picture")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfilePicture(
        IFormFile file,
        CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado." });

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var relativePath = await _mediator.Send(new UploadProfilePictureCommand(
                userId,
                ms.ToArray(),
                file.FileName,
                file.ContentType,
                file.Length), ct);

            // Retorna a URL completa para que o frontend possa exibir a imagem
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullUrl = $"{baseUrl}{relativePath}";

            return Ok(new { url = fullUrl });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}