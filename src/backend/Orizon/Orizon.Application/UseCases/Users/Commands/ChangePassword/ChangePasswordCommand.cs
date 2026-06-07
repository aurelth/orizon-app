using MediatR;

namespace Orizon.Application.UseCases.Users.Commands.ChangePassword;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest;