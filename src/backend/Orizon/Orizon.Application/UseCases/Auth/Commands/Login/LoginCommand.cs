using MediatR;
using Orizon.Application.DTOs.Auth;

namespace Orizon.Application.UseCases.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password) : IRequest<AuthResponseDto>;