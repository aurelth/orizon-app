using MediatR;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Domain.Entities;

namespace Orizon.Application.UseCases.Integrations.Trello.Command;

public class SaveBoardConfigCommandHandler : IRequestHandler<SaveBoardConfigCommand>
{
    private readonly ITrelloBoardConfigRepository _repository;

    public SaveBoardConfigCommandHandler(ITrelloBoardConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        SaveBoardConfigCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByUserAndBoardAsync(
            request.UserId,
            request.BoardId,
            cancellationToken);

        if (existing != null)
        {
            existing.TodayListId = request.TodayListId;
            existing.TodayListName = request.TodayListName;
            existing.InProgressListId = request.InProgressListId;
            existing.InProgressListName = request.InProgressListName;
            existing.BoardColor = request.BoardColor;
            existing.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var config = new TrelloBoardConfig
            {
                UserId = request.UserId,
                BoardId = request.BoardId,
                BoardName = request.BoardName,
                BoardColor = request.BoardColor,
                TodayListId = request.TodayListId,
                TodayListName = request.TodayListName,
                InProgressListId = request.InProgressListId,
                InProgressListName = request.InProgressListName,
                IsActive = true,
            };
            await _repository.AddAsync(config, cancellationToken);
        }
    }
}