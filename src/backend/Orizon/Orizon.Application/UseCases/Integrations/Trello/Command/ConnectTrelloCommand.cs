using MediatR;

namespace Orizon.Application.UseCases.Integrations.Trello.Command;

public record ConnectTrelloCommand(
    Guid UserId,
    string ApiKey,
    string Token
) : IRequest;