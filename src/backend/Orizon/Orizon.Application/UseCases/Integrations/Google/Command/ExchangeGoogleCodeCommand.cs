using MediatR;

namespace Orizon.Application.UseCases.Integrations.Google.Command;

public record ExchangeGoogleCodeCommand(
    string UserId,
    string Code
) : IRequest<ExchangeGoogleCodeResult>;

public record ExchangeGoogleCodeResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);