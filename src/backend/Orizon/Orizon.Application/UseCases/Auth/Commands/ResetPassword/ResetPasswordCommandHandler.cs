using MediatR;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(
    ResetPasswordCommand request,
    CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByEmailAsync(
            request.Email, cancellationToken);

        if (user is null)
            throw new InvalidOperationException("Usuário não encontrado.");

        var isSamePassword = await _identityService.CheckPasswordAsync(
            user.Id.ToString(), request.NewPassword, cancellationToken);

        if (isSamePassword)
            throw new InvalidOperationException(
                "A nova senha não pode ser igual à senha atual.");

        var success = await _identityService.ResetPasswordAsync(
            user.Id.ToString(),
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!success)
            throw new InvalidOperationException(
                "Token inválido ou expirado. Solicite um novo link de redefinição.");
    }
}