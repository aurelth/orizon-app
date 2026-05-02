using MediatR;

namespace Orizon.Application.UseCases.Auth.Commands.Logout;

public record LogoutCommand(string UserId) : IRequest;