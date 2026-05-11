using MediatR;
using Orizon.Application.DTOs.Trello;

namespace Orizon.Application.UseCases.Integrations.Trello.Query;

public record GetUserBoardsQuery(Guid UserId) : IRequest<IEnumerable<TrelloBoardDto>>;