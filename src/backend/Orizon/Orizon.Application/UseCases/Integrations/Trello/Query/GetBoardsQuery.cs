using MediatR;
using Orizon.Application.DTOs.Trello;

namespace Orizon.Application.UseCases.Integrations.Trello.Query;

public record GetBoardsQuery(
    string ApiKey,
    string Token
) : IRequest<IEnumerable<TrelloBoardDto>>;