using MediatR;
using Orizon.Application.DTOs.Auth;

namespace Orizon.Application.UseCases.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string Token) : IRequest<AuthResponseDto>;