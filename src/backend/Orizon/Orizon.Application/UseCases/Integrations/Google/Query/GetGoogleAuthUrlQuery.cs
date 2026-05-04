using MediatR;

namespace Orizon.Application.UseCases.Integrations.Google.Query;

public record GetGoogleAuthUrlQuery(
    string UserId,
    string State
) : IRequest<string>;