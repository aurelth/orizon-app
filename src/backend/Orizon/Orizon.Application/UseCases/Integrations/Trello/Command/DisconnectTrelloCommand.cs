using MediatR;

namespace Orizon.Application.UseCases.Integrations.Trello.Command;

public record DisconnectTrelloCommand(Guid UserId) : IRequest;