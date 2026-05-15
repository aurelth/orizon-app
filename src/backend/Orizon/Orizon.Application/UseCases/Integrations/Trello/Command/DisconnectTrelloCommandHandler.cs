using MediatR;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Integrations.Trello.Command;

public class DisconnectTrelloCommandHandler : IRequestHandler<DisconnectTrelloCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITrelloBoardConfigRepository _boardConfigRepository;

    public DisconnectTrelloCommandHandler(
        IUserRepository userRepository,
        ITrelloBoardConfigRepository boardConfigRepository)
    {
        _userRepository = userRepository;
        _boardConfigRepository = boardConfigRepository;
    }

    public async Task Handle(
        DisconnectTrelloCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(
            request.UserId, cancellationToken);

        if (user is null) return;

        user.TrelloApiKey = null;
        user.TrelloToken = null;
        user.TrelloEnabled = false;

        await _userRepository.UpdateAsync(user, cancellationToken);

        var configs = await _boardConfigRepository.GetByUserAsync(
            request.UserId, cancellationToken);

        foreach (var config in configs)
            await _boardConfigRepository.DeleteAsync(config.Id, cancellationToken);
    }
}