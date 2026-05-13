using MediatR;

namespace Orizon.Application.UseCases.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest;