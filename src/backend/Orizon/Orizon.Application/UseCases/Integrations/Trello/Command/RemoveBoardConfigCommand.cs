using MediatR;

namespace Orizon.Application.UseCases.Integrations.Trello.Command;

public record RemoveBoardConfigCommand(Guid UserId, string BoardId) : IRequest;