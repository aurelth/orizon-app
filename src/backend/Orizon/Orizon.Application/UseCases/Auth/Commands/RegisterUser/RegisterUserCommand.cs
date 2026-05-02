using MediatR;
using Orizon.Application.DTOs.Auth;

namespace Orizon.Application.UseCases.Auth.Commands.RegisterUser;

public record RegisterUserCommand(
    string DisplayName,
    string Email,
    string Password) : IRequest<AuthResponseDto>;