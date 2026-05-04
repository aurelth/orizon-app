using MediatR;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Integrations.Google.Command;

public class ExchangeGoogleCodeCommandHandler
    : IRequestHandler<ExchangeGoogleCodeCommand, ExchangeGoogleCodeResult>
{
    private readonly IGoogleOAuthService _googleOAuthService;

    public ExchangeGoogleCodeCommandHandler(IGoogleOAuthService googleOAuthService)
    {
        _googleOAuthService = googleOAuthService;
    }

    public async Task<ExchangeGoogleCodeResult> Handle(
        ExchangeGoogleCodeCommand request,
        CancellationToken cancellationToken)
    {
        var tokens = await _googleOAuthService.ExchangeCodeAsync(
            request.Code,
            cancellationToken);

        return new ExchangeGoogleCodeResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt);
    }
}