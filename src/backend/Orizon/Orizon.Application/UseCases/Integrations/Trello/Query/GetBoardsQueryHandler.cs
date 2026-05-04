using MediatR;
using Orizon.Application.DTOs.Trello;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Integrations.Trello.Query;

public class GetBoardsQueryHandler : IRequestHandler<GetBoardsQuery, IEnumerable<TrelloBoardDto>>
{
    private readonly ITrelloService _trelloService;

    public GetBoardsQueryHandler(ITrelloService trelloService)
    {
        _trelloService = trelloService;
    }

    public async Task<IEnumerable<TrelloBoardDto>> Handle(
        GetBoardsQuery request,
        CancellationToken cancellationToken)
    {
        return await _trelloService.GetBoardsAsync(
            request.ApiKey,
            request.Token,
            cancellationToken);
    }
}