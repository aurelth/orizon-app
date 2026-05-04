using MediatR;

namespace Orizon.Application.UseCases.Integrations.Trello.Command;

public record SaveBoardConfigCommand(
    Guid UserId,
    string BoardId,
    string BoardName,
    string? BoardColor,
    string? TodayListId,
    string? TodayListName,
    string? InProgressListId,
    string? InProgressListName
) : IRequest;