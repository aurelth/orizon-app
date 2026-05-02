using MediatR;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        await _refreshTokenRepository
            .RevokeAllUserTokensAsync(request.UserId, ct);
    }
}