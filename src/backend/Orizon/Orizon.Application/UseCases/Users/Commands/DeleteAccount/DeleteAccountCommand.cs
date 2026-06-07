using MediatR;

namespace Orizon.Application.UseCases.Users.Commands.DeleteAccount;

public record DeleteAccountCommand(
    Guid UserId,
    string Password
) : IRequest;