using MediatR;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Integrations.Trello.Query;

public class GetUserBoardsQueryHandler
    : IRequestHandler<GetUserBoardsQuery, IEnumerable<TrelloBoardDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITrelloService _trelloService;

    public GetUserBoardsQueryHandler(
        IUserRepository userRepository,
        ITrelloService trelloService)
    {
        _userRepository = userRepository;
        _trelloService = trelloService;
    }

    public async Task<IEnumerable<TrelloBoardDto>> Handle(
        GetUserBoardsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null || !user.TrelloEnabled ||
            string.IsNullOrEmpty(user.TrelloApiKey) ||
            string.IsNullOrEmpty(user.TrelloToken))
            return [];

        return await _trelloService.GetBoardsAsync(
            user.TrelloApiKey,
            user.TrelloToken,
            cancellationToken);
    }
}