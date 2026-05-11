using MediatR;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Integrations.Google.Command;

public class ExchangeGoogleCodeCommandHandler
    : IRequestHandler<ExchangeGoogleCodeCommand, ExchangeGoogleCodeResult>
{
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly IUserRepository _userRepository;

    public ExchangeGoogleCodeCommandHandler(
        IGoogleOAuthService googleOAuthService,
        IUserRepository userRepository)
    {
        _googleOAuthService = googleOAuthService;
        _userRepository = userRepository;
    }

    public async Task<ExchangeGoogleCodeResult> Handle(
        ExchangeGoogleCodeCommand request,
        CancellationToken cancellationToken)
    {
        var tokens = await _googleOAuthService.ExchangeCodeAsync(
            request.Code,
            cancellationToken);

        // ADICIONADO: salvar tokens no banco
        if (!string.IsNullOrEmpty(request.UserId))
        {
            var user = await _userRepository.GetByIdAsync(
                Guid.Parse(request.UserId), cancellationToken);

            if (user != null)
            {
                user.GoogleAccessToken = tokens.AccessToken;
                user.GoogleRefreshToken = tokens.RefreshToken;
                user.GoogleTokenExpiresAt = tokens.ExpiresAt;
                user.GoogleConnectedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user, cancellationToken);
            }
        }

        return new ExchangeGoogleCodeResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt);
    }
}