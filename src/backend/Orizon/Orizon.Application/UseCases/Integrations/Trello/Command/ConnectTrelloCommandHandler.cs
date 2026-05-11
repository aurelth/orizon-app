using MediatR;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Integrations.Trello.Command;

public class ConnectTrelloCommandHandler : IRequestHandler<ConnectTrelloCommand>
{
    private readonly IUserRepository _userRepository;

    public ConnectTrelloCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(
        ConnectTrelloCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return;

        user.TrelloApiKey = request.ApiKey;
        user.TrelloToken = request.Token;
        user.TrelloEnabled = true;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}