using MediatR;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Integrations.Google.Query;

public class GetGoogleAuthUrlQueryHandler : IRequestHandler<GetGoogleAuthUrlQuery, string>
{
    private readonly IGoogleOAuthService _googleOAuthService;

    public GetGoogleAuthUrlQueryHandler(IGoogleOAuthService googleOAuthService)
    {
        _googleOAuthService = googleOAuthService;
    }

    public Task<string> Handle(
        GetGoogleAuthUrlQuery request,
        CancellationToken cancellationToken)
    {
        var url = _googleOAuthService.GetAuthorizationUrl(request.UserId, request.State);
        return Task.FromResult(url);
    }
}