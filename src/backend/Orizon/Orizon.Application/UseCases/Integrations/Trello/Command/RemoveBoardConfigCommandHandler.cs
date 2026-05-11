using MediatR;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Integrations.Trello.Command;

public class RemoveBoardConfigCommandHandler : IRequestHandler<RemoveBoardConfigCommand>
{
    private readonly ITrelloBoardConfigRepository _repository;

    public RemoveBoardConfigCommandHandler(ITrelloBoardConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        RemoveBoardConfigCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByUserAndBoardAsync(
            request.UserId, request.BoardId, cancellationToken);

        if (config is null) return;

        await _repository.DeleteAsync(config.Id, cancellationToken);
    }
}