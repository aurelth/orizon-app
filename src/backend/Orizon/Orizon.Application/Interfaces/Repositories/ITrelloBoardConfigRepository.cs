using Orizon.Domain.Entities;

namespace Orizon.Application.Interfaces.Repositories;

public interface ITrelloBoardConfigRepository
{
    Task<TrelloBoardConfig?> GetByUserAndBoardAsync(
        Guid userId,
        string boardId,
        CancellationToken ct = default);

    Task<IEnumerable<TrelloBoardConfig>> GetByUserAsync(
        Guid userId,
        CancellationToken ct = default);

    Task AddAsync(
        TrelloBoardConfig config,
        CancellationToken ct = default);

    Task UpdateAsync(
        TrelloBoardConfig config,
        CancellationToken ct = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken ct = default);
}