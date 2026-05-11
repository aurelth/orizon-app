using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Application.Interfaces.Repositories;
using System.Security.Claims;

namespace Orizon.API.Controllers;

[ApiController]
[Route("location")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;

    public LocationController(IMediator mediator, IUserRepository userRepository)
    {
        _mediator = mediator;
        _userRepository = userRepository;
    }

    [HttpPost]
    public async Task<IActionResult> SaveLocation(
        [FromBody] SaveLocationRequest request,
        CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        var user = await GetUserAndUpdate(userId, request, ct);

        return Ok(new { message = "Localização salva com sucesso." });
    }

    private async Task<bool> GetUserAndUpdate(
        string userId,
        SaveLocationRequest request,
        CancellationToken ct)
    {
        var userRepository = HttpContext.RequestServices
            .GetRequiredService<Orizon.Application.Interfaces.Repositories.IUserRepository>();

        var user = await userRepository.GetByIdAsync(Guid.Parse(userId), ct);
        if (user is null) return false;

        user.LocationName = request.LocationName;
        user.Latitude = request.Latitude;
        user.Longitude = request.Longitude;

        await userRepository.UpdateAsync(user, ct);
        return true;
    }

    [HttpGet]
    public async Task<IActionResult> GetLocation(CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId), ct);

        if (user is null) return NotFound();

        return Ok(new
        {
            locationName = user.LocationName,
            latitude = user.Latitude,
            longitude = user.Longitude,
        });
    }
}

public record SaveLocationRequest(
    string LocationName,
    double Latitude,
    double Longitude);