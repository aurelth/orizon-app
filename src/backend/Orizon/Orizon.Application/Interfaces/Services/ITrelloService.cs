using Orizon.Application.DTOs.Trello;

namespace Orizon.Application.Interfaces.Services;

public interface ITrelloService
{
    Task<IEnumerable<TrelloTaskDto>> GetActiveTasksAsync(
        string userId,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TrelloBoardDto>> GetBoardsAsync(
        string apiKey,
        string token,
        CancellationToken cancellationToken = default);
}