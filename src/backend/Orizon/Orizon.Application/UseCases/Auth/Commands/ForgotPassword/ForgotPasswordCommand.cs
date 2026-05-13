using MediatR;

namespace Orizon.Application.UseCases.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest;