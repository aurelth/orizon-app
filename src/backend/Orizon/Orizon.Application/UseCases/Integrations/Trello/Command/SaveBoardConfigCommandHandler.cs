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
        // desativa todos os boards anteriores do usuário
        var existing = await _repository.GetByUserAsync(request.UserId, cancellationToken);
        foreach (var config in existing.Where(c => c.BoardId != request.BoardId))
        {
            config.IsActive = false;
            await _repository.UpdateAsync(config, cancellationToken);
        }

        // upsert no board selecionado
        var board = existing.FirstOrDefault(c => c.BoardId == request.BoardId);

        if (board != null)
        {
            board.IsActive = true;
            board.TodayListId = request.TodayListId;
            board.TodayListName = request.TodayListName;
            board.InProgressListId = request.InProgressListId;
            board.InProgressListName = request.InProgressListName;
            board.BoardColor = request.BoardColor;
            board.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(board, cancellationToken);
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