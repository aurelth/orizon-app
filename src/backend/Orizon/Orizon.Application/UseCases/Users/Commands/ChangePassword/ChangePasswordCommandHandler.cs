using MediatR;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CurrentPassword == request.NewPassword)
            throw new InvalidOperationException(
                "A nova senha deve ser diferente da senha atual.");

        var (success, errors) = await _identityService.ChangePasswordAsync(
            request.UserId.ToString(),
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!success)
            throw new InvalidOperationException(
                errors.FirstOrDefault() ?? "Erro ao alterar senha.");
    }
}