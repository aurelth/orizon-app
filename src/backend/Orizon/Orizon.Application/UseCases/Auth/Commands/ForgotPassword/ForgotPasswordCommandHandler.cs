using MediatR;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailNotificationService _emailService;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailNotificationService emailService)
    {
        _identityService = identityService;
        _emailService = emailService;
    }

    public async Task Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByEmailAsync(
            request.Email, cancellationToken);

        // não revelamos se o email existe ou não por segurança
        if (user is null) return;

        var token = await _identityService.GeneratePasswordResetTokenAsync(
            user.Id.ToString(), cancellationToken);

        await _emailService.SendPasswordResetEmailAsync(
            user.Email,
            user.DisplayName,
            token,
            cancellationToken);
    }
}