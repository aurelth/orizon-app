using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Application.UseCases.Auth.Commands.Login;
using Orizon.Application.UseCases.Auth.Commands.Logout;
using Orizon.Application.UseCases.Auth.Commands.RefreshToken;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using System.Security.Claims;

namespace Orizon.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // POST /auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            _logger.LogInformation(
                "Novo usuário registrado: {Email}", command.Email);
            return Created("/auth/login", result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = "Dados inválidos.",
                errors = ex.Errors.Select(e => e.ErrorMessage)
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            _logger.LogInformation(
                "Login bem-sucedido: {Email}", command.Email);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = "Dados inválidos.",
                errors = ex.Errors.Select(e => e.ErrorMessage)
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST /auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = "Dados inválidos.",
                errors = ex.Errors.Select(e => e.ErrorMessage)
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST /auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Usuário não identificado." });

            await _mediator.Send(new LogoutCommand(userId), ct);
            _logger.LogInformation("Logout efetuado: {UserId}", userId);
            return Ok(new { message = "Logout efetuado com sucesso." });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = "Dados inválidos.",
                errors = ex.Errors.Select(e => e.ErrorMessage)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao efetuar logout");
            return StatusCode(500, new { message = "Erro interno." });
        }
    }
}